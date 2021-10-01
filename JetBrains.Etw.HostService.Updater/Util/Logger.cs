using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util
{
  public static class Logger
  {
    public const string Delimiter = "::";

    [NotNull]
    public static string Context
    {
      [MethodImpl(MethodImplOptions.NoInlining)]
      get
      {
        var method = new StackFrame(1).GetMethod().NotNull();
        return $"{method.DeclaringType?.Name ?? ""}{Delimiter}{method.Name.NotNull()}";
      }
    }
  }
}