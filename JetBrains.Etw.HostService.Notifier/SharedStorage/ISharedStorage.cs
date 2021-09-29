namespace JetBrains.Etw.HostService.Notifier.SharedStorage
{
  internal interface ISharedStorage<TValue>
  {
    bool GetValue(out TValue value);
    void SetValue(TValue value);
  }
}