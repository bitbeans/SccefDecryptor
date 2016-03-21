using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using NaclKeys;
using Sodium;

namespace SccefDecryptor.ViewModels
{
	[Export]
	public sealed class IdentityViewModel : Screen
	{
		private string _inputOne;
		private string _inputTwo;
		private bool _isCalculating;
		private bool _keyFormatBytejailChecked;
		private bool _keyFormatCurveLockChecked;
		private bool _keyFormatMiniLockChecked;

		/// <summary>
		///     IdentityViewModel constructor.
		/// </summary>
		[ImportingConstructor]
		public IdentityViewModel()
		{
			// set the default key format
			_keyFormatBytejailChecked = true;
		}

		public KeyPair KeyPair { get; set; }

		public bool IsCalculating
		{
			get { return _isCalculating; }
			set
			{
				if (value.Equals(_isCalculating)) return;
				_isCalculating = value;
				NotifyOfPropertyChange(() => IsCalculating);
			}
		}

		public bool KeyFormatBytejailChecked
		{
			get { return _keyFormatBytejailChecked; }
			set
			{
				if (value.Equals(_keyFormatBytejailChecked)) return;
				_keyFormatBytejailChecked = value;
				NotifyOfPropertyChange(() => KeyFormatBytejailChecked);
			}
		}

		public bool KeyFormatCurveLockChecked
		{
			get { return _keyFormatCurveLockChecked; }
			set
			{
				if (value.Equals(_keyFormatCurveLockChecked)) return;
				_keyFormatCurveLockChecked = value;
				NotifyOfPropertyChange(() => KeyFormatCurveLockChecked);
			}
		}

		public bool KeyFormatMiniLockChecked
		{
			get { return _keyFormatMiniLockChecked; }
			set
			{
				if (value.Equals(_keyFormatMiniLockChecked)) return;
				_keyFormatMiniLockChecked = value;
				NotifyOfPropertyChange(() => KeyFormatMiniLockChecked);
			}
		}

		/// <summary>
		///     User input part one.
		/// </summary>
		public string InputOne
		{
			get { return _inputOne; }
			set
			{
				if (value.Equals(_inputOne)) return;
				_inputOne = value;
				NotifyOfPropertyChange(() => InputOne);
			}
		}

		/// <summary>
		///     User input part two.
		/// </summary>
		public string InputTwo
		{
			get { return _inputTwo; }
			set
			{
				if (value.Equals(_inputTwo)) return;
				_inputTwo = value;
				NotifyOfPropertyChange(() => InputTwo);
			}
		}

		/// <summary>
		///     InputOne changed.
		/// </summary>
		/// <param name="e"></param>
		public void InputOneChanged(RoutedEventArgs e)
		{
			var t = e.Source as PasswordBox;
			if (t != null && !string.IsNullOrEmpty(t.Password))
			{
				if (t.Password.Length == 1)
				{
					// set the cursor position to 1
					SetSelection(t, 1, 0);
				}
				InputOne = t.Password;
			}
			else
			{
				InputOne = string.Empty;
			}
		}

		/// <summary>
		///     InputTwo changed.
		/// </summary>
		/// <param name="e"></param>
		public void InputTwoChanged(RoutedEventArgs e)
		{
			var t = e.Source as PasswordBox;
			if (t != null && !string.IsNullOrEmpty(t.Password))
			{
				if (t.Password.Length == 1)
				{
					// set the cursor position to 1
					SetSelection(t, 1, 0);
				}
				InputTwo = t.Password;
			}
			else
			{
				InputTwo = string.Empty;
			}
		}

		/// <summary>
		///     Sets the cursor to a given selection.
		/// </summary>
		/// <param name="passwordBox">The PasswordBox to set the selection.</param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		private static void SetSelection(PasswordBox passwordBox, int start, int length)
		{
			passwordBox.GetType()
				.GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(passwordBox, new object[] {start, length});
		}

		/// <summary>
		///     Caluclate a keypair.
		/// </summary>
		public async void CalculateKeyPair()
		{
			var result = false;
			try
			{
				if (KeyFormatBytejailChecked)
				{
					IsCalculating = true;
					await
						Task.Run(() => { KeyPair = KeyGenerator.GenerateBytejailKeyPair(InputOne, InputTwo); })
							.ConfigureAwait(false);
					result = true;
				}
				else if (KeyFormatCurveLockChecked)
				{
					//TODO: validate input (mail) before calculate
					IsCalculating = true;
					await
						Task.Run(() => { KeyPair = KeyGenerator.GenerateCurveLockKeyPair(InputOne, InputTwo); })
							.ConfigureAwait(false);
					result = true;
				}
				else if (KeyFormatMiniLockChecked)
				{
					//TODO: validate input (mail) before calculate
					IsCalculating = true;
					await
						Task.Run(() => { KeyPair = KeyGenerator.GenerateMiniLockKeyPair(InputOne, InputTwo); })
							.ConfigureAwait(false);
					result = true;
				}
			}
			catch (ArgumentNullException)
			{
				Environment.Exit(0);
			}
			IsCalculating = false;
			InputOne = string.Empty;
			InputTwo = string.Empty;
			KeyFormatCurveLockChecked = false;
			KeyFormatMiniLockChecked = false;
			KeyFormatBytejailChecked = true;
			TryClose(result);
		}
	}
}