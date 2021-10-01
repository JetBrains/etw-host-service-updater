using System.Windows;
using System.Windows.Controls;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal static class WebBrowserBehavior
  {
    public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
      "Html",
      typeof(string),
      typeof(WebBrowserBehavior),
      new FrameworkPropertyMetadata(OnHtmlChanged));

    [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
    public static string GetHtml(WebBrowser control)
    {
      return (string) control.GetValue(HtmlProperty);
    }

    public static void SetHtml(WebBrowser control, string value)
    {
      control.SetValue(HtmlProperty, value);
    }

    private static void OnHtmlChanged(DependencyObject control, DependencyPropertyChangedEventArgs e)
    {
      if (control is WebBrowser webBrowserControl && e.NewValue is string html)
        webBrowserControl.NavigateToString(html);
    }
  }
}