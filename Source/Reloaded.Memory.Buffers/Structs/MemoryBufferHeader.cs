using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Memory.Buffers.Utilities;

namespace Reloaded.Memory.Buffers.Structs
{
    /// <summary>
    /// Contains the individual details of the memory buffer in question.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBufferHeader
    {
        /// <summary>Describes the current state of the buffer.</summary>
        public BufferState State;

        /// <summary> The location of the raw data stored in the buffer. </summary>
        public IntPtr DataPointer   { get; internal set; }

        /// <summary> Specifies the byte alignment of each item that will be added onto the buffer. </summary>
        public int Alignment        { get; private set; }

        /// <summary>Stores the current offset in the buffer, ranging from 0 to <see cref="Size"/>.</summary>
        public int Offset           { get; internal set; }

        /// <summary>Stores the size of the individual buffer in question.</summary>
        public int Size             { get; internal set; }

        /// <summary> Returns the remaining amount of space in the current buffer (in bytes). </summary>
        public int Remaining => Size - Offset;

        /// <summary> Returns the current write pointer in the buffer. (Address of next element to be written) </summary>
        public IntPtr WritePointer => DataPointer + Offset;

        /// <summary/>
        public enum BufferState
        {
            /// <summary>The buffer is currently not being written to/read from.</summary>
            Unlocked,

            /// <summary>The buffer is currently being written to/read from.</summary>
            Locked
        }

        /// <summary>
        /// Creates a new <see cref="MemoryBufferHeader"/> given the location of the raw data
        /// and the amount of raw data available at that location.
        /// </summary>
        /// <param name="dataPointer">Pointer to raw data (normally following this header).</param>
        /// <param name="size">The amount of data available at the given pointer, in bytes.</param>
        public MemoryBufferHeader(IntPtr dataPointer, int size)
        {
            this.Alignment      = 4;
            this.DataPointer    = dataPointer;
            this.Offset         = 0;
            this.Size           = size;
            State               = BufferState.Unlocked;
        }

        /// <summary>
        /// Sets a new alignment (in bytes) for the buffer.
        /// Note that setting the alignment will move the buffer offset to the next multiple of "alignment",
        /// unless it is already aligned.
        /// </summary>
        public void SetAlignment(int alignment)
        {
            Alignment = alignment;
            Align();
        }

        /// <summary>
        /// Locks the buffer with a flag.
        /// Use this before any memory read/write and <see cref="Unlock"/> when done.
        /// </summary>
        internal void Lock()
        {
            State = BufferState.Locked;
        }

        /// <summary>
        /// Unlocks the buffer with a flag to allow for other applications (DLLs)
        /// within the same process to access the memory.
        /// </summary>
        internal void Unlock()
        {
            State = BufferState.Unlocked;
        }

        /// <summary>
        /// Aligns the buffer offset to its current <see cref="Alignment"/> value.
        /// </summary>
        internal void Align()
        {
            Mathematics.RoundUp(Offset, Alignment);
        }
    }
}
