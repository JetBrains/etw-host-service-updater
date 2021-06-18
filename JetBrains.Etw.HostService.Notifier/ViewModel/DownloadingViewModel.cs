using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.Progress;

namespace JetBrains.Etw.HostService.Notifier.ViewModel
{
  internal sealed class DownloadingViewModel : BaseProgress, INotifyPropertyChanged
  {
    private readonly Dispatcher myDispatcher = Dispatcher.CurrentDispatcher;

    public double Progress { get; private set; }
    public string ProgressStr => $"{100 * Progress:F1}%";

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override void Start(double totalUnits)
    {
      myDispatcher.BeginInvoke(new Action(() =>
        {
          base.Start(totalUnits);
          Progress = Fraction;
          OnPropertyChanged(nameof(Progress));
          OnPropertyChanged(nameof(ProgressStr));
        }));
    }

    public override void Advance(double deltaUnits)
    {
      myDispatcher.BeginInvoke(new Action(() =>
        {
          base.Advance(deltaUnits);
          Progress = Fraction;
          OnPropertyChanged(nameof(Progress));
          OnPropertyChanged(nameof(ProgressStr));
        }));
    }

    public override void Stop()
    {
      myDispatcher.BeginInvoke(new Action(() =>
        {
          base.Stop();
          Progress = Fraction;
          OnPropertyChanged(nameof(Progress));
          OnPropertyChanged(nameof(ProgressStr));
        }));
    }
  }
}