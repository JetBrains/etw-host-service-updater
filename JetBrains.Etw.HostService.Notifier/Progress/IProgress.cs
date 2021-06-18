namespace JetBrains.Etw.HostService.Notifier.Progress
{
  public interface IProgress
  {
    void Start(double totalUnits);
    void Advance(double deltaUnits);
    void Stop();
  }
}