using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal sealed class LicenseItemViewModel
  {
    public LicenseItemViewModel([NotNull] string packageId, [NotNull] string packageVersion, [NotNull] string licenseName, [NotNull] Uri licenseUri, [NotNull] Uri projectUri)
    {
      PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
      PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
      LicenseName = licenseName ?? throw new ArgumentNullException(nameof(licenseName));
      LicenseUri = licenseUri ?? throw new ArgumentNullException(nameof(licenseUri));
      ProjectUri = projectUri ?? throw new ArgumentNullException(nameof(projectUri));
    }

    public string PackageId { get; }
    public string PackageVersion { get; }
    public Uri LicenseUri { get; }
    public string LicenseName { get; }
    public Uri ProjectUri { get; }
  }
}