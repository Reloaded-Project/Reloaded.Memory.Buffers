using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Internal.Structs;
using Reloaded.Memory.Buffers.Internal.Utilities;
using Reloaded.Memory.Sources;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// Provides a buffer for permanent (until the process dies) general small size memory storage, reusable 
    /// concurrently between different DLLs within the same process.
    /// </summary>
    public unsafe class MemoryBuffer : IDisposable
    {
        /// <summary>
        /// Stores the reference to a system-wide mutex which prevents concurrent access
        /// modifying/adding elements of the <see cref="MemoryBuffer"/>.
        /// </summary>
        private Mutex _bufferAddMutex;
        
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
            ----------
            Destructor
            ----------
        */

        /// <summary/>
        ~MemoryBuffer()
        {
            Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _bufferAddMutex?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets up the mutex to be used by this instance of the <see cref="MemoryBuffer"/>.
        /// The factory methods in <see cref="MemoryBufferFactory"/> SHOULD call this method.
        /// </summary>
        internal void SetupMutex(Process process)
        {
            try
            {
                _bufferAddMutex = Mutex.OpenExisting(GetMutexName(process));
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Mutex does not exist.
                _bufferAddMutex = new Mutex(false, GetMutexName(process));
            }
        }

        /// <summary>
        /// Generates the name of the named system-wide mutex for this class.
        /// </summary>
        internal string GetMutexName(Process process)
        {
            return $"Reloaded.Memory.Buffers.MemoryBuffer | PID: {process.Id} | Memory Address: {_headerAddress.ToString("X")}";
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
        /// <param name="alignment">The memory alignment of the item to be added to the buffer.</param>
        /// <returns>Pointer to the passed in bytes written to memory. Null pointer, if it cannot fit into the buffer.</returns>
        public IntPtr Add(byte[] bytesToWrite, int alignment = 4)
        {
            _bufferAddMutex.WaitOne();
            var bufferProperties = Properties;

            // Re-align the buffer before write operation.
            bufferProperties.SetAlignment(alignment);

            // Check if item can fit in buffer and buffer address is valid.
            if (Properties.Remaining < bytesToWrite.Length) // Inlined CanItemFit to prevent reading Properties from memory again.
                return IntPtr.Zero;

            // Append the item to the buffer.
            IntPtr appendAddress = bufferProperties.WritePointer;
            MemorySource.WriteRaw(appendAddress, bytesToWrite);
            bufferProperties.Offset += bytesToWrite.Length;
            Properties = bufferProperties;

            _bufferAddMutex.ReleaseMutex();
            return appendAddress;
        }

        /// <summary>
        /// Writes your own structure address into process' memory and gives you the address 
        /// to which the structure has been directly written to.
        /// </summary>
        /// <param name="bytesToWrite">A structure to be converted into individual bytes to be written onto the buffer.</param>
        /// <param name="marshalElement">Set this to true to marshal the given parameter before writing it to the buffer, else false.</param>
        /// <param name="alignment">The memory alignment of the item to be added to the buffer.</param>
        /// <returns>Pointer to the newly written structure in memory. Null pointer, if it cannot fit into the buffer.</returns>
        public IntPtr Add<TStructure>(ref TStructure bytesToWrite, bool marshalElement = false, int alignment = 4)
        {
            _bufferAddMutex.WaitOne();
            var bufferProperties = Properties;

            int structLength = Struct.GetSize<TStructure>(marshalElement);

            // Re-align the buffer before write operation.
            bufferProperties.SetAlignment(alignment);

            // Check if item can fit in buffer and buffer address is valid.
            if (Properties.Remaining < structLength) // Inlined CanItemFit to prevent reading Properties from memory again.
                return IntPtr.Zero;

            // Append the item to the buffer.
            IntPtr appendAddress = bufferProperties.WritePointer;
            MemorySource.Write(appendAddress, ref bytesToWrite, marshalElement);
            bufferProperties.Offset += structLength;            
            Properties = bufferProperties;

            _bufferAddMutex.ReleaseMutex();
            return appendAddress;
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

        /// <summary>
        /// Returns true if the object can fit into the buffer, else false.
        /// </summary>
        /// <param name="item">The item to check if it can fit into the buffer.</param>
        /// <param name="marshalElement">True if the item is to be marshalled, else false.</param>
        public bool CanItemFit<TGeneric>(ref TGeneric item, bool marshalElement = false)
        {
            return CanItemFit(Struct.GetSize<TGeneric>(marshalElement));
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
