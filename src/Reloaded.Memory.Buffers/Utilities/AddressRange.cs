namespace Reloaded.Memory.Buffers.Utilities;

/// <summary>
///     Defines a physical address range with a minimum and maximum address.
/// </summary>
internal readonly struct AddressRange
{
    public readonly nuint StartPointer;
    public readonly nuint EndPointer;
    public nuint Size => EndPointer - StartPointer;

    public AddressRange(nuint startPointer, nuint endPointer)
    {
        StartPointer = startPointer;
        EndPointer = endPointer;
    }

    /// <summary>
    ///     Returns true if the other address range is completely inside
    ///     the current address range.
    /// </summary>
    /// <param name="otherRange">True if this address range is contained entirely inside the other.</param>
    public bool Contains(in AddressRange otherRange)
        => otherRange.StartPointer >= StartPointer && otherRange.EndPointer <= EndPointer;

    /// <summary>
    ///     Returns true if the other address range intersects this address range, i.e.
    ///     start or end of this range falls inside other range.
    /// </summary>
    /// <param name="otherRange">Returns true if there are any overlaps in the address ranges.</param>
    public bool Overlaps(in AddressRange otherRange)
    {
        if (PointInRange(otherRange, StartPointer)) return true;
        if (PointInRange(otherRange, EndPointer)) return true;
        if (PointInRange(this, otherRange.StartPointer)) return true;
        if (PointInRange(this, otherRange.EndPointer)) return true;

        return false;
    }

    /// <summary>
    ///     Returns true if a number "point", is between min and max of address range.
    /// </summary>
    /// <param name="range">Range inside which to test the point.</param>
    /// <param name="point">The point to test.</param>
    private bool PointInRange(in AddressRange range, nuint point)
        => point >= range.StartPointer && point <= range.EndPointer;
}
