namespace Reloaded.Memory.Buffers.Utilities;

internal static class Mathematics
{
    /// <summary>
    /// Rounds up a specified number to the next multiple of X.
    /// </summary>
    /// <param name="number">The number to round up.</param>
    /// <param name="multiple">The multiple the number should be rounded to.</param>
    /// <returns></returns>
    internal static nuint RoundUp(nuint number, nuint multiple)
    {
        if (multiple == 0)
            return number;

        nuint remainder = number % multiple;
        if (remainder == 0)
            return number;

        return number + multiple - remainder;
    }

    /// <summary>
    /// Rounds up a specified number to the previous multiple of X.
    /// </summary>
    /// <param name="number">The number to round down.</param>
    /// <param name="multiple">The multiple the number should be rounded to.</param>
    internal static nuint RoundDown(nuint number, nuint multiple)
    {
        if (multiple == 0)
            return number;

        nuint remainder = number % multiple;
        if (remainder == 0)
            return number;

        return number - remainder;
    }

    /// <summary>
    /// Rounds up a specified number to the previous multiple of X.
    /// </summary>
    /// <param name="number">The number to round down.</param>
    /// <param name="multiple">The multiple the number should be rounded to.</param>
    /// <returns></returns>
    internal static nuint RoundDown(nuint number, int multiple) => RoundDown(number, (nuint)multiple);

    /// <summary>
    /// Rounds up a specified number to the next multiple of X.
    /// </summary>
    /// <param name="number">The number to round up.</param>
    /// <param name="multiple">The multiple the number should be rounded to.</param>
    internal static nuint RoundUp(nuint number, int multiple) => RoundUp(number, (nuint)multiple);

    /// <summary>
    /// Returns smaller of the two values.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    internal static nuint Min(nuint a, nuint b) => a < b ? a : b;

    /// <summary>
    /// Adds the two values, but caps the result at MaxValue if it overflows.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    internal static nuint AddWithOverflowCap(nuint a, nuint b)
    {
        var max = unchecked((nuint)(-1));
        if (max - a >= b)
            return a + b;

        return max;
    }

    /// <summary>
    /// Subtracts the two values, but caps the result at MinValue if it overflows.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    public static nuint SubtractWithUnderflowCap(nuint a, nuint b) => b <= a ? a - b : 0;
}
