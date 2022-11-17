using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using JetBrains.Etw.HostService.Updater.Views;

namespace JetBrains.Etw.HostService.Updater
{
  public partial class App : Application
  {
    [NotNull]
    public static readonly string ToolVersion = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.NotNull();

    private void OnStartup(object sender, StartupEventArgs e)
    {
      var logDir = Path.Combine(Path.GetTempPath(), "JetLogs", "EtwHost");
      Directory.CreateDirectory(logDir);
      
      var combine = Path.Combine(logDir, typeof(App).Namespace + $"_{DateTime.Now:yyyyMMdd_HHmmss}.log");
      var logFile = File.CreateText(combine);
      logFile.AutoFlush = true;
      ILogger logger = new TextLogger(logFile);
      Exit += (_, _) => logFile.Close();

      var loggerContext = Logger.Context;
      logger.Info($"{loggerContext} toolVersion={ToolVersion} toolArchitecture={RuntimeInformation.ProcessArchitecture.ToPresentableString()} osArchitecture={KernelExtensions.GetOSArchitecture().ToPresentableString()}");

      var singleRunEvent = new EventWaitHandle(true, EventResetMode.ManualReset, "JB_EtwHostServiceUpdater." + VersionControl.MajorVersion, out var createdNew);
      Exit += (_, _) => singleRunEvent.Close();
      if (!createdNew)
      {
        logger.Info($"{loggerContext} res=exit_already_run");
        Current.Shutdown(1);
        return;
      }

      const string checkForVersion = "--check-for-version=";
      const string baseUri = "--base-uri=";
      const string checkIntervalInSec = "--check-interval-in-sec=";
      var options = new Options();
      foreach (var arg in e.Args)
        try
        {
          if (arg.StartsWith(checkForVersion))
            options.CheckForVersion = VersionControl.CheckVersion(Version.Parse(arg.Substring(checkForVersion.Length)));
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

      MainWindow = new MainWindow(logger, options);
    }
  }
}