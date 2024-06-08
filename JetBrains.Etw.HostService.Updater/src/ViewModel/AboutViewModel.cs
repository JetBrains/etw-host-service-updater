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

    public IEnumerable<LicenseItemViewModel> Licenses
    {
      get
      {
        const string mitName = "MIT License";
        const string apache20Name = "Apache License 2.0";
        var mitUri = new Uri("https://spdx.org/licenses/MIT.html");
        var apache20Uri = new Uri("https://spdx.org/licenses/Apache-2.0.html");
        return new LicenseItemViewModel[]
          {
            // @formatter:off
            new("BouncyCastle.Cryptography"      , "2.4.0"   , "MIT License (2000-2024 Legion of the Bouncy Castle Inc.)", mitUri),
            new("Hardcodet.NotifyIcon.Wpf"       , "1.1.0"   , "Code Project Open License 1.02"                          , new Uri("https://spdx.org/licenses/CPOL-1.02.html")),
            new("HtmlSanitizer"                  , "8.0.865" , mitName + " (2013-2024 Michael Ganss)"                    , mitUri),
            new("JetBrains.Annotations"          , "2023.3.0", mitName + " (2016-2024 JetBrains s.r.o.)"                 , mitUri),
            new("JetBrains.DownloadPgpVerifier"  , "1.0.0"   , apache20Name                                              , apache20Uri),
            new("JetBrains.FormatRipper"         , "2.2.1"   , apache20Name                                              , apache20Uri),
            new("JetBrains.HabitatDetector"      , "1.4.1"   , apache20Name                                              , apache20Uri),
            new("System.Text.Json"               , "8.0.3"   , mitName + " (Microsoft Corporation)"                      , mitUri),
            new("WixToolset.Dtf.WindowsInstaller", "5.0.0"   , "Microsoft Reciprocal License"                            , new Uri("https://spdx.org/licenses/MS-RL.html")),
            // @formatter:on
          };
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