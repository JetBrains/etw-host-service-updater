using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal static unsafe class KernelExtensions
  {
    /// <summary>
    /// Retrieves a pseudo handle for the current process.
    /// </summary>
    /// <returns>The return value is a pseudo handle to the current process.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = false, ExactSpelling = true)]
    private static extern void* GetCurrentProcess();

    /// <summary>
    /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process. 
    /// To avoid the race conditions described in the Remarks section, use the GetModuleHandleEx function.
    /// </summary>
    /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). 
    /// If the file name extension is omitted, the default library extension .dll is appended. 
    /// The file name string can include a trailing point character (.) to indicate that the module name has no extension. 
    /// The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). 
    /// The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process. 
    /// If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file). 
    /// The GetModuleHandle function does not retrieve handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag. 
    /// For more information, see LoadLibraryEx.</param>
    /// <returns>If the function succeeds, the return value is a handle to the specified module. 
    /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
    private static extern void* GetModuleHandleW(string lpModuleName);

    /// <summary>
    /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
    /// </summary>
    /// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary or GetModuleHandle function returns this handle.</param>
    /// <param name="lpProcName">The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
    /// <returns>If the function succeeds, the return value is the address of the exported function or variable. If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
    private static extern void* GetProcAddress(void* hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

    /// <summary>
    /// Determines whether the specified process is running under WOW64; also returns additional machine process and architecture information.
    /// Available since Windows 10.0 Build 1511.
    /// <code>
    /// | OS    | Process | pProcessMachine                    | pNativeMachine                   |
    /// +=======+=========+====================================+==================================+
    /// | ARM64 | ARM64   | 0x0000(IMAGE_FILE_MACHINE_UNKNOWN) | 0xAA64(IMAGE_FILE_MACHINE_ARM64) |
    /// | ARM64 | ARM     | 0x01C4(IMAGE_FILE_MACHINE_ARMNT  ) | 0xAA64(IMAGE_FILE_MACHINE_ARM64) |
    /// | ARM64 | X64     | 0x0000(IMAGE_FILE_MACHINE_UNKNOWN) | 0xAA64(IMAGE_FILE_MACHINE_ARM64) | ATTENTION: Returns 0xAA64 !!! 
    /// | ARM64 | X86     | 0x014C(IMAGE_FILE_MACHINE_I386   ) | 0xAA64(IMAGE_FILE_MACHINE_ARM64) |
    /// | X64   | X64     | 0x0000(IMAGE_FILE_MACHINE_UNKNOWN) | 0x8664(IMAGE_FILE_MACHINE_AMD64) |
    /// | X64   | X86     | 0x014C(IMAGE_FILE_MACHINE_I386   ) | 0x8664(IMAGE_FILE_MACHINE_AMD64) |
    /// | X86   | X86     | 0x0000(IMAGE_FILE_MACHINE_UNKNOWN) | 0x014C(IMAGE_FILE_MACHINE_I386 ) |
    /// </code>
    /// </summary>
    /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
    /// <param name="pProcessMachine">On success, returns a pointer to an IMAGE_FILE_MACHINE_* value. The value will be IMAGE_FILE_MACHINE_UNKNOWN if the target process is not a WOW64 process; otherwise, it will identify the type of WoW process.</param>
    /// <param name="pNativeMachine">On success, returns a pointer to a possible IMAGE_FILE_MACHINE_* value identifying the native architecture of host system.</param>
    /// <returns>If the function succeeds, the return value is a nonzero value. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
    private static extern Int32 IsWow64Process2(void* hProcess, MachineId* pProcessMachine, MachineId* pNativeMachine);

    private delegate Int32 IsWow64Process2Delegate(void* hProcess, MachineId* pProcessMachine, MachineId* pNativeMachine);

    private static readonly IsWow64Process2Delegate ourIsWow64Process2;

    static KernelExtensions()
    {
      var hModule = GetModuleHandleW("kernel32.dll");
      if (hModule != null)
      {
        var pIsWow64Process2 = GetProcAddress(hModule, nameof(IsWow64Process2));
        if (pIsWow64Process2 != null)
          ourIsWow64Process2 = (IsWow64Process2Delegate)Marshal.GetDelegateForFunctionPointer((IntPtr)pIsWow64Process2, typeof(IsWow64Process2Delegate));
      }
    }

    // ReSharper disable once InconsistentNaming
    public static Architecture GetOSArchitecture()
    {
      // Note(ww898): GetNativeSystemInfo() on Windows ARM64 returns 9(PROCESSOR_ARCHITECTURE_AMD64) instead of 12(PROCESSOR_ARCHITECTURE_ARM64) for X64 processes!!! So, I should use Kernel32Dll.IsWow64Process2() to detect real OS architecture.
      if (ourIsWow64Process2 == null)
        return RuntimeInformation.OSArchitecture;
      MachineId processMachine, nativeMachine;
      if (ourIsWow64Process2(GetCurrentProcess(), &processMachine, &nativeMachine) == 0)
        throw new Win32Exception();
      return nativeMachine switch
        {
          MachineId.IMAGE_FILE_MACHINE_I386 => Architecture.X86,
          MachineId.IMAGE_FILE_MACHINE_AMD64 => Architecture.X64,
          MachineId.IMAGE_FILE_MACHINE_ARMNT => Architecture.Arm,
          MachineId.IMAGE_FILE_MACHINE_ARM64 => Architecture.Arm64,
          _ => throw new PlatformNotSupportedException()
        };
    }

    [NotNull]
    public static string ToPresentableString(this Architecture architecture) => architecture switch
      {
        Architecture.X86 => "x86",
        Architecture.X64 => "x64",
        Architecture.Arm => "ARM",
        Architecture.Arm64 => "ARM64",
        _ => throw new PlatformNotSupportedException($"Unsupported architecture {architecture}")
      };
  }
}