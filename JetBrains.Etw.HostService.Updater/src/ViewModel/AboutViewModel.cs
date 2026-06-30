using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using JetBrains.HabitatDetector;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal sealed class AboutViewModel : INotifyPropertyChanged
  {
    private const int FirstYear = 2021;

    public string ToolVersion => App.ToolVersion;
    public string ProcessArchitecture => HabitatInfo.ProcessArchitecture.ToPresentableString();

    public IEnumerable<LicenseItemViewModel> Licenses
    {
      get
      {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "JetBrains.Etw.HostService.Updater.third-party-libraries.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
          throw new InvalidOperationException($"Failed to find resource {resourceName}");

        using var document = JsonDocument.Parse(stream);
        return document.RootElement.EnumerateArray().Select(element =>
        {
          var name = element.GetStringPropertyEx("name");
          var version = element.GetStringPropertyEx("version");
          var url = element.GetAbsoluteUriPropertyEx("url");
          var license = element.GetStringPropertyEx("license");
          var licenseUrl = element.GetAbsoluteUriPropertyEx("licenseUrl");
          return new LicenseItemViewModel(name, version, license, licenseUrl, url);
        }).ToList();
      }
    }

    public string YearRange
    {
      get
      {
        var builder = new StringBuilder().Append(FirstYear);
        var nowYear = DateTime.Now.Year;
        if (nowYear > FirstYear)
          builder.Append('-').Append(nowYear);
        return builder.ToString();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}