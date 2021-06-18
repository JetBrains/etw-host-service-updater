using System;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  public interface ILogger : DownloadPgpVerifier.ILogger
  {
    void Exception(Exception e);
  }
}