namespace Reloaded.Memory.Buffers.Internal.Structs
{
    /// <summary>
    /// Defines a physical address range with a minimum and maximum address.
    /// </summary>
    internal struct AddressRange
    {
        public nuint StartPointer;
        public nuint EndPointer;

        public AddressRange(nuint startPointer, nuint endPointer)
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
            if (PointInRange(ref otherRange, this.StartPointer))
                return true;

            if (PointInRange(ref otherRange, this.EndPointer))
                return true;

            if (PointInRange(ref this, otherRange.StartPointer))
                return true;

            if (PointInRange(ref this, otherRange.EndPointer))
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if a number "point", is between min and max of address range.
        /// </summary>
        private bool PointInRange(ref AddressRange range, nuint point)
        {
            if (point >= range.StartPointer &&
                point <= range.EndPointer)
                return true;

            return false;
        }
    }
}
