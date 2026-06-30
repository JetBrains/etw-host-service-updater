using System.Linq;
using JetBrains.Etw.HostService.Updater.ViewModel;
using NUnit.Framework;

namespace JetBrains.Etw.HostService.Updater.Tests
{
  [TestFixture]
  public class AboutViewModelTests
  {
    [Test]
    public void LicensesLoadTest()
    {
      var viewModel = new AboutViewModel();
      var licenses = viewModel.Licenses.ToList();

      Assert.That(licenses, Is.Not.Null);
      Assert.That(licenses.Count, Is.GreaterThanOrEqualTo(10),
        "Should parse at least 10 licenses from third-party-libraries.json");

      var angleSharp = licenses.FirstOrDefault(l => l.PackageId == "AngleSharp");
      Assert.That(angleSharp, Is.Not.Null);
      Assert.That(angleSharp.PackageVersion, Is.EqualTo("0.17.1"));
      Assert.That(angleSharp.LicenseName, Is.EqualTo("MIT License (2013-2024 AngleSharp)"));
      Assert.That(angleSharp.LicenseUri.ToString(), Is.EqualTo("https://spdx.org/licenses/MIT.html"));
      Assert.That(angleSharp.ProjectUri.ToString(), Is.EqualTo("https://github.com/AngleSharp/AngleSharp"));
    }
  }
}