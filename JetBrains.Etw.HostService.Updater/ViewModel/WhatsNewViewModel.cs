using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Ganss.Xss;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.ViewModel
{
  internal sealed class WhatsNewViewModel : INotifyPropertyChanged
  {
    private readonly Dispatcher myDispatcher;

    private readonly HtmlSanitizer mySanitizer = new(new HtmlSanitizerOptions
      {
        AllowedTags = new HashSet<string>
          {
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li",
            "table", "colgroup", "col", "tr", "th", "td",
            "br",
            "p", "a", "b", "i", "u", "q", "em", "sup", "sub",
            "pre", "code", "span"
          },
        AllowedAttributes = new HashSet<string> { "href" },
        UriAttributes = new HashSet<string> { "href" }
      });

    private string myHtml;

    public WhatsNewViewModel()
    {
      myDispatcher = Dispatcher.CurrentDispatcher;
    }

    public string Html => @"<!DOCTYPE html><html><head><style>
* {
  font-family: ""Segoe UI"", Tahoma, sans-serif;
  line-height: 150%;
}
body {
  font-size: 10pt;
}
h1 {
  font-weight: normal;
}
h2 {
  font-weight: normal;
  color: #000;
  margin-top: 1em;
  margin-bottom: -0.2em;
}
h3, h4 {
   font-weight: bold;
   color: #555;
   margin-top: 1em;
   margin-bottom: -0.2em;
}
p {
  margin-top: 0.7em;
  margin-bottom: 0.7em;
}
ul {
  margin-top: -0.3em;
  margin-bottom: 1em;
}
li {
  margin-top: 0;
  margin-bottom: 0.4em;
}
strong, em {
  font-weight: bold;
  color: #555;
}
</style></head><body oncontextmenu=""return false"">" + (myHtml ?? "") + "</body></html>";

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetHtml([NotNull] string html)
    {
      var sanitizedHtml = mySanitizer.Sanitize(html);
      myDispatcher.BeginInvoke(new Action(() =>
        {
          myHtml = sanitizedHtml;
          OnPropertyChanged(nameof(Html));
        }));
    }

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}