using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.Util;

namespace JetBrains.Etw.HostService.Notifier
{
  internal partial class AboutWindow : Window
  {
    private readonly ILogger myLogger;

    public AboutWindow([NotNull] ILogger logger)
    {
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
      InitializeComponent();
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      myLogger.Info(Logger.Context);
      Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
    }
  }
}