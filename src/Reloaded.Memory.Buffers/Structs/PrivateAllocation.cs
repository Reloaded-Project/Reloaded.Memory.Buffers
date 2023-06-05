using System.Diagnostics;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Structs;

namespace Reloaded.Memory.Buffers.Structs;

/// <summary>
///     Provides information about a recently made allocation.
/// </summary>
/// <remarks>
///     This memory is automatically disposed by Garbage Collector if this class is no longer in use,
///     if you wish to keep it around forever, make sure to store it in a field or use `<see cref="GC.KeepAlive"/>`.
/// </remarks>
public class PrivateAllocation : IDisposable
{
    /// <summary>
    ///     Address of the buffer in memory.
    /// </summary>
    public nuint BaseAddress { get; }

    /// <summary>
    ///     Exact size of allocated data.
    /// </summary>
    public uint Size { get; }

    private bool _isDisposed;
    private readonly Action _free;

    /// <summary>
    ///     Creates a private allocation returned to user upon allocating a region of memory.
    /// </summary>
    /// <param name="baseAddress"></param>
    /// <param name="size"></param>
    /// <param name="process"></param>
    public PrivateAllocation(nuint baseAddress, nuint size, Process process)
    {
        BaseAddress = baseAddress;
        Size = (uint)size;
        _free = GetDisposeMethod(process);
        _isDisposed = false;
    }

    /// <inheritdoc />
    ~PrivateAllocation() => ReleaseUnmanagedResources();

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        if (_isDisposed)
            return;

        _free.Invoke();
        _isDisposed = true;
    }

    private Action GetDisposeMethod(Process process)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        if (process.Id == Polyfills.GetProcessId())
            return () => Memory.Instance.Free(new MemoryAllocation { Address =BaseAddress, Length = Size });

        // Note: Action holds reference to Process so it doesn't get disposed.
        return () => new ExternalMemory(process).Free(new MemoryAllocation { Address =BaseAddress, Length = Size });
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
