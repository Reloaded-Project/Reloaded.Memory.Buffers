using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Buffers.Internal.Utilities;

namespace Reloaded.Memory.Buffers
{
    /// <summary>
    /// Contains the individual details of the memory buffer in question.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBufferProperties
    {
        /// <summary> The location of the raw data stored in the buffer. </summary>
        public nuint DataPointer   { get; internal set; }

        /// <summary> Specifies the byte alignment of each item that will be added onto the buffer. </summary>
        public int Alignment        { get; private set; }

        /// <summary>Stores the current offset in the buffer, ranging from 0 to <see cref="Size"/>.</summary>
        public int Offset           { get; internal set; }

        /// <summary>Stores the size of the individual buffer in question.</summary>
        public int Size             { get; internal set; }

        /// <summary> Returns the remaining amount of space in the current buffer (in bytes). </summary>
        public int Remaining => Size - Offset;

        /// <summary> Returns the current write pointer in the buffer. (Address of next element to be written) </summary>
        public nuint WritePointer => (UIntPtr)DataPointer + Offset;

        /// <summary>
        /// Creates a new <see cref="MemoryBufferProperties"/> given the location of the raw data
        /// and the amount of raw data available at that location.
        /// </summary>
        /// <param name="dataPointer">Pointer to raw data (normally following this header).</param>
        /// <param name="size">The amount of data available at the given pointer, in bytes.</param>
        public MemoryBufferProperties(nuint dataPointer, int size)
        {
            this.Alignment      = 4;
            this.DataPointer    = dataPointer;
            this.Offset         = 0;
            this.Size           = size;
        }

        /// <summary>
        /// Sets a new alignment (in bytes) for the buffer and auto-aligns the buffer.
        /// Note that setting the alignment will move the buffer offset to the next multiple of "alignment",
        /// unless it is already aligned.
        /// </summary>
        internal void SetAlignment(int alignment)
        {
            Alignment = alignment;
            Align();
        }

        /// <summary>
        /// Aligns the buffer offset to its current <see cref="Alignment"/> value.
        /// </summary>
        internal void Align()
        {
            Offset = (int)(Mathematics.RoundUp((ulong)WritePointer, (ulong)Alignment) - (ulong) DataPointer);
        }
    }
}
