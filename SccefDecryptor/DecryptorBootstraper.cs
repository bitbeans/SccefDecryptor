using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using SccefDecryptor.Models;
using SccefDecryptor.ViewModels;

namespace SccefDecryptor
{
	public class DecryptorBootstraper : BootstrapperBase
	{
		private CompositionContainer _container;
		private IEventAggregator _events;

		public DecryptorBootstraper()
		{
			Initialize();
		}

		/// <summary>
		///     Catch all unhandled exceptions and show a Windows Form MessageBox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Execute.OnUIThread(
				() =>
					MessageBox.Show(
						string.Format("Message: {0}\nStackTrace: {1}", e.Exception.Message, e.Exception.StackTrace),
						"Error", MessageBoxButton.OK, MessageBoxImage.Error));
		}

		/// <summary>
		///     Handle application startup per file extension.
		/// </summary>
		/// <param name="query">File URI with path to a sccef file.</param>
		public void HandleQueryString(string query)
		{
			try
			{
				if (query.Length <= 0) return;
				var uri = new Uri(query);
				if (!uri.IsFile) return;
				var unescaped = Uri.UnescapeDataString(uri.AbsolutePath);
				var tmpExtension = Path.GetExtension(unescaped);
				// only allow sccef files
				if (!tmpExtension.Equals(".sccef")) return;
				var tmpFileName = Path.GetFileNameWithoutExtension(unescaped);
				var tmpAbsolutePath = Path.GetFullPath(unescaped);
				// pass the sccef file to the main view
				_events.PublishOnUIThreadAsync(new ActivationDataMessage
				{
					FileName = tmpFileName,
					AbsolutePath = tmpAbsolutePath
				});
			}
			catch (Exception)
			{
			}
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			base.OnStartup(sender, e);
			DisplayRootViewFor<MainViewModel>();

			var args = e.Args.ToList();
			if (args.Count > 0)
			{
				HandleQueryString(string.Join(" ", args));
			}
			else
			{
				Environment.Exit(0);
			}
		}

		protected override void Configure()
		{
			try
			{
				_events = new EventAggregator();
				_container =
					new CompositionContainer(
						new AggregateCatalog(
							AssemblySource.Instance.Select(x => new AssemblyCatalog(x)).OfType<ComposablePartCatalog>()));
				var batch = new CompositionBatch();
				batch.AddExportedValue<IWindowManager>(new AppWindowManager());
				batch.AddExportedValue(_events);
				batch.AddExportedValue(_container);
				_container.Compose(batch);
			}
			catch (Exception)
			{
			}
		}

		protected override object GetInstance(Type serviceType, string key)
		{
			try
			{
				var contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
				var exports = _container.GetExportedValues<object>(contract);
				var enumerable = exports as IList<object> ?? exports.ToList();
				if (enumerable.Any())
				{
					return enumerable.First();
				}
			}
			catch (Exception)
			{
			}
			return null;
		}
	}
}