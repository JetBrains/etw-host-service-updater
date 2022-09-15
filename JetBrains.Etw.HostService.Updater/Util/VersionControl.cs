using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Deployment.WindowsInstaller;

namespace JetBrains.Etw.HostService.Updater.Util
{
  public static class VersionControl
  {
    public static readonly int MajorVersion = typeof(App).Assembly.GetName().Version.Major;

    [CanBeNull]
    public static Version GetInstalledVersion([NotNull] ILogger logger)
    {
      if (logger == null) throw new ArgumentNullException(nameof(logger));

      var upgradeCode = MajorVersion switch
        {
          16 => "{25CB994F-CDCF-421B-9156-76528AAFC0E1}",
          _ => throw new ArgumentOutOfRangeException(nameof(MajorVersion), $"Unknown major version {MajorVersion}")
        };

      IEnumerable<ProductInstallation> productInstallations;
      try
      {
        productInstallations = ProductInstallation.GetRelatedProducts(upgradeCode);
      }
      catch (ArgumentException)
      {
        productInstallations = Enumerable.Empty<ProductInstallation>();
      }

      var foundVersions = productInstallations.Select(x => x.ProductVersion).OrderByDescending(x => x).ToList();
      logger.Info($"{Logger.Context} upgradeCode={upgradeCode} versions={string.Join(",", foundVersions.Select(x => x.ToString()))}");

      return foundVersions.Select(CheckVersion).SingleOrDefault();
    }

    [ContractAnnotation("null => null; notnull => notnull")]
    public static Version CheckVersion([CanBeNull] Version version)
    {
      if (version != null)
        if (version.Major != MajorVersion)
          throw new Exception($"Invalid the major version {version.Major}, expect {MajorVersion}");
      return version;
    }
  }
}