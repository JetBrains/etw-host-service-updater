using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using JetBrains.DownloadPgpVerifier;

namespace JetBrains.Etw.HostService.Updater.Util
{
  public static class UpdateChecker
  {
    [Flags]
    public enum Channels
    {
      Release = 0x1,
      Rc = 0x2,
      Eap = 0x4
    }

    public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromDays(1);
    public static readonly Uri PublicBaseUri = new("https://data.services.jetbrains.com");

    [CanBeNull]
    public static UpdateRequest Check(
      [NotNull] ILogger logger,
      [NotNull] Uri baseUri,
      [NotNull] string productCode,
      [NotNull] Version productVersion,
      Guid anonymousPermanentUserId,
      Channels channels = Channels.Release)
    {
      if (logger == null) throw new ArgumentNullException(nameof(logger));
      if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
      if (productCode == null) throw new ArgumentNullException(nameof(productCode));
      if (productVersion == null) throw new ArgumentNullException(nameof(productVersion));
      if (!baseUri.IsAbsoluteUri) throw new ArgumentOutOfRangeException(nameof(baseUri));

      var loggerContext = Logger.Context;
      logger.Info($"{loggerContext} productCode={productCode} productVersion={productVersion} channel={channels.ToString().Replace(" ", "")}");

      var query = ConvertToUriQuery(new SortedList<string, string>
        {
          { "code", productCode },
          { "majorVersion", productVersion.Major.ToString() },
          { "minorVersion", productVersion.Minor.ToString() },
          { "buildVersion", productVersion.Build.ToString() },
          { "uid", anonymousPermanentUserId.ToString("D") },
          { "os", GetOsName() },
          { "arch", GetOsArchitecture() }
        });
      var checkUri = new Uri(baseUri.ToDirectoryUri(), "products?" + query);
      logger.Info($"{loggerContext} checkUri={checkUri}");

      return checkUri.OpenStreamFromWeb(stream =>
        {
          var releases = GetReleaseTypes(channels);
          var downloads = RuntimeInformation.OSArchitecture switch
            {
              Architecture.X86 => new[] { "windows-x86", "windows32" },
              Architecture.X64 => new[] { "windows-x64", "windows64" },
              Architecture.Arm64 => new[] { "windows-arm64", "windowsARM64" },
              _ => throw new PlatformNotSupportedException($"Unsupported architecture {RuntimeInformation.OSArchitecture}")
            };

          using var json = JsonDocument.Parse(stream);
          foreach (var productElement in json.RootElement.EnumerateArray())
            try
            {
              var code = productElement.GetStringPropertyEx("code");
              if (code != productCode) continue;
              foreach (var releaseElement in productElement.GetPropertyEx("releases").EnumerateArray())
                try
                {
                  var version = releaseElement.GetVersionPropertyEx("version");
                  if (version.Major != productVersion.Major) continue;

                  // Note(ww898): https://youtrack.jetbrains.com/issue/JS-17230
                  var isSecurityCritical = releaseElement.TryGetBooleanPropertyEx("isSecurityCritical");
                  if (isSecurityCritical != true) continue;

                  var type = releaseElement.GetStringPropertyEx("type");
                  if (releases.All(x => x != type)) continue;

                  // Note(ww898): Expect that versions are in descending order!
                  if (version <= productVersion)
                    break;

                  var downloadsProperty = releaseElement.GetPropertyEx("downloads");
                  var downloadProperty = downloads.Select(x => downloadsProperty.TryGetPropertyEx(x)).FirstOrDefault(x => x != null);
                  if (downloadProperty == null) continue;

                  var downloadElement = downloadProperty.Value;
                  var size = downloadElement.GetInt64PropertyEx("size");
                  var link = downloadElement.GetAbsoluteUriPropertyEx("link");
                  var checksumLink = downloadElement.GetAbsoluteUriPropertyEx("checksumLink");
                  var signedChecksumLink = downloadElement.GetAbsoluteUriPropertyEx("signedChecksumLink");

                  var whatsNewHtml = releaseElement.GetStringPropertyEx("whatsnew");

                  logger.Info($"{loggerContext} res=found version={version} size={size}\n\tlink={link}\n\tchecksumLink={checksumLink}\n\tsignedChecksumLink={signedChecksumLink}");
                  
                  return new UpdateRequest
                    {
                      Version = version,
                      Link = link,
                      Size = size,
                      ChecksumLink = checksumLink,
                      SignedChecksumLink = signedChecksumLink,
                      WhatsNewHtml = whatsNewHtml
                    };
                }
                catch (Exception e)
                {
                  logger.Exception(e);
                }

              logger.Info($"{loggerContext} res=no_update_version version={productVersion}");
              return null;
            }
            catch (Exception e)
            {
              logger.Exception(e);
            }

          logger.Info($"{loggerContext} res=no");
          return null;
        });
    }

    [NotNull]
    private static string GetOsName()
    {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        throw new PlatformNotSupportedException();
      var osVersion = Environment.OSVersion.Version;
      return $"Windows {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
    }

    [NotNull]
    private static string GetOsArchitecture() => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

    [ItemNotNull]
    private static IReadOnlyCollection<string> GetReleaseTypes(Channels channels)
    {
      var res = new List<string>();
      if ((channels & Channels.Release) != 0) res.Add("release");
      if ((channels & Channels.Rc) != 0) res.Add("rc");
      if ((channels & Channels.Eap) != 0) res.Add("eap");
      return res;
    }

    [NotNull]
    private static string ConvertToUriQuery([NotNull] IEnumerable<KeyValuePair<string, string>> queries) =>
      queries
        .Aggregate(new StringBuilder(), (builder, pair) =>
          {
            if (builder.Length != 0)
              builder.Append('&');
            return builder
              .Append(WebUtility.UrlEncode(pair.Key))
              .Append('=')
              .Append(WebUtility.UrlEncode(pair.Value));
          })
        .ToString();
  }
}