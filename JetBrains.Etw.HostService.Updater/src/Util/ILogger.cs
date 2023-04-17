using System;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal interface ILogger : DownloadPgpVerifier.ILogger
  {
    void Exception(Exception e);
  }
}