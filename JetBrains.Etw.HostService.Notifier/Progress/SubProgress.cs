using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Notifier.Progress
{
  public sealed class SubProgress : BaseProgress
  {
    private readonly IProgress myParent;
    private readonly double myParentUnits;

    public SubProgress([NotNull] IProgress parent, double parentUnits)
    {
      if (parentUnits <= 0 || double.IsInfinity(parentUnits) || double.IsNaN(parentUnits)) throw new ArgumentOutOfRangeException(nameof(parentUnits));
      myParent = parent ?? throw new ArgumentNullException(nameof(parent));
      myParentUnits = parentUnits;
    }

    public override void Advance(double deltaUnits)
    {
      base.Advance(deltaUnits);
      myParent.Advance(deltaUnits * myParentUnits / TotalUnits);
    }
  }
}