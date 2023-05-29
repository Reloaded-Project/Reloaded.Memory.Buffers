using System.Globalization;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Reloaded.Memory.Buffers.Utilities;

internal static class Polyfills
{
#if !NET5_0_OR_GREATER
    private static int? s_processId;
#endif
    public static int GetProcessId()
    {
#if NET5_0_OR_GREATER
        return Environment.ProcessId;
#else
        if (s_processId != null)
            return s_processId.Value;

        s_processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        return s_processId.Value;
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

    /// <summary>
    ///     Returns true if the current operating system is Linux.
    /// </summary>
    public static bool IsLinux()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsLinux();
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif
    }

    /// <summary>
    ///     Returns true if the current operating system is MacOS.
    /// </summary>
    public static bool IsMacOS()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsMacOS();
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
    }

    /// <summary>
    ///     Parses a hex address from the string provided..
    /// </summary>
    /// <param name="text">The text to parse the address from.</param>
    /// <returns>Parsed address.</returns>
    /// <remarks>Limited to 64-bit on older frameworks.</remarks>
    public static nuint ParseHexAddress(ReadOnlySpan<char> text)
    {
        #if NET6_0_OR_GREATER
        return nuint.Parse(text, NumberStyles.HexNumber);
        #else
        return (nuint)ulong.Parse(text.ToString(), NumberStyles.HexNumber);
        #endif
    }
}
