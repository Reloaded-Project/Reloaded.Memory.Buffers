using System;
using System.Diagnostics;
using static Reloaded.Memory.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers.Internal.Utilities
{
    /// <summary/>
    public static unsafe class VirtualFreeUtility
    {
        /// <summary/>
        public delegate void VirtualFreeFunction(IntPtr processHandle, IntPtr address);

        /// <summary>
        /// Retrieves the function to use in place of VirtualFree.
        /// Returns VirtualFree if target is same process; else VirtualFreeEx
        /// </summary>
        /// <param name="targetProcess">The process which the VirtualFree call intends to target.</param>
        /// <returns>A delegate implementation of <see cref="VirtualFreeFunction"/></returns>
        public static VirtualFreeFunction GetVirtualFreeFunction(Process targetProcess)
        {
            // Get the VirtualQuery function implementation to use.
            // Local is faster and works for current process; Remote is for another process.
            VirtualFreeFunction virtualFreeFunction = VirtualFreeRemote;

            if (Process.GetCurrentProcess().Id == targetProcess.Id)
                virtualFreeFunction = VirtualFreeLocal;

            return virtualFreeFunction;
        }

        /*
         * Two implementations of VirtualAlloc for the VirtualAlloc Delegate
         * The Local one; runs it for the current process; being the faster of the two.
         * The Remote one; runs it for another process; being the slower of the two.
         */

        private static void VirtualFreeLocal(IntPtr processHandle, IntPtr address)
        {
            VirtualFree
            (
                address,
                (UIntPtr) 0,
                MEM_ALLOCATION_TYPE.MEM_RELEASE
            );
        }

        private static void VirtualFreeRemote(IntPtr processHandle, IntPtr address)
        {
            VirtualFreeEx
            (
                processHandle,
                address,
                (UIntPtr)0,
                MEM_ALLOCATION_TYPE.MEM_RELEASE
            );
        }
    }
}
