using System.Globalization;
using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Extensions;
using static Reloaded.Memory.Buffers.Utilities.Mathematics;

namespace Reloaded.Memory.Buffers;

/// <summary>
///     Class for allocating buffers between given memory ranges inside the current process.
/// </summary>
public static partial class BufferAllocator
{
    /// <summary>
    ///     Allocates a region of memory that satisfies the given parameters.
    /// </summary>
    /// <param name="settings">Settings for the memory allocator.</param>
    /// <returns>Native address of the allocated region.</returns>
    public static LocatorItem Allocate(BufferAllocatorSettings settings)
    {
        settings.Sanitize();
        if (Polyfills.IsWindows())
            return AllocateWindows(settings);
        else if (Polyfills.IsLinux())
            return AllocateLinux(settings);
        else if (Polyfills.IsMacOS())
            return AllocateOSX(settings);

        ThrowHelpers.ThrowPlatformNotSupportedException();
        return default;
    }

    internal static Span<nuint> GetPossibleBufferAddresses(nuint minimumPtr, nuint maximumPtr, nuint pageStart, nuint pageEnd,
        nuint bufSize, int allocationGranularity, Span<nuint> results)
    {
        // Get range for page and min-max region.
        var minMaxRange = new AddressRange(minimumPtr, maximumPtr);
        var pageRange = new AddressRange(pageStart, pageEnd);

        // Check if there is any overlap at all.
        if (!pageRange.Overlaps(minMaxRange))
            return default;

        // Three possible cases here:
        //   1. Page fits entirely inside min-max range and is smaller.
        if (bufSize > pageRange.Size)
            return default; // does not fit.

        int numItems = 0;

        // Note: We have to test aligned to both page boundaries and min-max range boundaries;
        //       because, they may not perfectly overlap, e.g. min-max may be way greater than
        //       page size, so testing from start/end of that will not even overlap with available pages.
        //       Or the opposite can happen... min-max range may be smaller than page size.

        //   2. Min-max range is inside page, test aligned to page boundaries.

        // Round up from page min.
        nuint pageMinAligned = RoundUp(pageRange.StartPointer, allocationGranularity);
        var pageMinRange = new AddressRange(pageMinAligned, AddWithOverflowCap(pageMinAligned, bufSize));

        if (pageRange.Contains(pageMinRange) && minMaxRange.Contains(pageMinRange))
            results.DangerousGetReferenceAt(numItems++) = pageMinRange.StartPointer;

        // Round down from page max.
        nuint pageMaxAligned = RoundDown(SubtractWithUnderflowCap(pageRange.EndPointer, bufSize), allocationGranularity);
        var pageMaxRange = new AddressRange(pageMaxAligned, pageMaxAligned + bufSize);

        if (pageRange.Contains(pageMaxRange) && minMaxRange.Contains(pageMaxRange))
            results.DangerousGetReferenceAt(numItems++) = pageMaxRange.StartPointer;

        //   3. Min-max range is inside page, test aligned to Min-max range.

        // Round up from ptr min.
        nuint ptrMinAligned = RoundUp(minimumPtr, allocationGranularity);
        var ptrMinRange = new AddressRange(ptrMinAligned, AddWithOverflowCap(ptrMinAligned, bufSize));

        if (pageRange.Contains(ptrMinRange) && minMaxRange.Contains(ptrMinRange))
            results.DangerousGetReferenceAt(numItems++) = ptrMinRange.StartPointer;

        // Round down from ptr max.
        nuint ptrMaxAligned = RoundDown(SubtractWithUnderflowCap(maximumPtr, bufSize), allocationGranularity);
        var ptrMaxRange = new AddressRange(ptrMaxAligned, ptrMaxAligned + bufSize);

        if (pageRange.Contains(ptrMaxRange) && minMaxRange.Contains(ptrMaxRange))
            results.DangerousGetReferenceAt(numItems++) = ptrMaxRange.StartPointer;

        return results.SliceFast(0, numItems);
    }
}
