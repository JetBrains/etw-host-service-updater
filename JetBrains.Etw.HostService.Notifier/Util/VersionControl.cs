using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Deployment.WindowsInstaller;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  public static class VersionControl
  {
    public const int MajorVersion = 16;

    // Major version | Upgrade code
    // ==============+========================================
    //       16      | {25CB994F-CDCF-421B-9156-76528AAFC0E1}
    //       17      | ???
    private const string UpgradeCode = "{25CB994F-CDCF-421B-9156-76528AAFC0E1}";

    [CanBeNull]
    public static Version GetInstalledVersion([NotNull] ILogger logger)
    {
      if (logger == null) throw new ArgumentNullException(nameof(logger));
      IEnumerable<ProductInstallation> productInstallations;
      try
      {
        productInstallations = ProductInstallation.GetRelatedProducts(UpgradeCode);
      }
      catch (ArgumentException)
      {
        productInstallations = Enumerable.Empty<ProductInstallation>();
      }

      var foundVersions = productInstallations.Select(x => x.ProductVersion).OrderByDescending(x => x).ToList();
      logger.Info($"{Logger.Context} upgradeCode={UpgradeCode} versions={string.Join(",", foundVersions.Select(x => x.ToString()))}");

      if (foundVersions.Any(x => x.Major != MajorVersion))
        throw new Exception("Unexpected major version");
      return foundVersions.FirstOrDefault();
    }
  }
}