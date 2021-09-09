namespace Reloaded.Memory.Buffers.Internal.Utilities
{
    internal static class Mathematics
    {
        /// <summary>
        /// Rounds up a specified number to the next multiple of X.
        /// </summary>
        /// <param name="number">The number to round up.</param>
        /// <param name="multiple">The multiple the number should be rounded to.</param>
        /// <returns></returns>
        internal static int RoundUp(int number, int multiple)
        {
            if (multiple == 0)
                return number;

            int remainder = number % multiple;
            if (remainder == 0)
                return number;

            return number + multiple - remainder;
        }


        /// <summary>
        /// Rounds up a specified number to the next multiple of X.
        /// </summary>
        /// <param name="number">The number to round up.</param>
        /// <param name="multiple">The multiple the number should be rounded to.</param>
        /// <returns></returns>
        internal static long RoundUp(long number, long multiple)
        {
            if (multiple == 0)
                return number;

            long remainder = number % multiple;
            if (remainder == 0)
                return number;

            return number + multiple - remainder;
        }

        /// <summary>
        /// Rounds up a specified number to the next multiple of X.
        /// </summary>
        /// <param name="number">The number to round up.</param>
        /// <param name="multiple">The multiple the number should be rounded to.</param>
        /// <returns></returns>
        internal static ulong RoundUp(ulong number, ulong multiple)
        {
            if (multiple == 0)
                return number;

            ulong remainder = number % multiple;
            if (remainder == 0)
                return number;

            return number + multiple - remainder;
        }

        /// <summary>
        /// Rounds up a specified number to the previous multiple of X.
        /// </summary>
        /// <param name="number">The number to round down.</param>
        /// <param name="multiple">The multiple the number should be rounded to.</param>
        /// <returns></returns>
        internal static long RoundDown(long number, long multiple)
        {
            if (multiple == 0)
                return number;

            long remainder = number % multiple;
            if (remainder == 0)
                return number;

            return number - remainder;
        }
    }
}
