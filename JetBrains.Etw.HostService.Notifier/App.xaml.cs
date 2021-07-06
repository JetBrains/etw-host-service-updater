using System;
using System.IO;
using System.Threading;
using System.Windows;
using JetBrains.Etw.HostService.Notifier.Util;

namespace JetBrains.Etw.HostService.Notifier
{
  public partial class App : Application
  {
    private void OnStartup(object sender, StartupEventArgs e)
    {
      var combine = Path.Combine(Path.GetTempPath(), typeof(App).Namespace + $"_{DateTime.Now:yyyyMMdd_HHmmss}.log");
      var logFile = File.CreateText(combine);
      logFile.AutoFlush = true;
      ILogger logger = new TextLogger(logFile);
      Exit += (_, _) => logFile.Close();

      var loggerContext = Logger.Context;
      logger.Info($"{loggerContext} version={typeof(App).Assembly.GetName().Version.ToString(3)}");

      const string checkForVersion = "--check-for-version=";
      const string baseUri = "--base-uri=";
      const string checkIntervalInSec = "--check-interval-in-sec=";
      var options = new Options();
      foreach (var arg in e.Args)
        try
        {
          if (arg.StartsWith(checkForVersion))
          {
            var version = Version.Parse(arg.Substring(checkForVersion.Length));
            if (version.Major != VersionControl.MajorVersion)
              throw new Exception($"Invalid major version, expect {VersionControl.MajorVersion}");
            options.CheckForVersion = version;
          }
          else if (arg.StartsWith(baseUri))
            options.BaseUri = new Uri(arg.Substring(baseUri.Length), UriKind.Absolute);
          else if (arg.StartsWith(checkIntervalInSec))
          {
            var value = ulong.Parse(arg.Substring(checkIntervalInSec.Length));
            const ulong minUpdateIntervalInSec = 15;
            if (value < minUpdateIntervalInSec)
              throw new Exception($"Too small update interval {value}, not less then {minUpdateIntervalInSec} is expected");
            options.CheckInterval = TimeSpan.FromSeconds(value);
          }
          else
            throw new ArgumentOutOfRangeException($"Unknown command line argument {arg}");
        }
        catch (Exception ex)
        {
          logger.Exception(ex);
        }

      var singleRunEvent = new EventWaitHandle(true, EventResetMode.ManualReset, "JB_EtwHostServiceNotifier." + VersionControl.MajorVersion, out var createdNew);
      Exit += (_, _) => singleRunEvent.Close();
      if (!createdNew)
      {
        logger.Info($"{loggerContext} res=exit_already_run");
        Current.Shutdown(1);
        return;
      }

      MainWindow = new MainWindow(logger, options);
    }
  }
}