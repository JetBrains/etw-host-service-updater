using System;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  public class UpdateRequest
  {
    public Uri ChecksumLink;
    public Uri Link;
    public Uri SignedChecksumLink;
    public Version Version;
    public string WhatsNewHtml;
  }
}