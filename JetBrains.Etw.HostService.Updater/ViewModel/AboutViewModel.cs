using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal sealed class AboutViewModel : INotifyPropertyChanged
  {
    private const int FirstYear = 2021;

    public string ToolVersion => App.ToolVersion;
    public string ProcessArchitecture => RuntimeInformation.ProcessArchitecture.ToPresentableString();

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