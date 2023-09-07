using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Native;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers.Internal;

/// <summary>
///     Class that locates the buffer locator.
/// </summary>
internal static unsafe partial class LocatorHeaderFinder
{
    private static LocatorHeader* s_locatorHeaderAddress;
    private static IMemoryMappedFile? s_mmf;
    private static object _lock = new();

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
        if (s_locatorHeaderAddress != (LocatorHeader*)0)
        {
            reason = FindReason.Cached;
            return s_locatorHeaderAddress;
        }

        // Create or open the memory-mapped file
        lock (_lock)
        {
            IMemoryMappedFile mmf = OpenOrCreateMemoryMappedFile();

            // If the MMF previously existed, we need to read the real address from the header, then close
            // our mapping.
            if (mmf.AlreadyExisted)
            {
                s_locatorHeaderAddress = ((LocatorHeader*)mmf.Data)->ThisAddress;
                reason = FindReason.PreviouslyExisted;
                mmf.Dispose();
                return s_locatorHeaderAddress;
            }

            Cleanup();
            s_mmf = mmf;
            s_locatorHeaderAddress = (LocatorHeader*)mmf.Data;
            s_locatorHeaderAddress->Initialize(mmf.Length);
            reason = FindReason.Created;
            return s_locatorHeaderAddress;
        }
    }

    internal static IMemoryMappedFile OpenOrCreateMemoryMappedFile()
    {
        var name = $"/Reloaded.Memory.Buffers.MemoryBuffer, PID {Polyfills.GetProcessId().ToString()}";

#pragma warning disable CA1416
        if (Polyfills.IsWindows())
            return new WindowsMemoryMappedFile(name, Cached.GetAllocationGranularity());

        if (Polyfills.IsMacOS() || Polyfills.IsLinux())
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
        s_locatorHeaderAddress = (LocatorHeader*)0;
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
