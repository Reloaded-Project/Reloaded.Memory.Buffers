using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Native.Linux;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Enums;
using Reloaded.Memory.Native.Unix;

namespace Reloaded.Memory.Buffers.Internal;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// Windows specific buffer allocator.
/// </summary>
internal static partial class BufferAllocator
{
    // Devirtualized based on target.
    private static LocatorItem AllocateLinux(BufferAllocatorSettings settings)
    {
        for (int x = 0; x < settings.RetryCount; x++)
        {
            // Until we get all of the pages.
            var regions = LinuxMapParser.GetFreeRegions(settings.TargetProcess);
            foreach (MemoryMapEntry region in regions)
            {
                // Exit if we are done iterating.
                if (region.StartAddress > settings.MaxAddress)
                    break;

                // Add the page and increment address iterator to go to next page.
                if (TryAllocateBuffer(region, settings, out var item))
                    return item;
            }
        }

        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }

    private static bool TryAllocateBuffer(MemoryMapEntry entry, BufferAllocatorSettings settings, out LocatorItem result)
    {
        result = default;

        Span<nuint> results = stackalloc nuint[4];
        foreach (var addr in GetPossibleBufferAddresses(settings.MinAddress, settings.MaxAddress, entry.StartAddress, entry.EndAddress, settings.Size, Cached.GetAllocationGranularity(), results))
        {
            // ReSharper disable once RedundantCast
            // MAP_PRIVATE | MAP_ANONYMOUS | MAP_FIXED_NOREPLACE = 0x100022
            nint allocated = Posix.mmap(addr, (nuint)settings.Size, (int)MemoryProtection.ReadWriteExecute, 0x100022, -1, 0);

            if (allocated == -1)
                continue;

            // Just in case, for older kernels;
            if ((nuint)allocated != addr)
            {
                Posix.munmap((nuint)allocated, settings.Size);
                continue;
            }

            result = new LocatorItem((nuint)allocated, settings.Size);
            return true;
        }

        return false;
    }
}
