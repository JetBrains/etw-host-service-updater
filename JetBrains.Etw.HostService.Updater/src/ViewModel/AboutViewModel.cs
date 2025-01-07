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
            new("AngleSharp"                     , "0.17.1"  , mitName + " (2013-2024 AngleSharp)"                                  , mitUri),
            new("AngleSharp.Css"                 , "0.17.0"  , mitName + " (2013-2024 AngleSharp)"                                  , mitUri),
            new("BouncyCastle.Cryptography"      , "2.4.0"   , mitName + " (2000-2024 Legion of the Bouncy Castle Inc.)"            , mitUri),
            new("Hardcodet.NotifyIcon.Wpf"       , "2.0.1"   , mitName + " (Philipp Sumi)"                                          , mitUri),
            new("HtmlSanitizer"                  , "8.1.870" , mitName + " (2013-2016 Michael Ganss and HtmlSanitizer contributors)", mitUri),
            new("JetBrains.Annotations"          , "2024.3.0", mitName + " (2016-2024 JetBrains s.r.o.)"                            , mitUri),
            new("JetBrains.DownloadPgpVerifier"  , "1.0.1"   , apache20Name                                                         , apache20Uri),
            new("JetBrains.FormatRipper"         , "2.2.2"   , apache20Name                                                         , apache20Uri),
            new("JetBrains.HabitatDetector"      , "1.4.3"   , apache20Name                                                         , apache20Uri),
            new("WixToolset.Dtf.WindowsInstaller", "5.0.2"   , "Microsoft Reciprocal License"                                       , new Uri("https://spdx.org/licenses/MS-RL.html")),
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