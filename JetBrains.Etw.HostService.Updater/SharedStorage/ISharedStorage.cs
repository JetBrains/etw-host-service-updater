namespace JetBrains.Etw.HostService.Updater.SharedStorage
{
  internal interface ISharedStorage<TValue>
  {
    bool GetValue(out TValue value);
    void SetValue(TValue value);
  }
}