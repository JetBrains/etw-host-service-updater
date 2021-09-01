using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  internal static class UriUtil
  {
    [NotNull]
    public static Uri ToDirectoryUri([NotNull] this Uri uri)
    {
      if (uri == null) throw new ArgumentNullException(nameof(uri));
      var uriStr = uri.AbsoluteUri;
      if (uriStr.EndsWith("/"))
        return uri;
      return new Uri(uriStr + '/', UriKind.Absolute);
    }
  }
}