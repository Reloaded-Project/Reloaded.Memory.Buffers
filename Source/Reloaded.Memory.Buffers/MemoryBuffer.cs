using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Internal.Structs;
using Reloaded.Memory.Buffers.Internal.Utilities;
using Reloaded.Memory.Sources;
using Vanara.PInvoke;
using static Reloaded.Memory.Buffers.Internal.Utilities.VirtualQueryUtility;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// Provides a buffer for permanent (until the process dies) general small size memory storage, reusable 
    /// concurrently between different DLLs within the same process.
    /// </summary>
    public unsafe class MemoryBuffer
    {
        /// <summary> Defines where Memory will be read in or written to. </summary>
        public IMemory MemorySource   { get; private set; }

        /// <summary> Gets/Sets the header/properties of the buffer stored in unmanaged memory. </summary>
        public MemoryBufferProperties Properties
        {
            get
            {
                MemorySource.SafeRead(_headerAddress, out MemoryBufferProperties bufferHeader);
                return bufferHeader;
            }
            set => MemorySource.Write(_headerAddress, ref value);
        }

        /// <summary> Stores the location of the <see cref="MemoryBufferProperties"/> structure. </summary>
        private readonly IntPtr _headerAddress;

        /// <summary> Used to ensure only one thread in the current application can access buffer at once. </summary>
        private readonly object _threadLock = new object();

        /*
            --------------
            Constructor(s)
            --------------
        */

        internal MemoryBuffer(IMemory memorySource, IntPtr headerAddress)
        {
            _headerAddress = headerAddress;
            MemorySource   = memorySource;
        }

        internal MemoryBuffer(IMemory memorySource, IntPtr headerAddress, MemoryBufferProperties memoryBufferProperties) : this(memorySource, headerAddress)
        {
            Properties = memoryBufferProperties;
        }

        /*
            --------------
            Core Functions
            --------------
        */

        /// <summary>
        /// Writes your own memory bytes into process' memory and gives you the address
        /// for the memory location of the written bytes.
        /// </summary>
        /// <param name="bytesToWrite">Individual bytes to be written onto the buffer.</param>
        /// <returns>Pointer to the passed in bytes written to memory. Null pointer, if it cannot fit into the buffer.</returns>
        public IntPtr Add(byte[] bytesToWrite)
        {
            /* A lock for the threads of the current application (DLL); just in case as extra backup. */
            lock (_threadLock)
            {
                /* The following is application (DLL) lock to ensure that various different modules
                   do not try reading/writing to the same buffer at once.
                */
                while (Properties.State == MemoryBufferProperties.BufferState.Locked)
                    Thread.Sleep(1);

                // Lock the buffer and locally store header for modification.
                var bufferProperties = Properties;

                /* Below we lock the buffer. Note that we cannot access the struct by pointer as it
                   may be in another process (remember we are using arbitrary memory sources) */
                bufferProperties.Lock();
                Properties = bufferProperties;

                // Check if item can fit in buffer and buffer address is valid.
                if (!CanItemFit(bytesToWrite.Length) || _headerAddress == IntPtr.Zero)
                {
                    bufferProperties.Unlock();
                    Properties = bufferProperties;
                    return IntPtr.Zero;
                }

                // Append the item to the buffer.
                IntPtr appendAddress = bufferProperties.WritePointer;
                MemorySource.WriteRaw(appendAddress, bytesToWrite);
                bufferProperties.Offset += bytesToWrite.Length;

                // Re-align, unlock and write back to memory.
                bufferProperties.Align();
                bufferProperties.Unlock();
                Properties = bufferProperties;

                return appendAddress;
            }
        }

        /// <summary>
        /// Writes your own structure address into process' memory and gives you the address 
        /// to which the structure has been directly written to.
        /// </summary>
        /// <param name="bytesToWrite">A structure to be converted into individual bytes to be written onto the buffer.</param>
        /// <param name="marshalElement">Set this to true to marshal the given parameter before writing it to the buffer, else false.</param>
        /// <returns>Pointer to the newly written structure in memory. Null pointer, if it cannot fit into the buffer.</returns>
        public IntPtr Add<TStructure>(ref TStructure bytesToWrite, bool marshalElement = false)
        {
            return Add(Struct.GetBytes(ref bytesToWrite, marshalElement));
        }

        /// <summary>
        /// Returns true if the object can fit into the buffer, else false.
        /// </summary>
        /// <param name="objectSize">The size of the object to be appended to the buffer.</param>
        /// <returns>Returns true if the object can fit into the buffer, else false.</returns>
        public bool CanItemFit(int objectSize)
        {
            // Check if base buffer uninitialized or if object size too big.
            return Properties.Remaining >= objectSize;
        }

        /*
            --------------
            Misc Functions
            --------------
        */

        /// <summary>
        /// [Testing use only]
        /// The address where the individual buffer has been allocated.
        /// </summary>
        internal IntPtr AllocationAddress => _headerAddress - sizeof(MemoryBufferMagic);

        /// <summary/>
        public override bool Equals(object obj)
        {
            // The two <see cref="MemoryBuffer"/>s are equal if their base address is the same.
            var buffer = obj as MemoryBuffer;
            return buffer != null && _headerAddress == buffer._headerAddress;
        }

        /// <summary/>
        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            return (int)_headerAddress;
        }
    }
}
