﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Internal.Structs;
using Reloaded.Memory.Buffers.Internal.Utilities;
using static Reloaded.Memory.Buffers.Internal.Kernel32.Kernel32;
using static Reloaded.Memory.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// Provides a a way to detect individual Reloaded buffers inside a process used for general small size memory storage,
    /// adding buffer information within certain proximity of an address as well as other various utilities partaining to
    /// buffers.
    /// </summary>
    public class MemoryBufferHelper
    {
        /// <summary> Contains the default size of memory pages to be allocated. </summary>
        internal const int DefaultPageSize = 0x1000;

        /// <summary> Name of the systemwide mutex used for allocation synchronization. </summary>
        internal string CreateBufferMutexName() => $"Reloaded.Memory.Buffers.MemoryBufferHelper | Allocate Memory | PID: {Process.Id}";

        /// <summary> Mutex used to mutually exclude runs of all functions which internally allocate memory leading to a change of internal state. </summary>
        private Mutex _allocateMemoryMutex;

        /// <summary> Contains all of the memory pages found in the last scan through the target process. </summary>
        private MEMORY_BASIC_INFORMATION[] _pageCache;

        /// <summary> Implementation of the Searcher that scans and finds existing <see cref="MemoryBuffer"/>s within the current process. </summary>
        private MemoryBufferSearcher _bufferSearcher;
        
        private VirtualQueryUtility.VirtualQueryFunction _virtualQueryFunction;

        /// <summary> The process on which the MemoryBuffer acts upon. </summary>
        public Process Process { get; private set; }

        /// <summary>
        /// Creates a new <see cref="MemoryBufferHelper"/> for the specified process.
        /// </summary>
        /// <param name="process">The process.</param>
        public MemoryBufferHelper(Process process)
        {
            Process = process;
            _bufferSearcher = new MemoryBufferSearcher(process);
            _virtualQueryFunction = VirtualQueryUtility.GetVirtualQueryFunction(process);
            _allocateMemoryMutex = MutexObtainer.MakeMutex(CreateBufferMutexName());
        }

        /*
            -----------------------
            Memory Buffer Factories
            -----------------------
        */

        /// <summary>
        /// Finds an appropriate location where a <see cref="MemoryBuffer"/>;
        /// or other memory allocation could be performed.
        /// Note: Please see remarks for this function.
        /// </summary>
        /// <param name = "size" > The space in bytes that the specific <see cref="MemoryBuffer"/> would require to accomodate.</param>
        /// <param name="minimumAddress">The minimum absolute address to find a buffer in.</param>
        /// <param name="maximumAddress">The maximum absolute address to find a buffer in.</param>
        /// <param name="isPrivateBuffer">Defines whether the buffer type created is a shared or private buffer.</param>
        /// <remarks>
        /// WARNING:
        ///     Using this in a multithreaded environment can be dangerous, be careful.
        ///     It is possible to have a race condition on memory allocation.
        ///     If you want to just allocate memory, please use the provided <see cref="Allocate"/> function instead.
        /// </remarks>
        public BufferAllocationProperties FindBufferLocation(int size, long minimumAddress, long maximumAddress, bool isPrivateBuffer = false)
        {
            if (minimumAddress <= 0)
                throw new ArgumentException("Please do not set the minimum address to 0 or negative. It collides with the return values of Windows API functions" +
                                            "where e.g. 0 is returned on failure but you can also allocate successfully on 0.");

            int bufferSize = GetBufferSize(size, isPrivateBuffer);

            // Search through the buffer cache first.
            if (_pageCache != null)
            {
                for (int x = 0; x < _pageCache.Length; x++)
                {
                    var pointer = GetBufferPointerInPageRange(ref _pageCache[x], bufferSize, (IntPtr) minimumAddress, (IntPtr) maximumAddress);
                    if (pointer != IntPtr.Zero)
                    {
                        // Page cache contains a page that can "work". Check if this page is still valid by running VirtualQuery on it 
                        // and rechecking the new page.
                        var memoryBasicInformation = new MEMORY_BASIC_INFORMATION();
                        var result =_virtualQueryFunction(Process.Handle, pointer, ref memoryBasicInformation);
                        if (result == (UIntPtr)0)
                            throw new Exception("VirtualQuery failed. Result is 0.");

                        var newPointer = GetBufferPointerInPageRange(ref memoryBasicInformation, bufferSize, (IntPtr) minimumAddress, (IntPtr) maximumAddress);
                        if (newPointer != IntPtr.Zero)
                            return new BufferAllocationProperties(newPointer, bufferSize);
                    }
                }
            }

            // Not found in cache, get all real pages and try find appropriate spot.
            var memoryPages = MemoryPages.GetPages(Process).ToArray();
            _pageCache      = memoryPages;

            for (int x = 0; x < memoryPages.Length; x++)
            {
                var pointer = GetBufferPointerInPageRange(ref memoryPages[x], bufferSize, (IntPtr) minimumAddress, (IntPtr) maximumAddress);
                if (pointer != IntPtr.Zero)
                    return new BufferAllocationProperties(pointer, bufferSize);
            }

            throw new Exception($"Unable to find memory location to fit MemoryBuffer of size {size} ({bufferSize}) between {minimumAddress.ToString("X")} and {maximumAddress.ToString("X")}.");
        }

        /// <summary>
        /// Creates a <see cref="MemoryBuffer"/> that satisfies a set size constraint
        /// and proximity to a set address.
        /// </summary>
        /// <param name="size">The minimum size the <see cref="MemoryBuffer"/> will have to accomodate.</param>
        /// <param name="minimumAddress">The minimum absolute address to create a buffer in.</param>
        /// <param name="maximumAddress">The maximum absolute address to create a buffer in.</param>
        /// <param name="retryCount">In the case the memory allocation fails; the amount of times memory allocation is to be retried.</param>
        /// <exception cref="System.Exception">Memory allocation failure due to possible race condition with other process/process itself/Windows scheduling.</exception>
        public MemoryBuffer CreateMemoryBuffer(int size, long minimumAddress = 0x10000, long maximumAddress = 0x7FFFFFFF, int retryCount = 3)
        {
            if (minimumAddress <= 0)
                throw new ArgumentException("Please do not set the minimum address to 0 or negative. It collides with the return values of Windows API functions" +
                                            "where e.g. 0 is returned on failure but you can also allocate successfully on 0.");

            // Keep retrying memory allocation.
            _allocateMemoryMutex.WaitOne();

            try
            {
                return Run(retryCount, () =>
                {
                    var memoryLocation = FindBufferLocation(size, minimumAddress, maximumAddress);
                    var buffer = MemoryBufferFactory.CreateBuffer(Process, memoryLocation.MemoryAddress, memoryLocation.Size);
                    _bufferSearcher.AddBuffer(buffer);

                    _allocateMemoryMutex.ReleaseMutex();
                    return buffer;
                });
            }
            catch (Exception)
            {
                _allocateMemoryMutex.ReleaseMutex();
                throw;
            }

        }


        /// <summary>
        /// Creates a <see cref="PrivateMemoryBuffer"/> that satisfies a set size constraint and proximity to a set address.
        /// </summary>
        /// <param name="size">The minimum size the <see cref="PrivateMemoryBuffer"/> will have to accomodate.</param>
        /// <param name="minimumAddress">The minimum absolute address to create a buffer in.</param>
        /// <param name="maximumAddress">The maximum absolute address to create a buffer in.</param>
        /// <param name="retryCount">In the case the memory allocation fails; the amount of times memory allocation is to be retried.</param>
        /// <exception cref="System.Exception">Memory allocation failure due to possible race condition with other process/process itself/Windows scheduling.</exception>
        public PrivateMemoryBuffer CreatePrivateMemoryBuffer(int size, long minimumAddress = 0x10000, long maximumAddress = 0x7FFFFFFF, int retryCount = 3)
        {
            if (minimumAddress <= 0)
                throw new ArgumentException("Please do not set the minimum address to 0 or negative. It collides with the return values of Windows API functions" +
                                            "where e.g. 0 is returned on failure but you can also allocate successfully on 0.");

            // Keep retrying memory allocation.
            _allocateMemoryMutex.WaitOne();

            try
            {
                return Run(retryCount, () =>
                {
                    var memoryLocation = FindBufferLocation(size, minimumAddress, maximumAddress, true);
                    var buffer = MemoryBufferFactory.CreatePrivateBuffer(Process, memoryLocation.MemoryAddress, memoryLocation.Size);

                    _allocateMemoryMutex.ReleaseMutex();
                    return buffer;
                });
            }
            catch (Exception)
            {
                _allocateMemoryMutex.ReleaseMutex();
                throw;
            }
        }

        /*
            -------------------
            Core Helper Methods
            -------------------
        */

        /// <summary>
        /// Searches unmanaged memory for pre-existing <see cref="MemoryBuffer"/>s that satisfy
        /// the given size requirements.
        /// </summary>
        /// <param name="size">The amount of bytes a buffer must have minimum.</param>
        /// <param name="useCache">See <see cref="MemoryBufferSearcher.GetBuffers"/></param>
        public MemoryBuffer[] FindBuffers(int size, bool useCache = true)
        {
            return _bufferSearcher.GetBuffers(size, useCache);
        }

        /// <summary>
        /// Searches unmanaged memory for pre-existing <see cref="MemoryBuffer"/>s that satisfy
        /// the given size requirements and address range.
        /// </summary>
        /// <param name="size">The amount of bytes a buffer must have minimum.</param>
        /// <param name="minimumAddress">The maximum pointer a <see cref="MemoryBuffer"/> can occupy.</param>
        /// <param name="maximumAddress">The minimum pointer a <see cref="MemoryBuffer"/> can occupy.</param>
        /// <param name="useCache">See <see cref="MemoryBufferSearcher.GetBuffers"/></param>
        /// <returns></returns>
        public MemoryBuffer[] FindBuffers(int size, IntPtr minimumAddress, IntPtr maximumAddress, bool useCache = true)
        {
            // Get buffers already existing in process.
            var buffers = _bufferSearcher.GetBuffers(size, useCache);

            // Get all MemoryBuffers where their raw data range fits into the given minimum and maximum address.
            AddressRange allowedRange = new AddressRange((long) minimumAddress, (long) maximumAddress);
            var memoryBuffers         = new List<MemoryBuffer>(buffers.Length);

            foreach (var buffer in buffers)
            {
                var bufferHeader = buffer.Properties;
                var bufferAddressRange = new AddressRange((long)bufferHeader.DataPointer, (long)(bufferHeader.DataPointer + bufferHeader.Size));
                if (allowedRange.Contains(ref bufferAddressRange))
                    memoryBuffers.Add(buffer);
            }

            return memoryBuffers.ToArray();
        }

        /// <summary>
        /// Allocates memory that satisfies a set size constraint and proximity to a set address.
        /// </summary>
        /// <param name="size">The minimum size of the memory to be allocated.</param>
        /// <param name="minimumAddress">The minimum absolute address to allocate in.</param>
        /// <param name="maximumAddress">The maximum absolute address to allocate in.</param>
        /// <param name="retryCount">In the case the memory allocation for a potential location fails; the amount of times memory allocation is to be retried.</param>
        /// <exception cref="System.Exception">Memory allocation failure due to possible race condition with other process/process itself/Windows scheduling.</exception>
        /// <remarks>
        ///     This function is virtually the same to running <see cref="FindBufferLocation"/> and then running Windows'
        ///     VirtualAlloc yourself. Except for the extra added safety of mutual exclusion (Mutex) and mitigating a wine bug
        ///     where allocation can fail on the first free pages repeatedly.
        ///     The memory is allocated with the PAGE_EXECUTE_READWRITE permissions.
        /// </remarks>
        public BufferAllocationProperties Allocate(int size, int minimumAddress = 0x10000, int maximumAddress = 0x7FFFFFFF, int retryCount = 3)
        {
            if (minimumAddress <= 0)
                throw new ArgumentException("Please do not set the minimum address to 0 or negative. It collides with the return values of Windows API functions" +
                                            "where e.g. 0 is returned on failure but you can also allocate successfully on 0.");
            Exception allocationException = new("Failed to allocate memory using VirtualAlloc/ VirtualAllocEx");

            // Keep retrying memory allocation.
            _allocateMemoryMutex.WaitOne();

            while (minimumAddress < maximumAddress)
            {
                try
                {
                    return Run(retryCount, () =>
                    {
                        var memoryLocation = FindBufferLocation(size, minimumAddress, maximumAddress, true);
                        var virtualAllocFunction = VirtualAllocUtility.GetVirtualAllocFunction(Process);
                        var result = virtualAllocFunction(Process.Handle, memoryLocation.MemoryAddress, (ulong)memoryLocation.Size);

                        if (result == IntPtr.Zero)
                            throw allocationException;
                        _allocateMemoryMutex.ReleaseMutex();
                        return memoryLocation;
                    });
                }
                catch (Exception)
                {
                    minimumAddress += 0x10000;
                }
            }
            _allocateMemoryMutex.ReleaseMutex();
            throw allocationException;
        }

        /// <summary>
        /// Frees memory that has been allocated by <see cref="Allocate"/>.
        /// </summary>
        /// <param name="address">The address of the memory originally received from the call to <see cref="Allocate"/>.</param>
        public void Free(IntPtr address)
        {
            _allocateMemoryMutex.WaitOne();

            try
            {
                VirtualFreeUtility.GetVirtualFreeFunction(Process)(Process.Handle, address);
                _allocateMemoryMutex.ReleaseMutex();
            }
            catch (Exception)
            {
                _allocateMemoryMutex.ReleaseMutex();
                throw;
            }
        }


        /*
            -----------------------
            Internal Helper Methods
            -----------------------
        */

        /// <summary>
        /// Calculates the size of a <see cref="MemoryBuffer"/> to be created for a given requested size
        /// of raw data, taking into consideration buffer overhead.
        /// </summary>
        /// <param name="size">The size of the buffer to be allocated.</param>
        /// <param name="isPrivateBuffer">Defines whether the buffer type created is a shared or private buffer.</param>
        /// <returns>A calculated buffer size based off of the requested capacity in bytes.</returns>
        public int GetBufferSize(int size, bool isPrivateBuffer = false)
        {
            // Get size of buffer; allocation granularity or larger if greater than the granularity.
            GetSystemInfo(out var systemInfo);

            // Guard to ensure that page size is at least the minimum supported by the processor
            // While Reloaded is only intended for X86/64; this may be useful in the future.
            // The second guard ensured the default page size is aligned with the system info.
            int pageSize = DefaultPageSize;
            if (systemInfo.dwPageSize > pageSize || (pageSize % systemInfo.dwPageSize != 0))
                pageSize = (int)systemInfo.dwPageSize;

            if (isPrivateBuffer)
                return Mathematics.RoundUp(size + MemoryBufferFactory.PrivateBufferOverhead, pageSize);

            return Mathematics.RoundUp(size + MemoryBufferFactory.BufferOverhead, pageSize);
        }


        /// <summary>
        /// Runs a given function with a specified number of retries if an exception is thrown.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="retries">The number of times to retry the function.</param>
        /// <param name="function">The function to run.</param>
        private T Run<T>(int retries, Func<T> function)
        {
            Exception caughtException = new Exception("This should not throw");
            for (int x = 0; x < retries; x++)
            {
                try  { return function();  }
                catch (Exception ex) { caughtException = ex; }
            }

            throw caughtException;
        }

        /// <summary>
        /// Checks if a buffer can be created within a given set of pages described by pageInfo
        /// satisfying the given size, minimum and maximum memory location.
        /// </summary>
        /// <param name="pageInfo">Contains the information about a singular memory page.</param>
        /// <param name="bufferSize">The size that a <see cref="MemoryBuffer"/> would occupy. Pre-aligned to page-size.</param>
        /// <param name="minimumPtr">The maximum pointer a <see cref="MemoryBuffer"/> can occupy.</param>
        /// <param name="maximumPtr">The minimum pointer a <see cref="MemoryBuffer"/> can occupy.</param>
        /// <returns>Zero if the operation fails; otherwise positive value.</returns>
        private IntPtr GetBufferPointerInPageRange(ref MEMORY_BASIC_INFORMATION pageInfo, int bufferSize, IntPtr minimumPtr, IntPtr maximumPtr)
        {
            // Fast return if page is not free.
            if (pageInfo.State != (uint)MEM_ALLOCATION_TYPE.MEM_FREE)
                return IntPtr.Zero;

            // This is valid in both 32bit and 64bit Windows.
            // We can call GetSystemInfo to get this but that's a waste; these are constant for x86 and x64.
            int allocationGranularity = 65536;

            // Do not align page start/end to allocation granularity yet.
            // Align it when we map the possible buffer ranges in the pages.
            long pageStart = (long)pageInfo.BaseAddress;
            long pageEnd   = (long)pageInfo.BaseAddress + (long)pageInfo.RegionSize;

            // Get range for page and min-max region.
            var minMaxRange  = new AddressRange((long)minimumPtr, (long)maximumPtr);
            var pageRange    = new AddressRange(pageStart, pageEnd);

            if (! pageRange.Overlaps(ref minMaxRange))
                return IntPtr.Zero;

            /* Three possible cases here:
               1. Page fits entirely inside min-max range and is smaller.
               2. Min-max range is inside page (i.e. page is bigger than the range)
               3. Page and min-max intersect, e.g. first half of pages in end of min-max
                  or second half of pages in start of min-max.

               Below we will build a set of possible buffer allocation ranges
               and check if they satisfy our conditions.
            */

            /* Try placing range at start and end of page boundaries.
               Since we are allocating in page boundaries, we must compare against Min-Max. */

            // Note: We are rounding page boundary addresses up/down, possibly beyond the original ends/starts of page.
            //       We need to validate that we are still in the bounds of the actual page itself.

            var allocPtrPageMaxAligned = Mathematics.RoundDown(pageRange.EndPointer - bufferSize, allocationGranularity);
            var allocRangePageMaxStart = new AddressRange(allocPtrPageMaxAligned, allocPtrPageMaxAligned + bufferSize);

            if (pageRange.Contains(ref allocRangePageMaxStart) && minMaxRange.Contains(ref allocRangePageMaxStart))
                return (IntPtr)allocRangePageMaxStart.StartPointer;

            var allocPtrPageMinAligned = Mathematics.RoundUp(pageRange.StartPointer, allocationGranularity);
            var allocRangePageMinStart = new AddressRange(allocPtrPageMinAligned, allocPtrPageMinAligned + bufferSize);

            if (pageRange.Contains(ref allocRangePageMinStart) && minMaxRange.Contains(ref allocRangePageMinStart))
                return (IntPtr)allocRangePageMinStart.StartPointer;

            /* Try placing range at start and end of given minimum-maximum.
               Since we are allocating in Min-Max, we must compare against Page Boundaries. */

            // Note: Remember that rounding is dangerous and could potentially cause max and min to cross as usual,
            //       must check proposed page range against both given min-max and page memory range.

            var allocPtrMaxAligned = Mathematics.RoundDown((long)maximumPtr - bufferSize, allocationGranularity);
            var allocRangeMaxStart = new AddressRange(allocPtrMaxAligned, allocPtrMaxAligned + bufferSize);

            if (pageRange.Contains(ref allocRangeMaxStart) && minMaxRange.Contains(ref allocRangeMaxStart))
                return (IntPtr)allocRangeMaxStart.StartPointer;

            var allocPtrMinAligned = Mathematics.RoundUp((long)minimumPtr, allocationGranularity);
            var allocRangeMinStart = new AddressRange(allocPtrMinAligned, allocPtrMinAligned + bufferSize);

            if (pageRange.Contains(ref allocRangeMinStart) && minMaxRange.Contains(ref allocRangeMinStart))
                return (IntPtr)allocRangeMinStart.StartPointer;

            return IntPtr.Zero;
        }
    }
}
