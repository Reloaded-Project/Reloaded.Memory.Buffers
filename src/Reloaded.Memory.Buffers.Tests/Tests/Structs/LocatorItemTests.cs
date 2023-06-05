using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Reloaded.Memory.Buffers.Structs.Internal;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests.Structs;

[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
public class LocatorItemTests
{
    [Fact]
    public unsafe void IsCorrectSize()
    {
        var expected = IntPtr.Size == 4 ? 16 : 20;
        sizeof(LocatorItem).Should().Be(expected);
    }

    [Fact]
    public void TryLock_ShouldLockItem_WhenLockIsAvailable()
    {
        // Arrange
        var item = new LocatorItem();

        // Act
        var result = item.TryLock();

        // Assert
        result.Should().BeTrue();
        item.IsTaken.Should().BeTrue();
    }

    [Fact]
    public void TryLock_ShouldNotLockItem_WhenLockIsAlreadyAcquired()
    {
        // Arrange
        var item = new LocatorItem();
        item.TryLock();

        // Act
        var result = item.TryLock();

        // Assert
        result.Should().BeFalse();
        item.IsTaken.Should().BeTrue();
    }

    [Fact]
    public void Lock_ShouldAcquireLock_WhenLockIsAvailable()
    {
        // Arrange
        var item = new LocatorItem();

        // Act
        item.Lock();

        // Assert
        item.IsTaken.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldReleaseLock_WhenItemIsLocked()
    {
        // Arrange
        var item = new LocatorItem();
        item.Lock();

        // Act
        item.Unlock();

        // Assert
        item.IsTaken.Should().BeFalse();
    }

#if DEBUG
    // Debug only feature.
    [Fact]
    public void Unlock_ShouldThrowException_WhenItemIsNotLocked()
    {
        // Arrange
        var item = new LocatorItem();

        // Act
        var action = new Action(() => item.Unlock());

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Attempted to unlock a LocatorItem that wasn't locked");
    }
#endif

    [Theory]
    [InlineData(0, 0, 0)]  // Case when BaseAddress is 0
    [InlineData(10, 20, 10)] // Case when BaseAddress is not 0
    public void MinAddress_ShouldReturnBaseAddress_WhenCalled(int baseAddress, uint size, int expected)
    {
        // Arrange
        var item = new LocatorItem { BaseAddress = (nuint)baseAddress, Size = size };

        // Act
        var result = item.MinAddress;

        // Assert
        result.Should().Be((nuint)expected);
    }

    [Theory]
    [InlineData(0, 0, 0)]  // Case when BaseAddress is 0
    [InlineData(10, 20, 30)] // Case when BaseAddress is not 0
    public void MaxAddress_ShouldReturnSumOfBaseAddressAndSize_WhenCalled(int baseAddress, uint size, int expected)
    {
        // Arrange
        var item = new LocatorItem { BaseAddress = (nuint)baseAddress, Size = size };

        // Act
        var result = item.MaxAddress;

        // Assert
        result.Should().Be((nuint)expected);
    }

    [Theory]
    [InlineData(50, 100, 200, 50, true)] // size needed is available
    [InlineData(50, 100, 200, 80, false)] // size needed is not available
    [InlineData(50, 100, 200, 300, false)] // size needed is beyond maxAddress
    [InlineData(0, 100, 150, 0, true)] // no size needed, and at start
    public void CanUse_ShouldReturnExpectedResult(int position, uint baseAddress, uint maxAddress, uint size, bool expected)
    {
        // Arrange
        var locatorItem = new LocatorItem
        {
            BaseAddress = baseAddress,
            Position = (uint)position,
            Size = maxAddress - baseAddress,
        };

        // Act
        bool result = locatorItem.CanUse(size, baseAddress, maxAddress);

        // Assert
        result.Should().Be(expected);
    }
}
