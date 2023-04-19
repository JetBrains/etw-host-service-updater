using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using JetBrains.HabitatDetector;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal sealed class AboutViewModel : INotifyPropertyChanged
  {
    private const int FirstYear = 2021;

    public string ToolVersion => App.ToolVersion;
    public string ProcessArchitecture => HabitatInfo.ProcessArchitecture.ToPresentableString();

    public IEnumerable<LicenseItemViewModel> Licenses => new LicenseItemViewModel[]
      {
        // @formatter:off
        new("Hardcodet.NotifyIcon.Wpf"       , "1.1.0"  , "Code Project Open License 1.02"              , new Uri("https://spdx.org/licenses/CPOL-1.02.html")),
        new("HtmlSanitizer"                  , "8.0.645", "MIT License (Michael Ganss and Contributors)", new Uri("https://spdx.org/licenses/MIT.html")),
        new("JetBrains.DownloadPgpVerifier"  , "1.0.0"  , "Apache License 2.0"                          , new Uri("https://spdx.org/licenses/Apache-2.0.html")),
        new("JetBrains.FormatRipper"         , "2.0.1"  , "Apache License 2.0"                          , new Uri("https://spdx.org/licenses/Apache-2.0.html")),
        new("JetBrains.HabitatDetector"      , "1.0.2"  , "Apache License 2.0"                          , new Uri("https://spdx.org/licenses/Apache-2.0.html")),
        new("System.Text.Json"               , "7.0.2"  , "MIT License (Microsoft Corporation)"         , new Uri("https://spdx.org/licenses/MIT.html")),
        new("WixToolset.Dtf.WindowsInstaller", "4.0.0"  , "Microsoft Reciprocal License"                , new Uri("https://spdx.org/licenses/MS-RL.html")),
        // @formatter:on
      };

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