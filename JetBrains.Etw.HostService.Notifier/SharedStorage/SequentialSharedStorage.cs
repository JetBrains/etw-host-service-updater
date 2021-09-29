using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Notifier.SharedStorage
{
  internal sealed class SequentialSharedStorage<TValue> : ISharedStorage<TValue>
  {
    private readonly ISharedStorage<TValue>[] myStorages;

    public SequentialSharedStorage([NotNull] params ISharedStorage<TValue>[] storages)
    {
      myStorages = storages ?? throw new ArgumentNullException(nameof(storages));
    }

    bool ISharedStorage<TValue>.GetValue(out TValue value)
    {
      foreach (var storage in myStorages)
        if (storage.GetValue(out value))
          return true;

      value = default;
      return false;
    }

    void ISharedStorage<TValue>.SetValue(TValue value)
    {
      foreach (var storage in myStorages)
        storage.SetValue(value);
    }
  }
}