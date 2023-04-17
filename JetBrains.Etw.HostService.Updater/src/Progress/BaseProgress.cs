using System;

namespace JetBrains.Etw.HostService.Updater.Progress
{
  public abstract class BaseProgress : IProgress
  {
    private bool myIsStarted;
    private double myUnits;

    protected double TotalUnits { get; private set; }

    protected double Fraction => myUnits / TotalUnits;

    public virtual void Start(double totalUnits)
    {
      if (totalUnits <= 0 || double.IsInfinity(totalUnits) || double.IsNaN(totalUnits)) throw new ArgumentOutOfRangeException(nameof(totalUnits));

      if (myIsStarted) throw new InvalidOperationException();
      myIsStarted = true;
      myUnits = 0;
      TotalUnits = totalUnits;
    }

    public virtual void Advance(double deltaUnits)
    {
      if (deltaUnits < 0 || double.IsInfinity(deltaUnits) || double.IsNaN(deltaUnits)) throw new ArgumentOutOfRangeException(nameof(deltaUnits));
      if (!myIsStarted) throw new InvalidOperationException();
      myUnits += deltaUnits;
      if (myUnits > TotalUnits)
        myUnits = TotalUnits;
    }

    public virtual void Stop()
    {
      Advance(TotalUnits - myUnits);
    }
  }
}