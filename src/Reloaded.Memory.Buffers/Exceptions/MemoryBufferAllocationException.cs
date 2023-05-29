using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers.Exceptions;

/// <summary>
/// This is an exception thrown when memory allocation of a specified number of bytes in a specified address range fails.
/// </summary>
public class MemoryBufferAllocationException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="MemoryBufferAllocationException"/>.
    /// </summary>
    /// <param name="end">The end of the address range used in allocation.</param>
    /// <param name="size">Size of the data to allocate in.</param>
    /// <param name="start">The start of the address range used in allocation.</param>
    public MemoryBufferAllocationException(nuint start, nuint end, int size)
        : base($"Failed to allocate {size} bytes in address range {start:X8} - {end:X8}.") { }

    /// <inheritdoc />
    public MemoryBufferAllocationException() : base() { }

    /// <inheritdoc />
    public MemoryBufferAllocationException(string message) : base(message) { }

    /// <inheritdoc />
    public MemoryBufferAllocationException(string message, Exception innerException) : base(message, innerException) { }
}
