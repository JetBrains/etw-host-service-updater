using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.Util;
using JetBrains.Etw.HostService.Notifier.ViewModel;

namespace JetBrains.Etw.HostService.Notifier
{
  internal partial class MainWindow : Window
  {
    private readonly bool myDownloadDelay;
    private readonly ILogger myLogger;
    private readonly UpdateStatusViewModel myViewModel = new();
    private UpdateChecker.Result myUpdateRequest;

    public MainWindow([NotNull] ILogger logger, [NotNull] Options options)
    {
      if (options == null) throw new ArgumentNullException(nameof(options));
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));

      myDownloadDelay = options.CheckInterval != null;

      var loggerContext = Logger.Context;

      void CheckForUpdate()
      {
        UpdateChecker.Result updateRequest;
        try
        {
          var installedVersion = options.CheckForVersion ?? VersionControl.GetInstalledVersion(logger);
          updateRequest = installedVersion != null ? UpdateChecker.Check(logger, options.BaseUri ?? UpdateChecker.PublicBaseUri, "EHS", installedVersion, UpdateChecker.Channel.EapAndRelease) : null;
        }
        catch (Exception e)
        {
          logger.Exception(e);
          return;
        }

        Interlocked.Exchange(ref myUpdateRequest, updateRequest);
        myViewModel.SetUpdateRequest(updateRequest);
        if (updateRequest == null)
        {
          logger.Info($"{loggerContext} res=exit_no_update");
          Application.Current.Shutdown();
        }
      }

      CheckForUpdate();

      // Note(ww898): Prevent blinking the tray icon!!!
      if (myUpdateRequest == null)
        return;

      InitializeComponent();
      DataContext = myViewModel;

      var timer = new DispatcherTimer {Interval = options.CheckInterval ?? TimeSpan.FromHours(23)};
      timer.Tick += (_, _) => CheckForUpdate();
      Closed += (_, _) => timer.Stop();
      timer.Start();
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      using (myViewModel.RunModalDialog())
        new AboutWindow().ShowDialog();
    }

    private void OnInstall(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      using (myViewModel.RunModalDialog())
        new DownloadingWindow(myLogger, myUpdateRequest, myDownloadDelay).ShowDialog();
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
      myLogger.Info($"{Logger.Context} res=exit_menu");
      Application.Current.Shutdown();
    }
  }
}