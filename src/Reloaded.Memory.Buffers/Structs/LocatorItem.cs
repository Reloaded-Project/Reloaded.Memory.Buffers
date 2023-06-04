using System.Runtime.InteropServices;

namespace Reloaded.Memory.Buffers.Structs;

/// <summary>
///     Individual item in the locator.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LocatorItem
{
    /// <summary>
    ///     Address of the buffer in memory.
    /// </summary>
    public nuint BaseAddress;

    /// <summary>
    ///     True if the buffer is allocated.
    /// </summary>
    public bool IsAllocated => BaseAddress != 0;

    /// <summary>
    ///     True if this item is locked, else false.
    /// </summary>
    private int _isTaken;

    /// <summary>
    ///     Returns true if the current item is locked, else false.
    /// </summary>
    public bool IsTaken => _isTaken == 1;

    /// <summary>
    ///     Size of the buffer.
    /// </summary>
    public uint Size;

    /// <summary>
    ///     Current position of the buffer.
    /// </summary>
    public uint Position;

    /// <summary>
    ///     Available number of bytes in item.
    /// </summary>
    public uint BytesLeft => Size - Position;

    /// <summary>
    ///     Minimum address of the allocation.
    /// </summary>
    public nuint MinAddress => BaseAddress;

    /// <summary>
    ///     Maximum address of the allocation.
    /// </summary>
    public nuint MaxAddress => BaseAddress + Size;

    /// <summary>
    ///     Creates a new instance of <see cref="LocatorItem" /> given base address, size and position.
    /// </summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="size">The size.</param>
    public LocatorItem(nuint baseAddress, uint size)
    {
        BaseAddress = baseAddress;
        Size = size;
        Position = 0;
    }

    /// <summary>
    ///     Tries to acquire the lock.
    /// </summary>
    /// <returns>True if the lock was successfully acquired, false otherwise.</returns>
    public bool TryLock() => Interlocked.CompareExchange(ref _isTaken, 1, 0) == 0;

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
        // Set _isTaken to 0 and return the original value.
        // ReSharper disable once UnusedVariable
        var original = Interlocked.Exchange(ref _isTaken, 0);

        // If the original value was already 0, something went wrong.
#if DEBUG
        if (original == 0)
            throw new InvalidOperationException("Attempted to unlock a LocatorItem that wasn't locked");
#endif
    }

    /// <summary>
    ///     Determines if this locator item can be used given the constraints.
    /// </summary>
    /// <param name="size">Available bytes between <paramref name="minAddress" /> and <paramref name="maxAddress" />.</param>
    /// <param name="minAddress">Minimum address accepted.</param>
    /// <param name="maxAddress">Maximum address accepted.</param>
    /// <returns>If this buffer can be used given the parameters.</returns>
    public bool CanUse(uint size, nuint minAddress, nuint maxAddress)
    {
        if (!IsAllocated)
            return false;

        // Calculate the start and end positions within the buffer
        var startAvailableAddress = BaseAddress + Position;
        var endAvailableAddress = startAvailableAddress + size;

        // Check if the requested memory lies within the remaining buffer and within the specified address range
        // If any of the checks fail, the buffer can't be used
        // [endAvailableAddress <= MaxAddress] checks if the data can fit.
        // [startAvailableAddress >= minAddress] checks if in range.
        // [endAvailableAddress <= maxAddress] checks if in range.
        return endAvailableAddress <= MaxAddress && startAvailableAddress >= minAddress &&
               endAvailableAddress <= maxAddress;
    }

    /// <summary>
    /// Appends the data to this buffer.
    /// </summary>
    /// <param name="data">The data to append to the item.</param>
    /// <returns>Address of the written data.</returns>
    /// <remarks>
    ///     It is the caller's responsibility to ensure there is sufficient space in the buffer.<br/>
    ///     When returning buffers from the library, the library will ensure there's at least the requested amount of space;
    ///     so if the total size of your data falls under that space, you are good.
    /// </remarks>
    public unsafe nuint Append(Span<byte> data)
    {
        nuint address = BaseAddress + Position;
        data.CopyTo(new Span<byte>((void*)address, data.Length));
        Position += (uint)data.Length;
        return address;
    }

    /// <summary>
    /// Appends the blittable variable to this buffer.
    /// </summary>
    /// <typeparam name="T">Type of the item to write.</typeparam>
    /// <param name="data">The item to append to the buffer.</param>
    /// <returns>Address of the written data.</returns>
    /// <remarks>
    ///     It is the caller's responsibility to ensure there is sufficient space in the buffer.<br/>
    ///     When returning buffers from the library, the library will ensure there's at least the requested amount of space;
    ///     so if the total size of your data falls under that space, you are good.
    /// </remarks>
    public unsafe nuint Append<T>(in T data) where T : unmanaged
    {
        var address = (T*)(BaseAddress + Position);
        *address = data;
        return (nuint)address;
    }
}
