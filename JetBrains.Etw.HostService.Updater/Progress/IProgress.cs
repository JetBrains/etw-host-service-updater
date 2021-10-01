namespace JetBrains.Etw.HostService.Updater.Progress
{
  public interface IProgress
  {
    void Start(double totalUnits);
    void Advance(double deltaUnits);
    void Stop();
  }
}