using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using static Reloaded.Memory.Buffers.Internal.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers.Internal.Utilities
{
    /// <summary/>
    public static unsafe class VirtualQueryUtility
    {
        /* Custom Kernel32 DLLImport statements for performance uptick. */

        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern UIntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern UIntPtr VirtualQuery(IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);

        /// <summary/>
        public delegate void VirtualQueryFunction(IntPtr processHandle, IntPtr address, ref MEMORY_BASIC_INFORMATION memoryInformation);

        /// <summary>
        /// Retrieves the function to use in place of VirtualQuery.
        /// Returns VirtualQuery if target is same process; else VirtualQueryEx
        /// </summary>
        /// <param name="targetProcess">The process which the VirtualQuery call intends to target.</param>
        /// <returns>A delegate implementation of <see cref="VirtualQueryFunction"/></returns>
        public static VirtualQueryFunction GetVirtualQueryFunction(Process targetProcess)
        {
            // Get the VirtualQuery function implementation to use.
            // Local is faster and works for current process; Remote is for another process.
            VirtualQueryFunction virtualQueryFunction = VirtualQueryRemote;

            if (Process.GetCurrentProcess().Id == targetProcess.Id)
                virtualQueryFunction = VirtualQueryLocal;

            return virtualQueryFunction;
        }

        /*
         * Two implementations of VirtualQuery for the VirtualQuery Delegate
         * The Local one; runs it for the current process; being the faster of the two.
         * The Remote one; runs it for another process; being the slower of the two.
         */

        private static void VirtualQueryLocal(IntPtr processHandle, IntPtr address, ref MEMORY_BASIC_INFORMATION memoryInformation)
        {
            VirtualQuery(address, ref memoryInformation, (UIntPtr) sizeof(MEMORY_BASIC_INFORMATION));
        }

        private static void VirtualQueryRemote(IntPtr processHandle, IntPtr address, ref MEMORY_BASIC_INFORMATION memoryInformation)
        {
            VirtualQueryEx(processHandle, address, ref memoryInformation, (UIntPtr) sizeof(MEMORY_BASIC_INFORMATION));
        }
    }
}
