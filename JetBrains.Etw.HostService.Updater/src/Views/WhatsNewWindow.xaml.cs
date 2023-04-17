using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using JetBrains.Etw.HostService.Updater.ViewModel;

namespace JetBrains.Etw.HostService.Updater.Views
{
  internal partial class WhatsNewWindow : Window
  {
    private readonly ILogger myLogger;
    private readonly WhatsNewViewModel myViewModel = new();

    public WhatsNewWindow([NotNull] ILogger logger, [NotNull] UpdateRequest updateRequest)
    {
      if (updateRequest == null) throw new ArgumentNullException(nameof(updateRequest));
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));

      myViewModel.SetHtml(updateRequest.WhatsNewHtml);

      InitializeComponent();
      DataContext = myViewModel;

      SourceInitialized += (_, _) => this.HideMinimizeAndMaximizeButtons();
    }

    private void OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
      if (e.Uri != null)
      {
        myLogger.Info(Logger.Context);
        e.Cancel = true;
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
      }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      // Tab      : Cycle forward to the next tab 
      // Space    : Scroll page down 
      // PageUp   : Scroll page up 
      // PageDown : Scroll page down 
      // Home     : Scroll to top of page 
      // End      : Scroll to bottom of page 
      // Up       : Scroll up 
      // Down     : Scroll down
      if (Keyboard.Modifiers == ModifierKeys.None && e.Key is Key.Tab or Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.Space) return;

      // Ctrl+C     : Copy
      // Ctrl+A     : Select all
      // Ctrl+Enter : Open link
      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.C or Key.A or Key.Enter) return;

      // Shift+Space : Scroll page up 
      // Shift+Tab   : Cycle forward to the previous tab 
      if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key is Key.Space or Key.Tab) return;

      e.Handled = true;
    }
  }
}