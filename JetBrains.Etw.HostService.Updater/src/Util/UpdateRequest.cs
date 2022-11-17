﻿using System;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal class UpdateRequest
  {
    public Uri ChecksumLink;
    public Uri Link;
    public Uri SignedChecksumLink;
    public long Size;
    public Version Version;
    public string WhatsNewHtml;
  }
}