using System;
using System.IO;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util
{
  public class TextLogger : ILogger
  {
    private readonly object myLock = new();
    private readonly TextWriter myWriter;

    public TextLogger([NotNull] TextWriter writer)
    {
      myWriter = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void Info(string str)
    {
      lock (myLock)
        myWriter.WriteLine(str);
    }

    public void Warning(string str)
    {
      lock (myLock)
      {
        myWriter.Write("WARNING: ");
        myWriter.WriteLine(str);
      }
    }

    public void Error(string str)
    {
      lock (myLock)
      {
        myWriter.Write("ERROR: ");
        myWriter.WriteLine(str);
      }
    }

    public void Exception(Exception e)
    {
      Error(e.ToString());
    }
  }
}