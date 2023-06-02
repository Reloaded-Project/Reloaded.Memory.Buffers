using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Enums;
using Reloaded.Memory.Native.Unix;
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

        // Define the necessary variables for mach_vm_region
        nuint address = currentAddress;
        nuint size = 0;

        int flavor = VM_REGION_BASIC_INFO_64;
        uint infoCount = VM_REGION_BASIC_INFO_COUNT;
        var selfTask = mach_task_self();

        for (int x = 0; x < settings.RetryCount; x++)
        {
            // Until we get all of the pages.
            while (currentAddress <= maxAddress)
            {
                int kr = mach_vm_region(selfTask, ref address, ref size, flavor, out var info, ref infoCount, out _);
                if (kr != 0)
                    break;

                if (TryAllocateBuffer(ref info, address, size, settings, selfTask, out var item))
                    return item;

                currentAddress = address + size;
                address = currentAddress;
            }
        }

        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }

    private static bool TryAllocateBuffer(ref vm_region_basic_info_64 pageInfo, nuint pageAddress, nuint pageSize,
        BufferAllocatorSettings settings, int selfTask, out LocatorItem result)
    {
        result = default;
        if (pageInfo.protection != VM_PROT_NONE)
            return false;

        Span<nuint> results = stackalloc nuint[4];
        foreach (var addr in GetBufferPointersInPageRange(pageAddress, pageSize, (int)settings.Size, settings.MinAddress, settings.MaxAddress, results))
        {
            int kr = mach_vm_allocate(selfTask, addr, settings.Size, 0);

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
