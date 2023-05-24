using FluentAssertions;

namespace Reloaded.Memory.Buffers.Tests.Utilities;

/// <summary>
///     Helpers for packing tests.
/// </summary>
public class PackingTestHelpers
{
    public static void TestPackedProperties<TStruct>(
        ref TStruct instance,
        SetPropertyByRefDelegate<TStruct, long> setProperty1,
        GetPropertyByRefDelegate<TStruct, long> getProperty1,
        SetPropertyByRefDelegate<TStruct, long> setProperty2,
        GetPropertyByRefDelegate<TStruct, long> getProperty2,
        long value1,
        long value2) where TStruct : struct
    {
        // Assert Basic Packing
        setProperty1(ref instance, value1);
        getProperty1(ref instance).Should().Be(value1);

        setProperty2(ref instance, value2);
        getProperty2(ref instance).Should().Be(value2);

        // Verify Property1 and Property2 can be packed

        // Now verify Property2 didn't override Property1
        getProperty1(ref instance).Should().Be(value1);

        // Now write Property1 and ensure Property2 is unmodified
        setProperty1(ref instance, value1);
        getProperty2(ref instance).Should().Be(value2);
    }

    public static void AssertSizeBits<TStruct>(
        ref TStruct instance,
        SetPropertyByRefDelegate<TStruct, long> setProperty,
        GetPropertyByRefDelegate<TStruct, long> getProperty,
        int numBits) where TStruct : struct
    {
        // Test claimed numbits.
        foreach (var testValue in Permutations.GetBitPackingOverlapTestValues(numBits))
        {
            setProperty(ref instance, testValue);
            getProperty(ref instance).Should().Be(testValue);
        }

        // Numbits + 1 should overflow.
        var overflowValue = Permutations.GetBitPackingOverlapTestValue(numBits + 1);
        setProperty(ref instance, overflowValue);
        getProperty(ref instance).Should().NotBe(overflowValue);
    }

    public delegate void SetPropertyByRefDelegate<TStruct, in TValue>(ref TStruct instance, TValue value)
        where TStruct : struct;

    public delegate TValue GetPropertyByRefDelegate<TStruct, out TValue>(ref TStruct instance) where TStruct : struct;
}
