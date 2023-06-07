using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Structs.Params;

namespace Reloaded.Memory.Buffers;

/// <summary>
///     Provides the high level API for Reloaded.Memory.Buffers.
/// </summary>
public static class Buffers
{
    /// <summary>
    ///     Allocates some memory with user specified settings.<br />
    ///     The allocated memory is for your use only.
    /// </summary>
    /// <param name="settings">Settings with which to allocate the memory.</param>
    /// <returns>Information about the recently made allocation.</returns>
    /// <remarks>
    ///     Allocating inside another process is only supported on Windows.
    /// </remarks>
    public static PrivateAllocation AllocatePrivateMemory(BufferAllocatorSettings settings)
    {
        LocatorItem alloc = BufferAllocator.Allocate(settings);
        return new PrivateAllocation(alloc.BaseAddress, alloc.Size, settings.TargetProcess);
    }

    /// <summary>
    ///     Gets a buffer with user specified requirements.
    /// </summary>
    /// <param name="settings">Settings with which to allocate the memory.</param>
    /// <returns>
    ///     Item allowing you to write to the buffer.<br/>
    ///     Make sure you dispose it, by using `using` statement.
    /// </returns>
    /// <remarks>Allocating inside another process is only supported on Windows.</remarks>
    /// <exception cref="MemoryBufferAllocationException">
    ///     Memory cannot be allocated within the needed constraints when there
    ///     is no existing suitable buffer.
    /// </exception>
    public static unsafe SafeLocatorItem GetBuffer(BufferSearchSettings settings) => GetBufferRecursive(settings, LocatorHeaderFinder.Find());

    private static unsafe SafeLocatorItem GetBufferRecursive(BufferSearchSettings settings, LocatorHeader* locator)
    {
        SafeLocatorItem? item =
            locator->GetFirstAvailableItemLocked(settings.Size, settings.MinAddress, settings.MaxAddress);
        if (item != null)
            return item.Value;

        if (locator->TryAllocateItem(settings.Size, settings.MinAddress, settings.MaxAddress, out item))
            return item.Value;

        return GetBufferRecursive(settings, locator->GetNextLocator());
    }
}
