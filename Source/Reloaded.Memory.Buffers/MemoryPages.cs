using System;
using System.Collections.Generic;
using System.Diagnostics;
using Reloaded.Memory.Buffers.Internal.Utilities;
using static Reloaded.Memory.Buffers.Internal.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// Provides various utility methods which allow for the query and retrieval of information regarding individual pages of
    /// RAM memory.
    /// </summary>
    public static unsafe class MemoryPages
    {
        /// <summary>
        /// Returns a list of pages that exist within a set process' memory.
        /// </summary>
        /// <returns></returns>
        public static List<MEMORY_BASIC_INFORMATION> GetPages(Process process)
        {
            // Is this Windows on Windows 64? (x86 app running on x64 Windows)
            IsWow64Process(process.Handle, out bool isWow64);
            GetSystemInfo(out SYSTEM_INFO systemInfo);

            // This should work.
            long currentAddress = 0;
            long maxAddress     = 0x7FFFFFFF; // 32bit (with Address Range Extension)

            // Check if 64bit.
            if (systemInfo.wProcessorArchitecture == ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 && !isWow64)
                maxAddress = (long) systemInfo.lpMaximumApplicationAddress;

            // Get the VirtualQuery function implementation to use.
            // Local is faster and works for current process; Remote is for another process.
            VirtualQueryUtility.VirtualQueryFunction virtualQueryFunction = VirtualQueryUtility.GetVirtualQueryFunction(process);
            
            // Shorthand for convenience.
            List<MEMORY_BASIC_INFORMATION> memoryPages = new List<MEMORY_BASIC_INFORMATION>(8192);

            // Until we get all of the pages.
            while (currentAddress <= maxAddress)
            {
                // Get our info from VirtualQueryEx.
                var memoryInformation = new MEMORY_BASIC_INFORMATION();
                virtualQueryFunction(process.Handle, (IntPtr)currentAddress, ref memoryInformation);

                // Add the page and increment address iterator to go to next page.
                memoryPages.Add(memoryInformation);
                currentAddress += (long)memoryInformation.RegionSize;
            }

            return memoryPages;
        }
    }
}
