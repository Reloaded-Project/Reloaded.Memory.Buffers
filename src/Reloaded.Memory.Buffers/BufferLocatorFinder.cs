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
    private static LocatorHeader* _address;
    private static IMemoryMappedFile? _mmf;

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
    internal static LocatorHeader* Find(out FindReason reason)
    {
        if (_address != (LocatorHeader*)0)
        {
            reason = FindReason.Cached;
            return _address;
        }

        // Create or open the memory-mapped file
        IMemoryMappedFile mmf = OpenOrCreateMemoryMappedFile();

        // If the MMF previously existed, we need to read the real address from the header, then close
        // our mapping.
        if (mmf.AlreadyExisted)
        {
            _address = ((LocatorHeader*)mmf.Data)->ThisAddress;
            reason = FindReason.PreviouslyExisted;
            mmf.Dispose();
            return _address;
        }

        Cleanup();
        _mmf = mmf;
        _address = (LocatorHeader*)mmf.Data;
        _address->Initialize(mmf.Length);
        reason = FindReason.Created;
        return _address;
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
        _address = (LocatorHeader*)0;
        _mmf?.Dispose();
        _mmf = null;
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
