using System;
using System.Collections.Generic;
using System.Text;

namespace Reloaded.Memory.Buffers.Structs
{
    /// <summary>
    /// Stores the properties which define a buffer allocation; i.e. where memory can be allocated.
    /// </summary>
    public struct BufferAllocationProperties
    {
        /// <summary>
        /// The address of where memory may be allocated. 
        /// </summary>
        public IntPtr MemoryAddress;

        /// <summary>
        /// The amount of bytes it is possible to allocate.
        /// </summary>
        public int Size;

        /// <summary>
        /// Creates a new instance of <see cref="BufferAllocationProperties"/>.
        /// </summary>
        public BufferAllocationProperties(IntPtr memoryAddress, int size)
        {
            MemoryAddress = memoryAddress;
            Size = size;
        }
    }
}
