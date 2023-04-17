using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using JetBrains.Etw.HostService.Updater.ViewModel;

namespace JetBrains.Etw.HostService.Updater.Views
{
  internal partial class AboutWindow : Window
  {
    private readonly ILogger myLogger;

    public AboutWindow([NotNull] ILogger logger)
    {
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
      InitializeComponent();
      DataContext = new AboutViewModel();
    }

    public void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      myLogger.Info(Logger.Context);
      Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
    }
  }
}