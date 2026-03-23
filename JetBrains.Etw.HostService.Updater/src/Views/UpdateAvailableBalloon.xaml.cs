using System;
using System.Windows;
using System.Windows.Controls;

namespace JetBrains.Etw.HostService.Updater.Views
{
  internal partial class UpdateAvailableBalloon : UserControl
  {
    public event EventHandler InstallClicked;
    public event EventHandler WhatsNewClicked;
    public event EventHandler DismissClicked;

    public UpdateAvailableBalloon()
    {
      InitializeComponent();
    }

    private void OnInstall(object sender, RoutedEventArgs e) =>
      InstallClicked?.Invoke(this, EventArgs.Empty);

    private void OnWhatsNew(object sender, RoutedEventArgs e) =>
      WhatsNewClicked?.Invoke(this, EventArgs.Empty);

    private void OnDismiss(object sender, RoutedEventArgs e) =>
      DismissClicked?.Invoke(this, EventArgs.Empty);
  }
}