using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Notifier.Util;

namespace JetBrains.Etw.HostService.Notifier.SharedStorage
{
  internal sealed class FileSharedStorage<TValue> : ISharedStorage<TValue>
  {
    private readonly string myFile;
    private readonly Func<string, TValue> myFromValue;
    private readonly ILogger myLogger;
    private readonly Func<TValue, string> myToValue;

    public FileSharedStorage([NotNull] ILogger logger, [NotNull] string file, [NotNull] Func<string, TValue> fromValue, [NotNull] Func<TValue, string> toValue)
    {
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
      myFromValue = fromValue ?? throw new ArgumentNullException(nameof(fromValue));
      myToValue = toValue ?? throw new ArgumentNullException(nameof(toValue));
      myFile = Environment.ExpandEnvironmentVariables(file);
    }

    bool ISharedStorage<TValue>.GetValue(out TValue value)
    {
      try
      {
        if (Path.IsPathRooted(myFile) && File.Exists(myFile))
        {
          var text = File.ReadAllText(myFile);
          value = myFromValue(text);
          return true;
        }
      }
      catch (Exception e)
      {
        myLogger.Exception(e);
      }

      value = default;
      return false;
    }

    void ISharedStorage<TValue>.SetValue(TValue value)
    {
      try
      {
        if (Path.IsPathRooted(myFile))
        {
          var dir = Path.GetDirectoryName(myFile);
          if (dir != null)
            Directory.CreateDirectory(dir);
          var text = myToValue(value);
          File.WriteAllText(myFile, text, Encoding.UTF8);
        }
      }
      catch (Exception e)
      {
        myLogger.Exception(e);
      }
    }
  }
}