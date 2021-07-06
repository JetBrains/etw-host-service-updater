using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  internal static class WindowExtensions
  {
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;
    private const int WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLongW(IntPtr hwnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLongW(IntPtr hwnd, int index, int value);

    public static void HideMinimizeAndMaximizeButtons(this Window window)
    {
      var hwnd = new WindowInteropHelper(window).Handle;
      var style = GetWindowLongW(hwnd, GWL_STYLE);
      SetWindowLongW(hwnd, GWL_STYLE, style & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
    }
  }
}