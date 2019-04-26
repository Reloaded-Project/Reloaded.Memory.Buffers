using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Sources;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// A type of <see cref="MemoryBuffer"/> which cannot be found by others (non-shared) and can be disposed of.
    /// </summary>
    public unsafe class PrivateMemoryBuffer : MemoryBuffer, IDisposable
    {
        /* Constructors */
        internal PrivateMemoryBuffer(IMemory memorySource, IntPtr headerAddress, MemoryBufferProperties memoryBufferProperties) : base(memorySource, headerAddress, memoryBufferProperties) { }

        /// <summary>
        /// Destroys this object.
        /// </summary>
        ~PrivateMemoryBuffer()
        {
            Dispose();
        }

        /*
            ------------------------
            Additional Functionality
            ------------------------
        */

        /// <summary>
        /// Disposes of the memory used by this buffer.
        /// </summary>
        public new void Dispose()
        {
            MemorySource.Free(AllocationAddress);
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
