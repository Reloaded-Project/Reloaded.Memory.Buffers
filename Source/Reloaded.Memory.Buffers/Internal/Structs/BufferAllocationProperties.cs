﻿using System;

namespace Reloaded.Memory.Buffers.Internal.Structs
{
    /// <summary>
    /// Stores the properties which define a buffer allocation; i.e. where memory can be allocated.
    /// </summary>
    public struct BufferAllocationProperties
    {
        /// <summary> The address of where memory may be allocated. </summary>
        public nuint MemoryAddress;

        /// <summary> The amount of bytes it is possible to allocate. </summary>
        public int Size;

        /// <summary>
        /// Creates a new instance of <see cref="BufferAllocationProperties"/>.
        /// </summary>
        public BufferAllocationProperties(nuint memoryAddress, int size)
        {
            MemoryAddress = memoryAddress;
            Size = size;
        }
    }
}
