using System.Diagnostics;
using Reloaded.Memory.Buffers.Exceptions;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using Reloaded.Memory.Buffers.Native.Windows;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using static Reloaded.Memory.Buffers.Native.Windows.Kernel32;
using static Reloaded.Memory.Buffers.Utilities.Mathematics;
using static Reloaded.Memory.Native.Windows.Kernel32;

namespace Reloaded.Memory.Buffers;

#pragma warning disable CA1416 // Validate platform compatibility
/// <summary>
/// Windows specific buffer allocator.
/// </summary>
public static partial class BufferAllocator
{
    // Devirtualized based on target.
    private static LocatorItem AllocateWindows(BufferAllocatorSettings settings) =>
        Polyfills.GetProcessId() == settings.TargetProcess.Id
        ? AllocateFast(new LocalKernel32(), GetMaxWindowsAddress(settings.TargetProcess), settings)
        : AllocateFast(new RemoteKernel32(settings.TargetProcess.Handle), GetMaxWindowsAddress(settings.TargetProcess), settings);

    private static unsafe LocatorItem AllocateFast<T>(T k32, nuint maxAddress, BufferAllocatorSettings settings) where T : IKernel32
    {
        maxAddress = Min(maxAddress, settings.MaxAddress);
        for (int x = 0; x < settings.RetryCount; x++)
        {
            // Until we get all of the pages.
            nuint currentAddress = settings.MinAddress;
            while (currentAddress <= maxAddress)
            {
                // Get our info from VirtualQueryEx.
                var memoryInformation = new MEMORY_BASIC_INFORMATION();
                var hasPage = k32.VirtualQuery(currentAddress, &memoryInformation);
                if (hasPage == 0)
                    break;

                // Add the page and increment address iterator to go to next page.
                if (TryAllocateBuffer(k32, ref memoryInformation, settings, out var item))
                    return item;

                currentAddress += memoryInformation.RegionSize;
            }
        }

        // See remarks on 'BruteForce' in BufferAllocatorSettings, as for why this code exists.
        // I'm not particularly fond of it, but what can you do?
        if (settings.BruteForce)
        {
            nuint currentAddress = settings.MinAddress;
            while (currentAddress <= maxAddress)
            {
                var memoryInformation = new MEMORY_BASIC_INFORMATION();
                var hasItem = k32.VirtualQuery(currentAddress, &memoryInformation);
                if (hasItem == 0)
                    break;

                if (TryAllocateBuffer(k32, ref memoryInformation, settings, out var item))
                    return item;

                currentAddress += (nuint)Cached.GetAllocationGranularity();
            }
        }

        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }

    /// <summary>
    ///     Calculates the max memory address for a given process.
    /// </summary>
    /// <param name="process">The process in question.</param>
    internal static nuint GetMaxWindowsAddress(Process process)
    {
        if (Polyfills.GetProcessId() == process.Id)
        {
            // Note: In WOW64 mode, the following rules apply:
            // - If current process is large address aware, this will return 0xFFFEFFFF.
            // - If it is not LAA, this should return 0x7FFEFFFF.
            return Cached.GetMaxAddress();
        }

        // Is this Windows on Windows 64? (x86 app running on x64 Windows)
        bool hasIsWow64 = IsWow64Process(process.Handle, out var isWow64);
        GetSystemInfo(out SYSTEM_INFO systemInfo);

        nuint maxAddress = 0x7FFFFFFF; // 32bit max

        // If target is not using WoW64 emulation layer, trust GetSystemInfo for max address.
        if (hasIsWow64 && !isWow64)
            maxAddress = systemInfo.lpMaximumApplicationAddress;

        return maxAddress;
    }

    private static bool TryAllocateBuffer<T>(T k32, ref MEMORY_BASIC_INFORMATION pageInfo, BufferAllocatorSettings settings, out LocatorItem result) where T : IKernel32
    {
        result = default;
        // Fast return if page is not free.
        if (pageInfo.State != MEM_STATE.FREE)
            return false;

        Span<nuint> results = stackalloc nuint[4];
        foreach (var addr in GetBufferPointersInPageRange(ref pageInfo, (int)settings.Size, settings.MinAddress, settings.MaxAddress, results))
        {
            // ReSharper disable once RedundantCast
            nuint allocated = k32.VirtualAlloc(addr, (nuint)settings.Size);
            if (allocated == 0)
                continue;

            // Sanity test in case of '0' value input.
            if (allocated != addr)
            {
                k32.VirtualFree(allocated, (nuint)settings.Size);
                continue;
            }

            result = new LocatorItem(allocated, settings.Size);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if memory can be allocated inside <paramref name="pageInfo"/> provided the size, minimum and maximum pointer.
    /// </summary>
    /// <param name="pageInfo">Contains the information about a singular memory page.</param>
    /// <param name="bufferSize">The size that an allocation would occupy. Pre-aligned to page-size.</param>
    /// <param name="minimumPtr">The maximum pointer an allocation can occupy.</param>
    /// <param name="maximumPtr">The minimum pointer an allocation can occupy.</param>
    /// <param name="results">Span containing the results; must have at least 4 items.</param>
    /// <returns>Zero if the operation fails; otherwise positive value.</returns>
    private static Span<nuint> GetBufferPointersInPageRange(ref MEMORY_BASIC_INFORMATION pageInfo, int bufferSize, nuint minimumPtr,
        nuint maximumPtr, Span<nuint> results)
    {
        nuint pageStart = pageInfo.BaseAddress;
        nuint pageEnd = pageInfo.BaseAddress + pageInfo.RegionSize;
        int allocationGranularity = Cached.GetAllocationGranularity();
        return GetPossibleBufferAddresses(minimumPtr, maximumPtr, pageStart, pageEnd, (nuint) bufferSize, allocationGranularity, results);
    }

    private interface IKernel32
    {
        unsafe nuint VirtualQuery(nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer);
        nuint VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize);
        bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize);
    }

    private struct LocalKernel32 : IKernel32
    {
        public unsafe nuint VirtualQuery(nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer)
            => Kernel32.VirtualQuery(lpAddress, lpBuffer, (nuint) sizeof(MEMORY_BASIC_INFORMATION));

        public nuint VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize) =>
            Reloaded.Memory.Native.Windows.Kernel32.VirtualAlloc(lpAddress, dwSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_EXECUTE_READWRITE);

        public bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize) =>
            Reloaded.Memory.Native.Windows.Kernel32.VirtualFree(lpAddress, dwSize, MEM_ALLOCATION_TYPE.MEM_RELEASE);
    }

    private readonly struct RemoteKernel32 : IKernel32
    {
        private readonly nint _handle;
        public RemoteKernel32(nint handle) => _handle = handle;

        public unsafe nuint VirtualQuery(nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer)
            => Kernel32.VirtualQueryEx(_handle, lpAddress, lpBuffer, (nuint) sizeof(MEMORY_BASIC_INFORMATION));

        public nuint VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize) =>
            VirtualAllocEx(_handle, lpAddress, dwSize, MEM_ALLOCATION_TYPE.MEM_RESERVE | MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_EXECUTE_READWRITE);

        public bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize) =>
            Reloaded.Memory.Native.Windows.Kernel32.VirtualFreeEx(_handle, lpAddress, dwSize, MEM_ALLOCATION_TYPE.MEM_RELEASE);
    }
}
