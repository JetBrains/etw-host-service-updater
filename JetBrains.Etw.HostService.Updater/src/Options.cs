﻿using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater
{
  public sealed class Options
  {
    [CanBeNull]
    public Uri BaseUri;

    [CanBeNull]
    public Version CheckForVersion;

    public TimeSpan? CheckInterval;
  }
}