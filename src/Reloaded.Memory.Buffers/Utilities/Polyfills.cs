#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Reloaded.Memory.Buffers.Utilities;

internal static class Polyfills
{
    public static int GetProcessId()
    {
#if NET5_0_OR_GREATER
        return Environment.ProcessId;
#else
        return System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
    }

    // The OS identifier platform code below is JIT friendly; compiled out at runtime for .NET 5 and above.

    /// <summary>
    ///     Returns true if the current operating system is Windows.
    /// </summary>
    public static bool IsWindows()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsWindows();
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }
}
