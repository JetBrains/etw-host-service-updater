using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.SharedStorage;
using JetBrains.Etw.HostService.Notifier.Util;
using JetBrains.Etw.HostService.Notifier.ViewModel;

namespace JetBrains.Etw.HostService.Notifier.Views
{
  internal partial class MainWindow : Window
  {
    private readonly bool myDownloadDelay;
    private readonly ILogger myLogger;
    private readonly UpdateStatusViewModel myViewModel = new();
    private UpdateRequest myUpdateRequest;

    public MainWindow([NotNull] ILogger logger, [NotNull] Options options)
    {
      if (options == null) throw new ArgumentNullException(nameof(options));
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
     
      myDownloadDelay = options.CheckInterval != null;

      var loggerContext = Logger.Context;
      var anonymousPermanentUserId = new AnonymousPermanentUserIdAccessor(logger);

      void CheckForUpdate()
      {
        UpdateRequest updateRequest;
        try
        {
          var installedVersion = options.CheckForVersion ?? VersionControl.GetInstalledVersion(logger);
          updateRequest = installedVersion != null ? UpdateChecker.Check(logger, options.BaseUri ?? UpdateChecker.PublicBaseUri, "EHS", installedVersion, anonymousPermanentUserId.GetOrGenerate()) : null;
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

      var timer = new DispatcherTimer {Interval = options.CheckInterval ?? UpdateChecker.DefaultCheckInterval};
      timer.Tick += (_, _) => CheckForUpdate();
      Closed += (_, _) => timer.Stop();
      timer.Start();
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      using (myViewModel.RunModalDialog())
        new AboutWindow(myLogger).ShowDialog();
    }

    private void OnWhatsNew(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      using (myViewModel.RunModalDialog())
        new WhatsNewWindow(myLogger, myUpdateRequest).ShowDialog();
    }

    private void OnDownloadAndInstall(object sender, RoutedEventArgs e)
    {
      myLogger.Info(Logger.Context);
      using (myViewModel.RunModalDialog())
      {
        var dlg = new DownloadingWindow(myLogger, myUpdateRequest, myDownloadDelay);
        if (dlg.ShowDialog() == true)
          try
          {
            myLogger.Info($"{Logger.Context} res=running");
            using var process = Process.Start(new ProcessStartInfo
              {
                UseShellExecute = true,
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe"),
                Arguments = $"/i \"{dlg.MsiFile}\""
              });
            if (process == null)
              throw new Exception("Failed to run msiexec.exe");
            myLogger.Info($"{Logger.Context} res=exit_run");
            Application.Current.Shutdown();
          }
          catch (Exception ex)
          {
            myLogger.Exception(ex);
            MessageBox.Show(ex.GetBaseException().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        else
          myLogger.Info($"{Logger.Context} res=cancel");
      }
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
      myLogger.Info($"{Logger.Context} res=exit_menu");
      Application.Current.Shutdown();
    }
  }
}