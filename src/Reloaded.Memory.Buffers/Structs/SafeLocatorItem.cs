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
    public SafeLocatorItem(LocatorItem* item) => Item = item;

    /// <inheritdoc />
    public void Dispose() => Item->Unlock();
}
