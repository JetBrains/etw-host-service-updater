using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util;

/// <summary>
///   PROF-1642: Old ETW Host Service installers fail to uninstall when the updater scheduled task is absent, so a
///   placeholder task has to be created before the new installer is started. The new installer recreates the real task.
/// </summary>
internal static class ScheduledTask
{
  private static readonly TimeSpan SchTasksTimeout = TimeSpan.FromMinutes(2);
  private static readonly string UpdaterTaskName = "ETW Host Service Updater v" + VersionControl.MajorVersion;

  public static void EnsureUpdaterTaskExists([NotNull] ILogger logger)
  {
    if (logger == null)
      throw new ArgumentNullException(nameof(logger));
    try
    {
      if (RunSchTasks(logger, $"/Query /TN \"{UpdaterTaskName}\"") == 0)
        return;

      // Note: The placeholder does nothing and is disabled. It doesn't require administrative rights.
      var exitCode = RunSchTasks(logger, $"/Create /TN \"{UpdaterTaskName}\" /TR \"cmd.exe /c exit 0\" /SC ONCE /ST 00:00 /F");
      if (exitCode != 0)
        throw new Exception($"Failed to create the placeholder scheduled task \"{UpdaterTaskName}\", schtasks.exe exit code {exitCode}");

      exitCode = RunSchTasks(logger, $"/Change /TN \"{UpdaterTaskName}\" /DISABLE");
      if (exitCode != 0)
        throw new Exception($"Failed to disable the placeholder scheduled task \"{UpdaterTaskName}\", schtasks.exe exit code {exitCode}");
    }
    catch (Exception e)
    {
      // Note: Don't block the installation, the installer may still succeed.
      logger.Exception(e);
    }
  }

  private static int RunSchTasks([NotNull] ILogger logger, [NotNull] string arguments)
  {
    using var process = Process.Start(new ProcessStartInfo
    {
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
      Arguments = arguments
    });
    if (process == null)
      throw new Exception("Failed to run schtasks.exe");
    // Note: Both streams are read asynchronously, otherwise the synchronous read would block without the time limit.
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    if (!process.WaitForExit((int)SchTasksTimeout.TotalMilliseconds) || !Task.WaitAll(new Task[] { output, error }, SchTasksTimeout))
    {
      try
      {
        process.Kill();
      }
      catch (Exception e)
      {
        logger.Exception(e);
      }

      throw new TimeoutException($"schtasks.exe {arguments} didn't finish in {SchTasksTimeout}");
    }

    logger.Info($"{Logger.Context} args={arguments} exitCode={process.ExitCode} stdout={output.Result.Trim()} stderr={error.Result.Trim()}");
    return process.ExitCode;
  }
}