using System;
using System.Collections.Generic;
using System.Text;

namespace Reloaded.Memory.Buffers.Structs
{
    /// <summary>
    /// Defines a physical address range with a minimum and maximum address.
    /// </summary>
    internal struct AddressRange
    {
        public long StartPointer;
        public long EndPointer;

        public AddressRange(long startPointer, long endPointer)
        {
            StartPointer = startPointer;
            EndPointer = endPointer;
        }

        /// <summary>
        /// Returns true if the other address range is completely inside
        /// the current address range.
        /// </summary>
        public bool Contains(ref AddressRange otherRange)
        {
            if (otherRange.StartPointer >= this.StartPointer &&
                otherRange.EndPointer   <= this.EndPointer)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the other address range intersects another address range, i.e.
        /// start or end of this range falls inside other range.
        /// </summary>
        public bool Overlaps(ref AddressRange otherRange)
        {
            if (this.StartPointer >= otherRange.StartPointer &&
                this.StartPointer <= otherRange.EndPointer)
                return true;

            if (this.EndPointer >= otherRange.StartPointer &&
                this.EndPointer <= otherRange.EndPointer)
                return true;
            
            return false;
        }
    }
}
