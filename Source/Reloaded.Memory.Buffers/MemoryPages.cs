using System;
using System.Collections.Generic;
using System.Diagnostics;
using Reloaded.Memory.Buffers.Utilities;
using Vanara.PInvoke;

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
        public static List<Kernel32.MEMORY_BASIC_INFORMATION> GetPages(Process process)
        {
            // Is this Windows on Windows 64? (x86 app running on x64 Windows)
            Kernel32.IsWow64Process(process.Handle, out bool isWow64);
            Kernel32.GetSystemInfo(out Kernel32.SYSTEM_INFO systemInfo);

            // This should work.
            long currentAddress = 0;
            long maxAddress     = 0xFFFFFFFF; // 32bit (with Address Range Extension)

            // Check if 64bit.
            if (systemInfo.wProcessorArchitecture == ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 && !isWow64)
                maxAddress = (long) systemInfo.lpMaximumApplicationAddress;

            // Get the VirtualQuery function implementation to use.
            // Local is faster and works for current process; Remote is for another process.
            VirtualQueryUtility.VirtualQueryFunction virtualQueryFunction = VirtualQueryUtility.GetVirtualQueryFunction(process);
            
            // Shorthand for convenience.
            List<Kernel32.MEMORY_BASIC_INFORMATION> memoryPages = new List<Kernel32.MEMORY_BASIC_INFORMATION>();

            // Until we get all of the pages.
            while (currentAddress <= maxAddress)
            {
                // Get our info from VirtualQueryEx.
                var memoryInformation = virtualQueryFunction(process.Handle, (IntPtr)currentAddress);

                // Add the page and increment address iterator to go to next page.
                memoryPages.Add(memoryInformation);
                currentAddress += (long)memoryInformation.RegionSize.Value;
            }

            return memoryPages;
        }
    }
}
