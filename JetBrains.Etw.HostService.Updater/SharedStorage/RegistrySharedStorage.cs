using System;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using Microsoft.Win32;

namespace JetBrains.Etw.HostService.Updater.SharedStorage
{
  internal sealed class RegistrySharedStorage<TValue> : ISharedStorage<TValue>
  {
    private readonly Func<object, TValue> myFromValue;
    private readonly ILogger myLogger;
    private readonly string myName;
    private readonly string myPath;
    private readonly RegistryKey myRoot;
    private readonly Func<TValue, object> myToValue;

    public RegistrySharedStorage([NotNull] ILogger logger, [NotNull] RegistryKey root, [NotNull] string path, [CanBeNull] string name, [NotNull] Func<object, TValue> fromValue, [NotNull] Func<TValue, object> toValue)
    {
      myLogger = logger ?? throw new ArgumentNullException(nameof(logger));
      myRoot = root ?? throw new ArgumentNullException(nameof(root));
      myPath = path ?? throw new ArgumentNullException(nameof(path));
      myName = name;
      myFromValue = fromValue ?? throw new ArgumentNullException(nameof(fromValue));
      myToValue = toValue ?? throw new ArgumentNullException(nameof(toValue));
    }

    bool ISharedStorage<TValue>.GetValue(out TValue value)
    {
      try
      {
        using var key = myRoot.OpenSubKey(myPath);
        var rawValue = key?.GetValue(myName);
        if (rawValue != null)
        {
          value = myFromValue(rawValue);
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
        using var key = myRoot.CreateSubKey(myPath, true);
        key.SetValue(myName, myToValue(value));
      }
      catch (Exception e)
      {
        myLogger.Exception(e);
      }
    }
  }
}