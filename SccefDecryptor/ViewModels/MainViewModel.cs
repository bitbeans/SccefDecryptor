using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using SccefDecryptor.Models;
using SccefDecryptor.Tools;
using StreamCryptor;
using StreamCryptor.Model;
using WPFLocalizeExtension.Engine;

namespace SccefDecryptor.ViewModels
{
	/// <summary>
	///     The MainViewModel.
	/// </summary>
	[Export]
	public sealed class MainViewModel : Screen, IShell, IHandle<ActivationDataMessage>
	{
		private const string ApplicationName = "sccef Decryptor";
		private readonly IWindowManager _windowManager;
		private string _inputFilePath;
		private string _outputFilePath;
		private int _overlayDependencies;
		private int _progressBarValue;

		/// <summary>
		///     MainViewModel construcor (XAML).
		/// </summary>
		public MainViewModel()
		{
		}

		/// <summary>
		///     MainViewModel construcor.
		/// </summary>
		/// <param name="windowManager">The current window manager.</param>
		/// <param name="eventAggregator">The event aggregator.</param>
		[ImportingConstructor]
		private MainViewModel(IWindowManager windowManager, IEventAggregator eventAggregator)
		{
			_windowManager = windowManager;
			eventAggregator.Subscribe(this);

			DisplayName = string.Format("{0} {1}", ApplicationName, VersionUtilities.PublishVersion);

			// automatically use the correct translations if available (fallback: en)
			LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
			LocalizeDictionary.Instance.Culture = Thread.CurrentThread.CurrentCulture;
		}

		/// <summary>
		///     Overlay management for MetroMessageBoxViewModel.
		/// </summary>
		public bool IsOverlayVisible
		{
			get { return _overlayDependencies > 0; }
		}

		/// <summary>
		///     The given (encrypted) file.
		/// </summary>
		public string InputFilePath
		{
			get { return _inputFilePath; }
			set
			{
				_inputFilePath = value;
				NotifyOfPropertyChange(() => InputFilePath);
			}
		}

		/// <summary>
		///     Path to the decrypted file.
		/// </summary>
		public string OutputFilePath
		{
			get { return _outputFilePath; }
			set
			{
				_outputFilePath = value;
				NotifyOfPropertyChange(() => OutputFilePath);
			}
		}

		/// <summary>
		///     User visible progress value.
		/// </summary>
		public int ProgressBarValue
		{
			get { return _progressBarValue; }
			set
			{
				_progressBarValue = value;
				NotifyOfPropertyChange(() => ProgressBarValue);
			}
		}

		/// <summary>
		///     Handle the given file.
		/// </summary>
		/// <param name="activationDataMessage"></param>
		public async void Handle(ActivationDataMessage activationDataMessage)
		{
			try
			{
				// validate the inputFile
				if (string.IsNullOrEmpty(activationDataMessage.AbsolutePath))
				{
					throw new ArgumentOutOfRangeException(
						string.Format(LocalizationEx.GetUiString("error_filename_invalid_size", Thread.CurrentThread.CurrentCulture), 0));
				}
				if (!File.Exists(activationDataMessage.AbsolutePath))
				{
					throw new FileNotFoundException(LocalizationEx.GetUiString("error_file_not_found",
						Thread.CurrentThread.CurrentCulture));
				}
				InputFilePath = activationDataMessage.AbsolutePath;
				var outputFolder = Path.GetDirectoryName(InputFilePath);
				// validate the outputFolder
				if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
				{
					throw new DirectoryNotFoundException(LocalizationEx.GetUiString("error_directory_not_found",
						Thread.CurrentThread.CurrentCulture));
				}

				if (outputFolder.IndexOfAny(Path.GetInvalidPathChars()) > -1)
					throw new ArgumentException(LocalizationEx.GetUiString("error_bad_character_input",
						Thread.CurrentThread.CurrentCulture));

				var decryptionProgress = new Progress<StreamCryptorTaskAsyncProgress>();
				decryptionProgress.ProgressChanged += (s, e) => { ProgressBarValue = e.ProgressPercentage; };

				var identityWindow = new IdentityViewModel
				{
					DisplayName = ApplicationName
				};

				dynamic settings = new ExpandoObject();
				settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				settings.Owner = GetView();
				var calculation = _windowManager.ShowDialog(identityWindow, null, settings);
				if (calculation == true)
				{
					if (identityWindow.KeyPair != null)
					{
						//TODO: maybe allow cancellation on large files
						var cancellationToken = new CancellationToken();
						var outputFile =
							await
								Cryptor.DecryptFileWithStreamAsync(identityWindow.KeyPair, InputFilePath, outputFolder, decryptionProgress, true,
									cancellationToken).ConfigureAwait(false);
						identityWindow.KeyPair = null;
						var outputFileFullPath = Path.Combine(outputFolder, outputFile);
						if (File.Exists(outputFileFullPath))
						{
							ProgressBarValue = 0;
							OutputFilePath = outputFileFullPath;
						}
						else
						{
							throw new FileNotFoundException(LocalizationEx.GetUiString("error_decrypted_file_not_found",
								Thread.CurrentThread.CurrentCulture));
						}
					}
					else
					{
						throw new CryptographicException(LocalizationEx.GetUiString("error_failed_to_calculate",
							Thread.CurrentThread.CurrentCulture));
					}
				}
				else
				{
					Environment.Exit(0);
				}
			}
			catch (Exception exception)
				when (
					exception is ArgumentOutOfRangeException ||
					exception is ArgumentException ||
					exception is CryptographicException ||
					exception is FileNotFoundException ||
					exception is DirectoryNotFoundException)
			{
				_windowManager.ShowMetroMessageBox(
					exception.Message, LocalizationEx.GetUiString("error_header_handled", Thread.CurrentThread.CurrentCulture),
					MessageBoxButton.OK, BoxType.Warning);
				TryClose();
			}
			catch (Exception unhandledException)
			{
				_windowManager.ShowMetroMessageBox(
					unhandledException.Message,
					LocalizationEx.GetUiString("error_header_unhandled", Thread.CurrentThread.CurrentCulture),
					MessageBoxButton.OK, BoxType.Error);
				TryClose();
			}
		}

		/// <summary>
		///     Overlay management for MetroMessageBoxViewModel.
		/// </summary>
		public void ShowOverlay()
		{
			_overlayDependencies++;
			NotifyOfPropertyChange(() => IsOverlayVisible);
		}

		/// <summary>
		///     Overlay management for MetroMessageBoxViewModel.
		/// </summary>
		public void HideOverlay()
		{
			_overlayDependencies--;
			NotifyOfPropertyChange(() => IsOverlayVisible);
		}

		/// <summary>
		///     Close the application.
		/// </summary>
		public void Close()
		{
			TryClose();
		}

		/// <summary>
		///     Open a file with associated application.
		/// </summary>
		public async void OpenOutputFilePath()
		{
			if (File.Exists(OutputFilePath))
			{
				await Task.Run(() => { Process.Start(OutputFilePath); }).ConfigureAwait(false);
			}
		}
	}
}