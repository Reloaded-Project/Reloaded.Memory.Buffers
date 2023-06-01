using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Native.Linux;
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

        for (int x = 0; x < settings.RetryCount; x++)
        {
            // Until we get all of the pages.
            while (currentAddress <= maxAddress)
            {
                int kr = mach_vm_region(mach_task_self(), ref address, ref size, flavor, out var info, ref infoCount, out int objectName);
                if (kr != 0)
                    break;

                if (TryAllocateBuffer(ref info, address, size, settings, out var item))
                    return item;

                currentAddress = address + size;
                address = currentAddress;
            }
        }

        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }

    private static bool TryAllocateBuffer(ref vm_region_basic_info_64 pageInfo, nuint pageAddress, nuint pageSize, BufferAllocatorSettings settings, out LocatorItem result)
    {
        result = default;
        if (pageInfo.protection != VM_PROT_NONE)
            return false;

        Span<nuint> results = stackalloc nuint[4];
        foreach (var addr in GetBufferPointersInPageRange(pageAddress, pageSize, (int)settings.Size, settings.MinAddress, settings.MaxAddress, results))
        {
            // MAP_PRIVATE | MAP_ANON = 0x1002
            // MAP_FIXED_NOREPLACE doesn't exist on OSX, map will fail if matches existing page.
            // Note: MAP_ANON on OSX is 0x1000, not 0x20 like on Linux.
            // ReSharper disable once RedundantCast
            nuint allocated = (nuint)(nint)Posix.mmap(addr, settings.Size, (int)MemoryProtection.ReadWriteExecute, 0x101002, -1, 0);
            if (allocated == MAP_FAILED)
                continue;

            // If our hint failed and memory was mapped elsewhere, unmap and try again.
            if (allocated != addr)
            {
                Posix.munmap(allocated, settings.Size);
                continue;
            }

            result = new LocatorItem(allocated, settings.Size);
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
