using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Native;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers;

/// <summary>
///     Class that locates the buffer locator.
/// </summary>
public static unsafe partial class BufferLocatorFinder
{
    private static LocatorHeader* s_address;
    private static IMemoryMappedFile? s_mmf;

    /// <summary>
    ///     Retrieves the address of the first locator.
    /// </summary>
    /// <returns>Address of the first locator.</returns>
    internal static LocatorHeader* Find() => Find(out _);

    /// <summary>
    ///     Retrieves the address of the first locator.
    /// </summary>
    /// <param name="reason">The reason the element was found.</param>
    /// <returns>Address of the first locator.</returns>
    /// <exception cref="PlatformNotSupportedException">This operation is not supported on the current platform.</exception>
    internal static LocatorHeader* Find(out FindReason reason)
    {
        if (s_address != (LocatorHeader*)0)
        {
            reason = FindReason.Cached;
            return s_address;
        }

        // Create or open the memory-mapped file
        IMemoryMappedFile mmf = OpenOrCreateMemoryMappedFile();

        // If the MMF previously existed, we need to read the real address from the header, then close
        // our mapping.
        if (mmf.AlreadyExisted)
        {
            s_address = ((LocatorHeader*)mmf.Data)->ThisAddress;
            reason = FindReason.PreviouslyExisted;
            mmf.Dispose();
            return s_address;
        }

        Cleanup();
        s_mmf = mmf;
        s_address = (LocatorHeader*)mmf.Data;
        s_address->Initialize(mmf.Length);
        reason = FindReason.Created;
        return s_address;
    }

    internal static IMemoryMappedFile OpenOrCreateMemoryMappedFile()
    {
        var name = $"/Reloaded.Memory.Buffers.MemoryBuffer, PID {Polyfills.GetProcessId().ToString()}";

#pragma warning disable CA1416
        if (Polyfills.IsWindows())
            return new WindowsMemoryMappedFile(name, Cached.GetAllocationGranularity());

        if (Polyfills.IsLinux())
            return new LinuxMemoryMappedFile(name, Cached.GetAllocationGranularity());

        if (Polyfills.IsMacOS())
            return new UnixMemoryMappedFile(name, Cached.GetAllocationGranularity());

        ThrowHelpers.ThrowPlatformNotSupportedException();
        return null!;
#pragma warning restore CA1416
    }

    /// <summary>
    ///     For test purposes only.
    ///     Discards any present views.
    /// </summary>
    internal static void Reset()
    {
        s_address = (LocatorHeader*)0;
        s_mmf?.Dispose();
        s_mmf = null;
    }

    internal enum FindReason
    {
        /// <summary>
        ///     Previously found and cached.
        /// </summary>
        Cached,

        /// <summary>
        ///     Memory mapped file was already opened by someone else.
        /// </summary>
        PreviouslyExisted,

        /// <summary>
        ///     Memory mapped file was newly created (nobody made it before).
        /// </summary>
        Created
    }
}
