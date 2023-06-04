using System.Diagnostics;
using JetBrains.Annotations;
using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers.Structs.Params;

/// <summary>
///     Settings to pass to the buffer allocator.
/// </summary>
[PublicAPI]
public struct BufferAllocatorSettings
{
    /// <summary>
    ///     Minimum address of the allocation.
    /// </summary>
    public required nuint MinAddress { get; set; } = 0;

    /// <summary>
    ///     Maximum address of the allocation.
    /// </summary>
    public required nuint MaxAddress { get; set; } = uint.MaxValue;

    /// <summary>
    ///     Required size of the data.
    /// </summary>
    public required uint Size { get; set; } = 4096;

    /// <summary>
    ///     Process to allocate memory in.
    /// </summary>
    public Process TargetProcess { get; init; } = Cached.GetThisProcess();

    /// <summary>
    ///     Amount of times library should retry after failing to allocate a region.
    /// </summary>
    /// <remarks>
    ///     This is useful when there's high memory pressure, meaning pages become unavailable between the time
    ///     they are found and the time we try to allocate them.
    /// </remarks>
    public int RetryCount = 8;

    /// <summary>
    ///     Whether to use brute force to find a suitable address.
    /// </summary>
    /// <remarks>
    ///     This for some reason only ever was needed in FFXIV under Wine; and was contributed in the original library
    ///     (prior to rewrite) by the Dalamud folks. In Wine and on FFXIV *only*; the regular procedure of trying to allocate
    ///     returned pages doesn't always work. This is a last ditch workaround for that.<br/>
    ///
    ///     This setting is only used on Windows targets today.
    /// </remarks>
    public bool BruteForce = true;

    /// <summary>
    ///     Initializes the buffer allocator with default settings.
    /// </summary>
    public BufferAllocatorSettings() { }

    /// <summary>
    /// Sanitizes the input values.
    /// </summary>
    public void Sanitize()
    {
        // On Windows, VirtualAlloc treats 0 as 'any address', we might aswell avoid this out the gate.
        if (Polyfills.IsWindows() && MinAddress < (ulong)Cached.GetAllocationGranularity())
            MinAddress = (nuint)Cached.GetAllocationGranularity();

        Size = (uint)Mathematics.RoundUp(Size, Cached.GetAllocationGranularity());
    }
}
