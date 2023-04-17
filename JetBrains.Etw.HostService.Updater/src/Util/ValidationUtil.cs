using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal static class ValidationUtil
  {
    [NotNull]
    public static TValue NotNull<TValue>([CanBeNull] this TValue value) where TValue : class
    {
      if (value == null)
        throw new NullReferenceException();
      return value;
    }
  }
}