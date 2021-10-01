using System;

namespace JetBrains.Etw.HostService.Updater.Util
{
  public interface ILogger : DownloadPgpVerifier.ILogger
  {
    void Exception(Exception e);
  }
}