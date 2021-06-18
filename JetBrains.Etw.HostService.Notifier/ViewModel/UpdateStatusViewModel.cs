using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.Util;

namespace JetBrains.Etw.HostService.Notifier.ViewModel
{
  public sealed class UpdateStatusViewModel : INotifyPropertyChanged
  {
    private readonly Dispatcher myDispatcher;
    private int myModalCounter;

    public UpdateStatusViewModel()
    {
      myDispatcher = Dispatcher.CurrentDispatcher;
      SetUpdateRequest(null);
    }

    public bool IsNotModalState => myModalCounter == 0;
    public bool HasUpdate { get; private set; }
    public string UpdateMessage { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public IDisposable RunModalDialog()
    {
      Interlocked.Increment(ref myModalCounter);
      myDispatcher.BeginInvoke(new Action(() => OnPropertyChanged(nameof(IsNotModalState))));

      return new RunUpdateDisposable(() =>
        {
          Interlocked.Decrement(ref myModalCounter);
          myDispatcher.BeginInvoke(new Action(() => OnPropertyChanged(nameof(IsNotModalState))));
        });
    }

    public void SetUpdateRequest([CanBeNull] UpdateChecker.Result updateRequest)
    {
      myDispatcher.BeginInvoke(new Action(() =>
        {
          if (updateRequest != null)
          {
            var version = updateRequest.Version;
            UpdateMessage = $"The new version {version.Major}.{version.Minor} is available for downloading!";
            HasUpdate = true;
          }
          else
          {
            HasUpdate = false;
            UpdateMessage = "No update available";
          }

          OnPropertyChanged(nameof(UpdateMessage));
          OnPropertyChanged(nameof(HasUpdate));
        }));
    }

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RunUpdateDisposable : IDisposable
    {
      private readonly Action myOnDispose;

      public RunUpdateDisposable([NotNull] Action onDispose)
      {
        myOnDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
      }

      public void Dispose()
      {
        myOnDispose();
      }
    }
  }
}