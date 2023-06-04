using System.Runtime.CompilerServices;

namespace Reloaded.Memory.Buffers.Exceptions;

/// <summary>
///     Helper class for throwing exceptions.
/// </summary>
internal abstract class ThrowHelpers
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowPlatformNotSupportedException()
        => throw new PlatformNotSupportedException("Operating System in use is not supported.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowExternalAllocationNotSupportedException()
        => throw new PlatformNotSupportedException("Allocating memory in external process is not supported on this platform.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowLinuxBadMemoryMapEntry()
        => throw new FormatException("Invalid Memory Map Entry");
}

