using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using static Reloaded.Memory.Buffers.Native.OSX.Mach;

namespace Reloaded.Memory.Buffers;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// Windows specific buffer allocator.
/// </summary>
public static partial class BufferAllocator
{
    // Devirtualized based on target.
    private static LocatorItem AllocateOSX(BufferAllocatorSettings settings)
    {
        var maxAddress = Math.Min(Cached.GetMaxAddress(), settings.MaxAddress);
        nuint currentAddress = settings.MinAddress;

        var selfTask = mach_task_self();
        for (int x = 0; x < settings.RetryCount; x++)
        {
            // Until we get all of the pages.
            // ReSharper disable once RedundantCast
            foreach (var page in GetFreePages(currentAddress, (nuint)maxAddress, selfTask))
            {
                if (TryAllocateBuffer(page.addr, page.size, settings, selfTask, out var item))
                    return item;
            }
        }

        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }

    private static List<(nuint addr, nuint size)> GetFreePages(nuint minAddress, nuint maxAddress, nuint selfTask)
    {
        var result = new List<(nuint addr, nuint size)>();
        uint infoCount = VM_REGION_BASIC_INFO_COUNT;
        var currentAddress = minAddress;

        // Until we get all of the pages.
        while (currentAddress <= maxAddress)
        {
            var actualAddress = currentAddress;
            nuint availableSize = 0;
            int kr = mach_vm_region(selfTask, ref actualAddress, ref availableSize, VM_REGION_BASIC_INFO_64, out vm_region_basic_info_64 _, ref infoCount, out _);

            // KERN_INVALID_ADDRESS, i.e. no more regions.
            if (kr == 1)
            {
                var padding = maxAddress - currentAddress;
                if (padding > 0)
                    result.Add((currentAddress, padding));

                break;
            }

            // Any other error.
            if (kr != 0)
                break;

            var freeBytes = actualAddress - currentAddress;
            if (freeBytes > 0)
                result.Add((currentAddress, freeBytes));

            currentAddress = actualAddress + availableSize;
        }

        return result;
    }

    private static unsafe bool TryAllocateBuffer(nuint pageAddress, nuint pageSize,
        BufferAllocatorSettings settings, nuint selfTask, out LocatorItem result)
    {
        result = default;
        Span<nuint> results = stackalloc nuint[4];
        foreach (var addr in GetBufferPointersInPageRange(pageAddress, pageSize, (int)settings.Size, settings.MinAddress, settings.MaxAddress, results))
        {
            int kr = mach_vm_allocate(selfTask, (nuint)(&addr), settings.Size, 0);

            if (kr != 0)
                continue;

            result = new LocatorItem(addr, settings.Size);
            return true;
        }

        return false;
    }

    private static Span<nuint> GetBufferPointersInPageRange(nuint pageAddress, nuint pageSize, int bufferSize, nuint minimumPtr, nuint maximumPtr, Span<nuint> results)
    {
        nuint pageStart = pageAddress;
        nuint pageEnd = pageAddress + pageSize;
        int allocationGranularity = Cached.GetAllocationGranularity();
        return GetPossibleBufferAddresses(minimumPtr, maximumPtr, pageStart, pageEnd, (nuint) bufferSize, allocationGranularity, results);
    }
}
