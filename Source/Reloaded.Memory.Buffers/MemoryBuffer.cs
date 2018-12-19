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
        /// <summary>
        /// Stores the reference to a system-wide mutex which prevents concurrent access
        /// modifying/adding elements of the <see cref="MemoryBuffer"/>.
        /// </summary>
        private Mutex _bufferMutex;
        
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

        /// <summary>
        /// Sets up the mutex to be used by this instance of the <see cref="MemoryBuffer"/>.
        /// The factory methods in <see cref="MemoryBufferFactory"/> SHOULD call this method.
        /// </summary>
        internal void SetupMutex(Process process)
        {
            try
            {
                _bufferMutex = Mutex.OpenExisting(GetMutexName(process));
            }
            catch (WaitHandleCannotBeOpenedException ex)
            {
                // Mutex does not exist.
                _bufferMutex = new Mutex(false, GetMutexName(process));
            }
        }

        /// <summary>
        /// Generates the name of the named system-wide mutex for this class.
        /// </summary>
        internal string GetMutexName(Process process)
        {
            return $"Reloaded.Memory.Buffers | PID: {process.Id} | Memory Address: {_headerAddress.ToString("X")}";
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
            _bufferMutex.WaitOne();
            var bufferProperties = Properties;

            // Check if item can fit in buffer and buffer address is valid.
            if (!CanItemFit(bytesToWrite.Length) || _headerAddress == IntPtr.Zero)
                return IntPtr.Zero;

            // Append the item to the buffer.
            IntPtr appendAddress = bufferProperties.WritePointer;
            MemorySource.WriteRaw(appendAddress, bytesToWrite);
            bufferProperties.Offset += bytesToWrite.Length;

            // Re-align the buffer for next write operation, unlock and write back to memory.
            bufferProperties.Align();
            Properties = bufferProperties;

            _bufferMutex.ReleaseMutex();

            return appendAddress;
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
