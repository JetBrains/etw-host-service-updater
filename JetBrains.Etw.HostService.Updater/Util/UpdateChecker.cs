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
          var download = RuntimeInformation.OSArchitecture switch
            {
              Architecture.X86 => "windows32",
              Architecture.X64 => "windows64",
              Architecture.Arm64 => "windowsArm64",
              _ => throw new PlatformNotSupportedException($"Unsupported architecture {RuntimeInformation.OSArchitecture}")
            };

          using var json = JsonDocument.Parse(stream);
          foreach (var productElement in json.RootElement.EnumerateArray())
            try
            {
              var code = productElement.GetPropertyEx("code").GetString();
              if (code != productCode) continue;
              foreach (var releaseElement in productElement.GetPropertyEx("releases").EnumerateArray())
                try
                {
                  var version = releaseElement.GetPropertyEx("version").GetVersion();
                  if (version.Major != productVersion.Major) continue;

                  // Note(ww898): https://youtrack.jetbrains.com/issue/JS-17230
                  var isSecurityCritical = releaseElement.TryGetPropertyEx("isSecurityCritical")?.GetBoolean();
                  if (isSecurityCritical != true) continue;

                  var type = releaseElement.GetPropertyEx("type").GetString();
                  if (releases.All(x => x != type)) continue;
                  
                  // Note(ww898): Expect that versions are in descending order!
                  if (version <= productVersion)
                  {
                    logger.Info($"{loggerContext} res=ignore version={version}");
                    return null;
                  }

                  var whatsNewHtml = releaseElement.GetPropertyEx("whatsnew").GetString();
                  foreach (var downloadProperty in releaseElement.GetPropertyEx("downloads").EnumerateObject())
                    try
                    {
                      if (downloadProperty.Name != download) continue;
                      var downloadElement = downloadProperty.Value;
                      var link = downloadElement.GetPropertyEx("link").GetAbsoluteUri();
                      var size = downloadElement.GetPropertyEx("size").GetInt64();
                      var checksumLink = downloadElement.GetPropertyEx("checksumLink").GetAbsoluteUri();
                      var signedChecksumLink = downloadElement.GetPropertyEx("signedChecksumLink").GetAbsoluteUri();

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
                }
                catch (Exception e)
                {
                  logger.Exception(e);
                }
            }
            catch (Exception e)
            {
              logger.Exception(e);
            }

          logger.Info($"{loggerContext} res=no");
          return null;
        });
    }

    private static JsonElement? TryGetPropertyEx(this JsonElement element, [NotNull] string propertyName)
    {
      if (element.TryGetProperty(propertyName, out var childElement))
        return childElement;
      return null;
    }

    private static JsonElement GetPropertyEx(this JsonElement element, [NotNull] string propertyName)
    {
      if (element.TryGetProperty(propertyName, out var childElement))
        return childElement;
      throw new KeyNotFoundException($"Failed to find property with name {propertyName}");
    }

    [NotNull]
    private static Uri GetAbsoluteUri(this JsonElement element)
    {
      var str = element.GetString();
      if (Uri.TryCreate(str, UriKind.Absolute, out var res))
        return res;
      throw new FormatException($"Failed to parse the absolute URI value {str}");
    }

    [NotNull]
    private static Version GetVersion(this JsonElement element)
    {
      var str = element.GetString();
      if (Version.TryParse(str!, out var res))
        return res;
      throw new FormatException($"Failed to parse the version value {str}");
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
    private static string GetOsArchitecture()
    {
      return RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
    }

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
    private static string ConvertToUriQuery([NotNull] IEnumerable<KeyValuePair<string, string>> queries)
    {
      return queries
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
}