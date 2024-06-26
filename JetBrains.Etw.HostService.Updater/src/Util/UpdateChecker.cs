﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using JetBrains.DownloadPgpVerifier;
using JetBrains.HabitatDetector;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal static class UpdateChecker
  {
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum Channels
    {
      RTM = 0x1,
      RC = 0x2,
      EAP = 0x4
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
      Channels channels = Channels.RTM)
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
          { "arch", HabitatInfo.OSArchitecture.ToPresentableString() }
        });
      var checkUri = new Uri(baseUri.ToDirectoryUri(), "products?" + query);
      logger.Info($"{loggerContext} checkUri={checkUri}");

      return checkUri.OpenStreamFromWeb(stream =>
        {
          var releases = GetReleaseTypes(channels);
          var downloads = new[]
            {
              HabitatInfo.OSRuntimeIdString,
              HabitatInfo.OSArchitecture switch
                {
                  JetArchitecture.X86 => "windows32",
                  JetArchitecture.X64 => "windows64",
                  JetArchitecture.Arm64 => "windowsARM64",
                  _ => throw new PlatformNotSupportedException($"Unsupported OS architecture {HabitatInfo.OSArchitecture.ToPresentableString()}")
                }
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

    [ItemNotNull]
    private static IReadOnlyCollection<string> GetReleaseTypes(Channels channels)
    {
      var res = new List<string>();
      if ((channels & Channels.RTM) != 0) res.Add("release");
      if ((channels & Channels.RC) != 0) res.Add("rc");
      if ((channels & Channels.EAP) != 0) res.Add("eap");
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