// ReSharper disable InterpolatedStringExpressionIsNotIFormattable
namespace Reloaded.Memory.Buffers.Native.Linux;

/// <summary>
///     Individual entry in Linux memory map.
/// </summary>
internal struct MemoryMapEntry
{
    /// <summary>
    ///     Gets or sets the start address of the memory mapping.
    /// </summary>
    public nuint StartAddress { get; set; }

    /// <summary>
    ///     Gets or sets the end address of the memory mapping.
    /// </summary>
    public nuint EndAddress { get; set; }

    /// <summary>
    ///     Returns a string representation of the memory mapping entry.
    /// </summary>
    /// <returns>A string representation of the memory mapping entry.</returns>
    public override string ToString()
        => $"Start: 0x{StartAddress:X}, End: 0x{EndAddress:X}";
}
