namespace Reloaded.Memory.Buffers.Structs;

/// <summary>
/// An individual item in the buffer locator that you can dispose.
/// </summary>
public unsafe struct SafeLocatorItem : IDisposable
{
    /// <summary>
    /// The item behind this struct.
    /// </summary>
    public LocatorItem* Item;

    /// <summary>
    /// Creates a disposable locator item.
    /// </summary>
    /// <param name="item">The item to dispose.</param>
    /// <remarks>
    ///     Item used with this constructor must be locked.
    /// </remarks>
    public SafeLocatorItem(LocatorItem* item) => Item = item;

    /// <summary>
    /// Appends the data to this buffer.
    /// </summary>
    /// <param name="data">The data to append to the item.</param>
    /// <remarks>
    ///     It is the caller's responsibility to ensure there is sufficient space in the buffer.<br/>
    ///     When returning buffers from the library, the library will ensure there's at least the requested amount of space;
    ///     so if the total size of your data falls under that space, you are good.
    /// </remarks>
    public nuint Append(Span<byte> data) => Item->Append(data);

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
    public nuint Append<T>(in T data) where T : unmanaged => Item->Append(data);

    /// <inheritdoc />
    public void Dispose() => Item->Unlock();
}
