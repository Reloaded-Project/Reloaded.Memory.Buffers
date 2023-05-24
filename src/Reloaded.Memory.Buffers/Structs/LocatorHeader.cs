using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Reloaded.Memory.Buffers.Structs;

/// <summary>
///     Represents the header of an individual memory locator.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct LocatorHeader
{
    /// <summary>
    /// Static length of this locator.
    /// </summary>
    public const int Length = 4096;

    /// <summary>
    /// Returns the maximum possible amount of items in this locator.
    /// </summary>
    public static int MaxItemCount => (Length - sizeof(LocatorHeader)) / sizeof(LocatorItem);

    /// <summary>
    ///     Address of this header in memory.
    /// </summary>
    public LocatorHeader* ThisAddress;

    /// <summary>
    ///     Address of next locator in memory.
    /// </summary>
    public LocatorHeader* NextLocatorPtr;

    /// <summary>
    ///     True if this locator is locked, else false.
    /// </summary>
    private int _isLocked;

    /// <summary>
    ///     Returns true if the current item is locked, else false.
    /// </summary>
    public bool IsLocked => _isLocked == 1;

    private byte _flags;

    /// <summary>
    ///     Version represented by the first 3 bits of _flags.
    /// </summary>
    public byte Version
    {
        get => (byte)(_flags & 0x07);
        set => _flags = (byte)((_flags & 0xF8) | (value & 0x07));
    }

    /// <summary>
    ///     Number of items in this buffer.
    /// </summary>
    public byte NumItems;

    /// <summary>
    ///     True if next locator is present.
    /// </summary>
    public bool HasNextLocator => NextLocatorPtr != null;

    /// <summary>
    /// True if this buffer is full.
    /// </summary>
    public bool IsFull => NumItems >= MaxItemCount;

    /// <summary>
    ///     Tries to acquire the lock.
    /// </summary>
    /// <returns>True if the lock was successfully acquired, false otherwise.</returns>
    public bool TryLock() => Interlocked.CompareExchange(ref _isLocked, 1, 0) == 0;

    /// <summary>
    ///     Acquires the lock, blocking until it can do so.
    /// </summary>
    public void Lock()
    {
        while (!TryLock())
        {
            // We're using `SpinWait` to implement a backoff strategy, which can provide better performance
            // than a simple busy loop when contention for the lock is high.
            Thread.SpinWait(100);
        }
    }

    /// <summary>
    ///     Unlocks the object in a thread-safe manner.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     If the buffer is already unlocked, this error is thrown.
    ///     It is only thrown in debug mode.
    /// </exception>
    public void Unlock()
    {
        // Set _isLocked to 0 and return the original value.
        // ReSharper disable once UnusedVariable
        var original = Interlocked.Exchange(ref _isLocked, 0);

        // If the original value was already 0, something went wrong.
#if DEBUG
        if (original == 0)
            throw new InvalidOperationException("Attempted to unlock a LocatorHeader that wasn't locked");
#endif
    }

    /// <summary>
    /// Gets the item at a specific index.
    /// </summary>
    public LocatorItem* GetFirstItem() => (LocatorItem*)((LocatorHeader*)Unsafe.AsPointer(ref this) + 1);

    /// <summary>
    /// Gets the item at a specific index.
    /// </summary>
    /// <param name="index">Index to get item at.</param>
    public LocatorItem* GetItem(int index) => GetFirstItem() + index;

    /// <summary>
    /// Gets the item at a specific index, with lock.
    /// </summary>
    /// <param name="index">Index to get item at.</param>
    /// <returns>Locked locator item. Make sure this is disposed with use of 'using' statement. Disposing will release lock.</returns>
    public SafeLocatorItem GetItemLocked(int index)
    {
        var item = GetItem(index);
        item->Lock();
        return new SafeLocatorItem(item);
    }

    /// <summary>
    /// Gets the first available, with lock.
    /// </summary>
    /// <param name="size">Required size of buffer.</param>
    /// <param name="minAddress">Minimum address for the allocation.</param>
    /// <param name="maxAddress">Maximum address for the allocation.</param>
    /// <returns>Locked locator item. Make sure this is disposed with use of 'using' statement. Disposing will release lock.</returns>
    public SafeLocatorItem? GetFirstAvailableItemLocked(uint size, nuint minAddress, nuint maxAddress)
    {
        ref var currentItem = ref Unsafe.AsRef<LocatorItem>(GetFirstItem());
        ref var finalItem = ref Unsafe.Add(ref currentItem, NumItems);
        while (Unsafe.IsAddressLessThan(ref currentItem, ref finalItem))
        {
            if (currentItem.CanUse(size, minAddress, maxAddress) && currentItem.TryLock())
                return new SafeLocatorItem((LocatorItem*)Unsafe.AsPointer(ref currentItem));

            currentItem = ref Unsafe.Add(ref currentItem, 1);
        }

        return null;
    }
}
