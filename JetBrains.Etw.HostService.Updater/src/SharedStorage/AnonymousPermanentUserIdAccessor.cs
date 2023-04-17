using System;
using JetBrains.Annotations;
using JetBrains.Etw.HostService.Updater.Util;
using Microsoft.Win32;

namespace JetBrains.Etw.HostService.Updater.SharedStorage
{
  internal sealed class AnonymousPermanentUserIdAccessor
  {
    private readonly ISharedStorage<Guid> myStorage;

    public AnonymousPermanentUserIdAccessor([NotNull] ILogger logger)
    {
      myStorage = new SequentialSharedStorage<Guid>(
        new FileSharedStorage<Guid>(logger, @"%AppData%\JetBrains\PermanentUserId", x => new Guid(x), x => x.ToString("D")),
        new RegistrySharedStorage<Guid>(logger, Registry.CurrentUser, @"SOFTWARE\JavaSoft\Prefs\jetbrains", "user_id_on_machine", x => new Guid((string)x), x => x.ToString("D")),
        new RegistrySharedStorage<Guid>(logger, Registry.CurrentUser, @"SOFTWARE\JetBrains\Platform", "InstallationId", x => new Guid((string)x), x => x.ToString("D")));
    }

    public Guid GetOrGenerate()
    {
      if (!myStorage.GetValue(out var value))
      {
        value = Guid.NewGuid();
        myStorage.SetValue(value);
      }

      return value;
    }
  }
}