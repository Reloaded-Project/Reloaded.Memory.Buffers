using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Native.Unix;
using static Reloaded.Memory.Buffers.Native.Windows.Kernel32;

namespace Reloaded.Memory.Buffers.Utilities;

/// <summary>
///     Retrieves cached values that can be reused.
/// </summary>
internal static class Cached
{
    private static readonly nuint s_maxAddress;
    private static readonly int s_allocationGranularity;
    private const int ScPagesizeLinux = 30;
    private const int ScPagesizeOsx = 29;

    static Cached()
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
        else if (Polyfills.IsLinux())
        {
            s_maxAddress = unchecked((nuint)(-1));
            s_allocationGranularity = (int)Posix.sysconf(ScPagesizeLinux);
        }
        else if (Polyfills.IsMacOS())
        {
            s_maxAddress = unchecked((nuint)(-1));
            s_allocationGranularity = (int)Posix.sysconf(ScPagesizeOsx);
        }
        else
        {
            ThrowHelpers.ThrowPlatformNotSupportedException();
        }
        #pragma warning restore CA1416 // Validate platform compatibility

    }

    public static nuint GetMaxAddress() => s_maxAddress;

    public static int GetAllocationGranularity() => s_allocationGranularity;
}
