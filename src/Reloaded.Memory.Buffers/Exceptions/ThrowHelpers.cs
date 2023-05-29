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
    public static void ThrowLinuxBadMemoryMapEntry()
        => throw new FormatException("Invalid Memory Map Entry");
}

