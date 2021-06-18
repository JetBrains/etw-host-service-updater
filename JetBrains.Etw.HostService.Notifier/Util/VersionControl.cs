using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Deployment.WindowsInstaller;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  public static class VersionControl
  {
    private const int VersionV16 = 16;
    private const string UpgradeCodeV16 = "{25CB994F-CDCF-421B-9156-76528AAFC0E1}";

    [CanBeNull]
    public static Version GetInstalledVersion([NotNull] ILogger logger)
    {
      if (logger == null) throw new ArgumentNullException(nameof(logger));
      IEnumerable<ProductInstallation> productInstallations;
      try
      {
        productInstallations = ProductInstallation.GetRelatedProducts(UpgradeCodeV16);
      }
      catch (ArgumentException)
      {
        productInstallations = Enumerable.Empty<ProductInstallation>();
      }

      var foundVersions = productInstallations.Select(x => x.ProductVersion).OrderByDescending(x => x).ToList();
      logger.Info($"{Logger.Context} upgradeCode={UpgradeCodeV16} versions={string.Join(",", foundVersions.Select(x => x.ToString()))}");

      if (foundVersions.Any(x => x.Major != VersionV16))
        throw new Exception("Unexpected major version");
      return foundVersions.FirstOrDefault();
    }
  }
}