using System.Diagnostics;
using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Native.Unix;
using static Reloaded.Memory.Buffers.Native.Windows.Kernel32;
// ReSharper disable RedundantOverflowCheckingContext

namespace Reloaded.Memory.Buffers.Utilities;

/// <summary>
///     Retrieves cached values that can be reused.
/// </summary>
internal static class Cached
{
    private static readonly nuint s_maxAddress;
    private static readonly int s_allocationGranularity;
    private static readonly Process s_thisProcess;
    private const int ScPagesizeLinux = 30;
    private const int ScPagesizeOsx = 29;

    static unsafe Cached()
    {
        #pragma warning disable CA1416 // Validate platform compatibility
        if (Polyfills.IsWindows())
        {
            GetSystemInfo(out SYSTEM_INFO info);
            s_maxAddress = info.lpMaximumApplicationAddress;
            s_allocationGranularity = (int)info.dwAllocationGranularity;
        }
        // Note: On POSIX, applications are aware of full address space by default.
        // Technically a chunk of address space is reserved for kernel, however for our use case that's not a concern.
        // Note 2: There is no API on Linux (or OSX) to get max address; so we'll restrict to signed 48-bits on x64 for now.
        else if (Polyfills.IsLinux())
        {
            s_maxAddress = sizeof(nuint) == 4 ? unchecked((nuint)(-1)) : unchecked((nuint)0x7FFFFFFFFFFFL);
            s_allocationGranularity = (int)Posix.sysconf(ScPagesizeLinux);
        }
        else if (Polyfills.IsMacOS())
        {
            s_maxAddress = sizeof(nuint) == 4 ? unchecked((nuint)(-1)) : unchecked((nuint)0x7FFFFFFFFFFFL);
            s_allocationGranularity = (int)Posix.sysconf(ScPagesizeOsx);
        }
        else
        {
            ThrowHelpers.ThrowPlatformNotSupportedException();
        }

        s_thisProcess = Process.GetCurrentProcess();
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public static nuint GetMaxAddress() => s_maxAddress;

    public static int GetAllocationGranularity() => s_allocationGranularity;

    public static Process GetThisProcess() => s_thisProcess;
}
