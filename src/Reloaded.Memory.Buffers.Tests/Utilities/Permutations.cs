using System.Collections.Generic;
using System.Linq;

namespace Reloaded.Memory.Buffers.Tests.Utilities;

public static class Permutations
{
    /// <summary>
    ///     Returns values for bit packing tests. <br />
    ///     This function returns values from numBits 0 to <paramref name="maxBits" /> such that: <br /><br />
    ///     Value 0: 0b1<br />
    ///     Value 1: 0b11<br />
    ///     Value 2: 0b111<br />
    ///     Value 3: 0b1111<br />
    ///     etc.<br />
    ///     These values are used for testing individual bit packed values do not overlap.
    /// </summary>
    public static IEnumerable<long> GetBitPackingOverlapTestValues(int maxBits)
    {
        long value = 1;
        for (var x = 0; x < maxBits; x++)
        {
            yield return value;
            value <<= 1;
            value |= 1;
        }
    }

    /// <summary>
    ///     Gets the value that would appear at index <paramref name="numBits" /> of
    ///     <see cref="GetBitPackingOverlapTestValues" />.
    /// </summary>
    public static long GetBitPackingOverlapTestValue(int numBits)
    {
        long value = 1;
        for (var x = 0; x < numBits - 1; x++)
        {
            value <<= 1;
            value |= 1;
        }

        return value;
    }

    /// <summary>
    ///     Retrieves all permutations of a given collection.
    /// </summary>
    public static IEnumerable<T[]> GetPermutations<T>(this IEnumerable<T> elements)
    {
        List<T> elementList = elements.ToList();
        var indexList = Enumerable.Range(0, elementList.Count).ToArray();

        yield return elementList.ToArray();
        while (true)
        {
            var i = elementList.Count - 1;
            while (i > 0 && indexList[i - 1] >= indexList[i])
                i--;

            if (i <= 0)
                break;

            var j = elementList.Count - 1;
            while (indexList[j] <= indexList[i - 1])
                j--;

            Swap(indexList, i - 1, j);
            j = elementList.Count - 1;
            while (i < j)
            {
                Swap(indexList, i, j);
                i++;
                j--;
            }

            yield return indexList.Select(x => elementList[x]).ToArray();
        }
    }

    private static void Swap<T>(T[] array, int i, int j) => (array[i], array[j]) = (array[j], array[i]);
}
