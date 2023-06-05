using JetBrains.Annotations;
using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers.Structs.Params;

/// <summary>
///     Settings to pass to buffer search mechanisms.
/// </summary>
[PublicAPI]
public struct BufferSearchSettings
{
    /// <summary>
    ///     Minimum address of the allocation.
    /// </summary>
    public required nuint MinAddress { get; init; } = 0;

    /// <summary>
    ///     Maximum address of the allocation.
    /// </summary>
    public required nuint MaxAddress { get; init; } = Cached.GetMaxAddress();

    /// <summary>
    ///     Required size of the data.
    /// </summary>
    public required uint Size { get; init; } = 4096;

    /// <summary>
    ///     Initializes the buffer allocator with default settings.
    /// </summary>
    public BufferSearchSettings() { }

    /// <summary>
    ///     Creates settings such that the returned buffer will always be within <paramref name="proximity"/> bytes of <paramref name="target"/>.
    /// </summary>
    /// <param name="proximity">Max proximity (number of bytes) to target.</param>
    /// <param name="target">Target address.</param>
    /// <param name="size">Size required in the settings.</param>
    /// <returns>Settings that would satisfy this search.</returns>
    public static BufferSearchSettings FromProximity(nuint proximity, nuint target, nuint size) => new()
    {
        MaxAddress = Mathematics.AddWithOverflowCap(target, proximity),
        MinAddress = Mathematics.SubtractWithUnderflowCap(target, proximity),
        Size = (uint)size
    };
}
