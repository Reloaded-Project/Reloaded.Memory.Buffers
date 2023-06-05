using System;
using FluentAssertions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Internal;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests.Structs;

public unsafe class SafeLocatorItemTests
{
    [Fact]
    public void AppendData_BufferShouldContainData()
    {
        const int length = 10;

        // Arrange
        var buffer = stackalloc byte[length];
        var locatorItem = new LocatorItem((nuint)buffer, length);
        var safeLocatorItem = new SafeLocatorItem(&locatorItem);
        var testData = new Span<byte>(new byte[]{1, 2, 3, 4, 5});

        // Act
        safeLocatorItem.Append(testData);

        // Assert
        var result = new Span<byte>(buffer, length).Slice(0, testData.Length);
        Assert.True(result.SequenceEqual(testData));
    }

    [Fact]
    public void AppendBlittableData_ShouldBeAtCorrectAddress()
    {
        const int length = 4;

        // Arrange
        var buffer = stackalloc byte[length];
        var locatorItem = new LocatorItem((nuint)buffer, length);
        var safeLocatorItem = new SafeLocatorItem(&locatorItem);
        int testData = 123456;

        // Act
        nuint address = safeLocatorItem.Append(testData);

        // Assert
        int* ptr = (int*)address;
        (*ptr).Should().Be(testData);
    }
}
