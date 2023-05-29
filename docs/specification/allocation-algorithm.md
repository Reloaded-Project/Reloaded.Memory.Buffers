# Allocation Algorithm

!!! tip "This page provides sample source code to demonstrate how the library may be implemented."

!!! tip "This is to help you implement compatible solutions."

!!! tip "For a full implementation, look at this repo's code."

Allocating buffers and locators generally follows the same algorithm; with minor platform specific differences.

- [Enumerate Free Memory Pages](#finding-free-memory-pages)  
- [Check if Page Satisfies Constraints](#checking-if-buffer-fits)  
- [Allocating A Buffer](#allocating-a-buffer)  

## Finding Free Memory Pages

!!! info "In order to find all free memory regions we do a page walk using VirtualQuery(Ex)."

### Windows

=== "C#"

    ```csharp
    private static unsafe LocatorItem Allocate<T>(T k32, nuint maxAddress, BufferAllocatorSettings settings) where T : IKernel32
    {
        // Note: `k32.VirtualQuery` is `VirtualQueryEx` if targeting another process.
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
    
        // Some form of error handling.
        throw new MemoryBufferAllocationException(settings.MinAddress, settings.MaxAddress, (int)settings.Size);
    }
    ```

=== "C++"

    ```cpp
    // Disclaimer: This code was ported by AI, then cleaned up by human. Not tested.
    #include <windows.h>
    #include <stdexcept>
    #include <memory>

    LocatorItem Allocate(BufferAllocatorSettings settings, uintptr_t maxAddress)
    {
        // Note: `VirtualQuery` is `VirtualQueryEx` if targeting another process. Abstract this if needed.
        // Until we get all of the pages.
        uintptr_t currentAddress = settings.MinAddress;
        while (currentAddress <= maxAddress)
        {
            // Get our info from VirtualQueryEx.
            MEMORY_BASIC_INFORMATION memoryInformation;
            DWORD hasPage = VirtualQuery((LPVOID)currentAddress, &memoryInformation, sizeof(memoryInformation));
            if (hasPage == 0)
                break;

            // Add the page and increment address iterator to go to next page.
            LocatorItem item;
            if (TryAllocateBuffer(memoryInformation, settings, item))
                return item;

            currentAddress += memoryInformation.RegionSize;
        }
    
        // Some form of error handling.
        throw std::runtime_error("Failed to allocate memory buffer");
    }
    ```

### Linux

!!! info "In order to find all free memory regions we need to parse a text file at `$"/proc/{processId}/maps"`."

The following code parses an individual line of said file:  

=== "C#"

    ```csharp
    internal static MemoryMapEntry ParseMemoryMapEntry(string line)
    {
        // Example line: "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file"
        ReadOnlySpan<char> lineSpan = line.AsSpan();
        var dashIndex = lineSpan.IndexOf('-');
        if (dashIndex == -1)
            ThrowHelpers.ThrowLinuxBadMemoryMapEntry();
    
        var spaceIndex = lineSpan.Slice(dashIndex).IndexOf(' ');
        if (spaceIndex == -1)
            ThrowHelpers.ThrowLinuxBadMemoryMapEntry();
    
        ReadOnlySpan<char> startAddressSpan = lineSpan.SliceFast(..dashIndex);
        ReadOnlySpan<char> endAddressSpan = lineSpan.SliceFast(dashIndex + 1, spaceIndex - 1);
    
        return new MemoryMapEntry
        {
            StartAddress = ParseHexAddress(startAddressSpan),
            EndAddress = ParseHexAddress(endAddressSpan)
        };
    }
    ```

=== "C++"

    ```cpp
    // Disclaimer: This code was ported by AI, then cleaned up by human. Not tested.
    #include <stdexcept>
    #include <string>
    #include <cstddef>

    struct MemoryMapEntry {
        unsigned long StartAddress;
        unsigned long EndAddress;
    };

    MemoryMapEntry ParseMemoryMapEntry(const std::string& line) {
        // Example line: "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file"
        std::size_t dashIndex = line.find('-');
        if (dashIndex == std::string::npos) {
            throw std::runtime_error("Bad Memory Map Entry");
        }
    
        std::size_t spaceIndex = line.find(' ', dashIndex);
        if (spaceIndex == std::string::npos) {
            throw std::runtime_error("Bad Memory Map Entry");
        }
    
        std::string startAddressStr = line.substr(0, dashIndex);
        std::string endAddressStr = line.substr(dashIndex + 1, spaceIndex - dashIndex - 1);
    
        MemoryMapEntry entry;
        entry.StartAddress = std::stoul(startAddressStr, nullptr, 16);
        entry.EndAddress = std::stoul(endAddressStr, nullptr, 16);
        
        return entry;
    }
    ```

Parsing this file gives you all of the mapped memory regions.  
Any regions not listed in this files are considered free, thus, we should try allocating there.  
We can extract the free regions in the following fashion:  

=== "C#"

    ```csharp
    /// <summary>
    /// Returns all free regions based on the found regions.
    /// </summary>
    /// <param name="regions">The found regions.</param>
    public static List<MemoryMapEntry> GetFreeRegions(Span<MemoryMapEntry> regions)
    {
        nuint lastEndAddress = 0;
        var freeRegions = new List<MemoryMapEntry>(regions.Length + 2); // +2 for start and finish
    
        for (int x = 0; x < regions.Length; x++)
        {
            MemoryMapEntry entry = regions.DangerousGetReferenceAt(x);
            if (entry.StartAddress > lastEndAddress)
            {
                freeRegions.Add(new MemoryMapEntry
                {
                    StartAddress = lastEndAddress,
                    EndAddress = entry.StartAddress - 1
                });
            }
    
            lastEndAddress = entry.EndAddress + 1;
        }
    
        // After the last region, up to the end of memory
        if (lastEndAddress <= Cached.GetMaxAddress())
        {
            freeRegions.Add(new MemoryMapEntry
            {
                StartAddress = lastEndAddress,
                EndAddress = Cached.GetMaxAddress()
            });
        }
    
        return freeRegions;
    }
    ```

=== "C++"

    ```cpp
    #include <vector>
    #include <climits>
    #include <stdexcept>
    #include <cstdint>
    #ifdef _WIN32
    #include <Windows.h>
    #else
    #include <unistd.h>
    #endif

    class Cached
    {
    private:
        static constexpr int ScPagesizeLinux = 30;
        static constexpr int ScPagesizeOsx = 29;
    
        static uint64_t s_maxAddress;
        static int s_allocationGranularity;
    
    public:
        static uint64_t GetMaxAddress() { return s_maxAddress; }
        static int GetAllocationGranularity() { return s_allocationGranularity; }
    
        static void Initialize()
        {
    #ifdef _WIN32
            SYSTEM_INFO info;
            GetSystemInfo(&info);
            s_maxAddress = reinterpret_cast<uint64_t>(info.lpMaximumApplicationAddress);
            s_allocationGranularity = static_cast<int>(info.dwAllocationGranularity);
    #elif __linux__
            s_maxAddress = UINTPTR_MAX;
            s_allocationGranularity = static_cast<int>(sysconf(ScPagesizeLinux));
    #elif __APPLE__
            s_maxAddress = UINTPTR_MAX;
            s_allocationGranularity = static_cast<int>(sysconf(ScPagesizeOsx));
    #else
            throw std::runtime_error("Platform not supported");
    #endif
        }
    };
    
    // Initialize static members
    uint64_t Cached::s_maxAddress = 0;
    int Cached::s_allocationGranularity = 0;
    
    std::vector<MemoryMapEntry> GetFreeRegions(std::vector<MemoryMapEntry> regions)
    {
        uintptr_t lastEndAddress = 0;
        std::vector<MemoryMapEntry> freeRegions;
        freeRegions.reserve(regions.size() + 2); // +2 for start and finish
    
        for (auto& entry : regions)
        {
            if (entry.StartAddress > lastEndAddress)
            {
                freeRegions.push_back({lastEndAddress, entry.StartAddress - 1});
            }
    
            lastEndAddress = entry.EndAddress + 1;
        }
    
        // After the last region, up to the end of memory
        if (lastEndAddress <= Cached::GetMaxAddress())
        {
            freeRegions.push_back({lastEndAddress, Cached::GetMaxAddress()});
        }
    
        return freeRegions;
    }
    ```

Lastly, try allocating; on Linux, to allocate you should use `mmap`. 
Here's remainder of the code that does the actual lookup and allocation.

=== "C#"

    ```csharp
    // Until we get all of the pages.
    foreach (MemoryMapEntry region in LinuxMapParser.GetFreeRegions(settings.TargetProcess))
    {
        // Exit if we are done iterating.
        if (region.StartAddress > settings.MaxAddress)
            break;
    
        // Add the page and increment address iterator to go to next page.
        if (TryAllocateBuffer(region, settings, out var item))
            return item;
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
    
            // Error handling for older kernels before 2018 that don't respect MAP_FIXED_NOREPLACE.
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
    ```

=== "C++"

    ```cpp
    // Disclaimer: This code was ported by AI, then cleaned up by human. Not tested.
    // Note: Uses C++20
    #include <sys/mman.h>
    #include <array>
    
    // Until we get all of the pages.
    for (const MemoryMapEntry& region : LinuxMapParser::GetFreeRegions(settings.TargetProcess))
    {
        // Exit if we are done iterating.
        if (region.StartAddress > settings.MaxAddress)
            break;
    
        // Add the page and increment address iterator to go to next page.
        LocatorItem item;
        if (TryAllocateBuffer(region, settings, item))
            return item;
    }
    
    bool TryAllocateBuffer(const MemoryMapEntry& entry, const BufferAllocatorSettings& settings, LocatorItem& result)
    {
        result = LocatorItem(0, 0);
    
        std::array<uintptr_t, 4> resultArray;
        std::span<uintptr_t> results(resultArray);
        for (auto addr : GetPossibleBufferAddresses(settings.MinAddress, settings.MaxAddress, entry.StartAddress, entry.EndAddress, settings.Size, GetAllocationGranularity(), results))
        {
            intptr_t allocated = mmap(reinterpret_cast<void*>(addr), settings.Size, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_PRIVATE | MAP_ANONYMOUS | MAP_FIXED_NOREPLACE, -1, 0);
            if (allocated == reinterpret_cast<intptr_t>(MAP_FAILED))
                continue;
    
            // Error handling for older kernels before 2018 that don't respect MAP_FIXED_NOREPLACE.
            if (static_cast<uintptr_t>(allocated) != addr)
            {
                munmap(reinterpret_cast<void*>(allocated), settings.Size);
                continue;
            }
    
            result = LocatorItem(static_cast<uintptr_t>(allocated), settings.Size);
            return true;
        }
    
        return false;
    }
    ```

## Allocating a Buffer

### Windows

!!! info "For all found pages, satisfying constraints, we then do VirtualAlloc(Ex) to reserve and commit them."

=== "C#"

    ```csharp
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
    ```

=== "C++"

    ```cpp
    // Disclaimer: This code was ported by AI, then cleaned up by human. Not tested.
    // Note: Uses C++20
    #include <span>
    
    std::span<uintptr_t> GetBufferPointersInPageRange(MEMORY_BASIC_INFORMATION& pageInfo, int bufferSize, uintptr_t minimumPtr, uintptr_t maximumPtr, std::span<uintptr_t>& results)
    {
        uintptr_t pageStart = pageInfo.BaseAddress;
        uintptr_t pageEnd = pageInfo.BaseAddress + pageInfo.RegionSize;
        int allocationGranularity = GetAllocationGranularity();
        return GetPossibleBufferAddresses(minimumPtr, maximumPtr, pageStart, pageEnd, static_cast<uintptr_t>(bufferSize), allocationGranularity, results);
    }

    bool TryAllocateBuffer(Kernel32& k32, MEMORY_BASIC_INFORMATION& pageInfo, BufferAllocatorSettings& settings, LocatorItem& result)
    {
        result = LocatorItem(0, 0);
        if (pageInfo.State != MEM_STATE::FREE)
            return false;
    
        std::array<uintptr_t, 4> resultArray;
        std::span<uintptr_t> results(resultArray);
        for (auto addr : GetBufferPointersInPageRange(pageInfo, static_cast<int>(settings.Size), settings.MinAddress, settings.MaxAddress, results))
        {
            // Can redirect to VirtualAllocEx if target is another process.
            uintptr_t allocated = k32.VirtualAlloc(addr, static_cast<uintptr_t>(settings.Size));
            if (allocated == 0)
                continue;
    
            result = LocatorItem(allocated, settings.Size);
            return true;
        }
    
        return false;
    }
    ```

### Checking if Buffer Fits

!!! info "For any memory page returned from the OS, we need to check if it satisfies our constraints."

=== "C#"

    ```csharp
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
    
    /// <summary>
    ///     Defines a physical address range with a minimum and maximum address.
    /// </summary>
    internal readonly struct AddressRange
    {
        public readonly nuint StartPointer;
        public readonly nuint EndPointer;
        public nuint Size => EndPointer - StartPointer;
        
        public AddressRange(nuint startPointer, nuint endPointer)
        {
            StartPointer = startPointer;
            EndPointer = endPointer;
        }
    
        /// <summary>
        ///     Returns true if the other address range is completely inside
        ///     the current address range.
        /// </summary>
        /// <param name="otherRange">True if this address range is contained entirely inside the other.</param>
        public bool Contains(in AddressRange otherRange)
            => otherRange.StartPointer >= StartPointer && otherRange.EndPointer <= EndPointer;
    
        /// <summary>
        ///     Returns true if the other address range intersects this address range, i.e.
        ///     start or end of this range falls inside other range.
        /// </summary>
        /// <param name="otherRange">Returns true if there are any overlaps in the address ranges.</param>
        public bool Overlaps(in AddressRange otherRange)
        {
            if (PointInRange(otherRange, StartPointer)) return true;
            if (PointInRange(otherRange, EndPointer)) return true;
            if (PointInRange(this, otherRange.StartPointer)) return true;
            if (PointInRange(this, otherRange.EndPointer)) return true;
    
            return false;
        }
    
        /// <summary>
        ///     Returns true if a number "point", is between min and max of address range.
        /// </summary>
        /// <param name="range">Range inside which to test the point.</param>
        /// <param name="point">The point to test.</param>
        private bool PointInRange(in AddressRange range, nuint point)
            => point >= range.StartPointer && point <= range.EndPointer;
    }
    ```

=== "C++"

    ```cpp
    // Disclaimer: This code was ported by AI, then cleaned up by human. Not tested.
    // Note: Uses C++20
    #include <cstddef>
    #include <algorithm>
    #include <cstdint>
    #include <span>
    
    struct AddressRange {
        uintptr_t StartPointer;
        uintptr_t EndPointer;
    
        /**
         * Returns the size of the address range.
         */
        uintptr_t Size() const { return EndPointer - StartPointer; }
    
        /**
         * Constructor for AddressRange.
         * @param startPointer - Start of the address range.
         * @param endPointer - End of the address range.
         */
        AddressRange(uintptr_t startPointer, uintptr_t endPointer) : StartPointer(startPointer), EndPointer(endPointer) {}
    
        /**
         * Checks if this address range contains the other address range.
         * @param otherRange - The other address range to check.
         * @return true if this address range contains the other, false otherwise.
         */
        bool Contains(const AddressRange& otherRange) const {
            return otherRange.StartPointer >= StartPointer && otherRange.EndPointer <= EndPointer;
        }
    
        /**
         * Checks if this address range overlaps with the other address range.
         * @param otherRange - The other address range to check.
         * @return true if the address ranges overlap, false otherwise.
         */
        bool Overlaps(const AddressRange& otherRange) const {
            return PointInRange(otherRange, StartPointer) || PointInRange(otherRange, EndPointer)
                || PointInRange(*this, otherRange.StartPointer) || PointInRange(*this, otherRange.EndPointer);
        }

    private:
        /**
         * Checks if a point is within an address range.
         * @param range - The address range to check.
         * @param point - The point to check.
         * @return true if the point is in the range, false otherwise.
         */
        bool PointInRange(const AddressRange& range, uintptr_t point) const {
            return point >= range.StartPointer && point <= range.EndPointer;
        }
    };

    /**
     * Rounds up a number to the nearest multiple.
     * @param number - The number to round up.
     * @param multiple - The multiple to round up to.
     * @return The rounded up number.
    */
    uintptr_t RoundUp(uintptr_t number, int multiple) {
        return ((number + multiple - 1) / multiple) * multiple;
    }
    
    /**
    * Rounds down a number to the nearest multiple.
    * @param number - The number to round down.
    * @param multiple - The multiple to round down to.
    * @return The rounded down number.
    */
    uintptr_t RoundDown(uintptr_t number, int multiple) {
        return (number / multiple) * multiple;
    }

    /**
    * Adds two numbers and caps the result to UINTPTR_MAX to prevent overflow.
    * @param a - The first number.
    * @param b - The second number.
    * @return The sum of the two numbers, capped at UINTPTR_MAX if there would be an overflow.
    */
    uintptr_t AddWithOverflowCap(uintptr_t a, uintptr_t b) {
        if (a > UINTPTR_MAX - b) return UINTPTR_MAX;
        else return a + b;
    }

    /**
    * Subtracts two numbers and makes the result 0 if it is going to underflow.
    * @param a - The first number.
    * @param b - The second number.
    * @return The subtraction of the two numbers, with value 0 if there would be an underflow.
    */
    uintptr_t SubtractWithUnderflowCap(uintptr_t a, uintptr_t b)
    {
        return b <= a ? a - b : 0;
    }

    /**
    * Retrieves all locations could be allocated in given a page and address range.
    * @param minimumPtr - The minimum possible address for the buffer.
    * @param maximumPtr - The maximum possible address for the buffer.
    * @param pageStart - The start of the page.
    * @param pageEnd - The end of the page.
    * @param bufSize - The size of the buffer.
    * @param allocationGranularity - The allocation granularity.
    * @param results - Stores possible locations of the buffer, must be at least 4 elements long.
    * @return All possible locations for the buffer.
    */
    std::span<uintptr_t> GetPossibleBufferAddresses(
        uintptr_t minimumPtr, uintptr_t maximumPtr,
        uintptr_t pageStart, uintptr_t pageEnd,
        uintptr_t bufSize, int allocationGranularity,
        std::span<uintptr_t> results)
    {
        // Get range for page and min-max region.
        AddressRange minMaxRange(minimumPtr, maximumPtr);
        AddressRange pageRange(pageStart, pageEnd);

        // Check if there is any overlap at all.
        if (!pageRange.Overlaps(minMaxRange))
            return std::span<uintptr_t>();
        
        // Three possible cases here:
        //   1. Page fits entirely inside min-max range and is smaller.
        if (bufSize > pageRange.Size())
            return std::span<uintptr_t>();
    
        int numItems = 0;
    
        // Note: We have to test aligned to both page boundaries and min-max range boundaries;
        //       because, they may not perfectly overlap, e.g. min-max may be way greater than
        //       page size, so testing from start/end of that will not even overlap with available pages.
        //       Or the opposite can happen... min-max range may be smaller than page size.
    
        //   2. Min-max range is inside page, test aligned to page boundaries.
        // Round up from page min.
        uintptr_t pageMinAligned = RoundUp(pageRange.StartPointer, allocationGranularity);
        AddressRange pageMinRange(pageMinAligned, AddWithOverflowCap(pageMinAligned, bufSize));

        if (pageRange.Contains(pageMinRange) && minMaxRange.Contains(pageMinRange))
            results[numItems++] = pageMinRange.StartPointer;
    
        // Round down from page max.
        uintptr_t pageMaxAligned = RoundDown(SubtractWithUnderflowCap(pageRange.EndPointer, bufSize), allocationGranularity);
        AddressRange pageMaxRange(pageMaxAligned, pageMaxAligned + bufSize);
    
        if (pageRange.Contains(pageMaxRange) && minMaxRange.Contains(pageMaxRange))
            results[numItems++] = pageMaxRange.StartPointer;
    
        //   3. Min-max range is inside page, test aligned to Min-max range.
    
        // Round up from ptr min.
        uintptr_t ptrMinAligned = RoundUp(minimumPtr, allocationGranularity);
        AddressRange ptrMinRange(ptrMinAligned, AddWithOverflowCap(ptrMinAligned, bufSize));
    
        if (pageRange.Contains(ptrMinRange) && minMaxRange.Contains(ptrMinRange))
            results[numItems++] = ptrMinRange.StartPointer;
    
        // Round down from ptr max.
        uintptr_t ptrMaxAligned = RoundDown(SubtractWithUnderflowCap(maximumPtr, bufSize), allocationGranularity);
        AddressRange ptrMaxRange(ptrMaxAligned, ptrMaxAligned + bufSize);
    
        if (pageRange.Contains(ptrMaxRange) && minMaxRange.Contains(ptrMaxRange))
            results[numItems++] = ptrMaxRange.StartPointer;
    
        return results.subspan(0, numItems);
    }
    ```