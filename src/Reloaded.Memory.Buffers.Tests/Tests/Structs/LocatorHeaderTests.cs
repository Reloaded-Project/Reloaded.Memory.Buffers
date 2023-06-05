using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Tests.Attributes;
using Reloaded.Memory.Buffers.Tests.Utilities;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;
// ReSharper disable RedundantCast

namespace Reloaded.Memory.Buffers.Tests.Tests.Structs;

[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
public class LocatorHeaderTests
{
    [Fact]
    public unsafe void IsCorrectSize()
    {
        var expected = IntPtr.Size == 4 ? 14 : 22;
        sizeof(LocatorHeader).Should().Be(expected);
    }

    [Fact]
    public void HasCorrectMaxItemCount()
    {
        var expected = IntPtr.Size == 4 ? 255 : 203;
        LocatorHeader.MaxItemCount.Should().Be(expected);
    }

    [Fact]
    public void TryLock_ShouldLockHeader_WhenLockIsAvailable()
    {
        // Arrange
        var header = new LocatorHeader();

        // Act
        var result = header.TryLock();

        // Assert
        result.Should().BeTrue();
        header.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void TryLock_ShouldNotLockHeader_WhenLockIsAlreadyAcquired()
    {
        // Arrange
        var header = new LocatorHeader();
        header.TryLock();

        // Act
        var result = header.TryLock();

        // Assert
        result.Should().BeFalse();
        header.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Lock_ShouldAcquireLock_WhenLockIsAvailable()
    {
        // Arrange
        var header = new LocatorHeader();

        // Act
        header.Lock();

        // Assert
        header.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldReleaseLock_WhenHeaderIsLocked()
    {
        // Arrange
        var header = new LocatorHeader();
        header.Lock();

        // Act
        header.Unlock();

        // Assert
        header.IsLocked.Should().BeFalse();
    }

    [Theory]
    [AutoLocatorHeader(true)]
    internal void VersionShouldBe3Bits(LocatorHeader header) => PackingTestHelpers.AssertSizeBits(
        ref header,
        (ref LocatorHeader instance, long value) => instance.Version = (byte)value,
        (ref LocatorHeader instance) => instance.Version,
        3);

#if DEBUG
    // Debug only feature.
    [Fact]
    public void Unlock_ShouldThrowException_WhenHeaderIsNotLocked()
    {
        // Arrange
        var header = new LocatorHeader();

        // Act
        var action = new Action(() => header.Unlock());

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Attempted to unlock a LocatorHeader that wasn't locked");
    }
#endif

    [Fact]
    public unsafe void GetFirstAvailableItemLocked_ShouldReturnExpectedResult()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;

        header->ThisAddress = header;
        header->NumItems = 2;

        LocatorItem* firstItem = header->GetFirstItem();
        firstItem->BaseAddress = 100;
        firstItem->Size = 50;
        firstItem->Position = 25;

        LocatorItem* secondItem = header->GetItem(1);
        secondItem->BaseAddress = 200;
        secondItem->Size = 50;
        secondItem->Position = 25;

        // Act
        using var result = header->GetFirstAvailableItemLocked(25, 100, 300)!.Value;

        // Assert
        result.Should().NotBeNull();
        result.Item->BaseAddress.Should().Be((nuint)100);
        result.Item->IsTaken.Should().BeTrue();
    }

    [Fact]
    public unsafe void GetFirstAvailableItemLocked_ShouldReturnNullIfNoAvailableItem_BecauseSizeIsInsufficient()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;

        header->ThisAddress = header;
        header->NumItems = 2;

        LocatorItem* firstItem = header->GetFirstItem();
        firstItem->BaseAddress = 100;
        firstItem->Size = 50;
        firstItem->Position = 30;

        LocatorItem* secondItem = header->GetItem(1);
        secondItem->BaseAddress = 200;
        secondItem->Size = 50;
        secondItem->Position = 30;

        // Act
        using var result = header->GetFirstAvailableItemLocked(25, 100, 300);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public unsafe void GetFirstAvailableItemLocked_ShouldReturnNullIfNoAvailableItem_BecauseNoBufferFitsRange()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;

        header->ThisAddress = header;
        header->NumItems = 2;

        LocatorItem* firstItem = header->GetFirstItem();
        firstItem->BaseAddress = 100;
        firstItem->Size = 50;
        firstItem->Position = 0;

        LocatorItem* secondItem = header->GetItem(1);
        secondItem->BaseAddress = 200;
        secondItem->Size = 50;
        secondItem->Position = 0;

        // Act
        using var result = header->GetFirstAvailableItemLocked(25, 0, 100);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public unsafe void TryAllocateItem_ShouldAllocateItem_WhenHeaderIsNotFullAndWithinAddressLimits()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;
        header->Initialize(LocatorHeader.Length);

        uint size = 100;
        nuint minAddress = Cached.GetMaxAddress() / 2;
        nuint maxAddress = Cached.GetMaxAddress();

        // Act
        bool result = header->TryAllocateItem(size, minAddress, maxAddress, out var item);

        // Assert
        result.Should().BeTrue();
        item.Should().NotBeNull();
        item!.Value.Item->Size.Should().BeGreaterOrEqualTo(size);
        Assert.True(item.Value.Item->BaseAddress >= minAddress);
        Assert.True(item.Value.Item->BaseAddress <= maxAddress);
        item.Value.Dispose();
    }

    [Fact]
    public unsafe void TryAllocateItem_ShouldNotAllocateItem_WhenHeaderIsFull()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;
        header->Initialize(LocatorHeader.Length);
        header->NumItems = (byte)LocatorHeader.MaxItemCount;

        uint size = 100;
        nuint minAddress = 0;
        nuint maxAddress = uint.MaxValue;

        // Act
        bool result = header->TryAllocateItem(size, minAddress, maxAddress, out var item);

        // Assert
        result.Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact]
    public unsafe void TryAllocateItem_ShouldNotAllocateItem_WhenOutsideAddressLimits()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;
        header->Initialize(LocatorHeader.Length);

        uint size = 100;
        nuint minAddress = 0;
        nuint maxAddress = 10; // Set maxAddress to a small value to make allocation impossible

        // Act
        Assert.Throws<MemoryBufferAllocationException>(() => header->TryAllocateItem(size, minAddress, maxAddress, out SafeLocatorItem? _));
    }

    [Fact]
    public unsafe void GetNextLocator_ShouldAllocate_WhenNewlyCreated()
    {
        // Arrange
        byte* buffer = stackalloc byte[LocatorHeader.Length];
        var header = (LocatorHeader*)buffer;
        header->Initialize(LocatorHeader.Length);
        header->NumItems = (byte)LocatorHeader.MaxItemCount;

        // Act
        var next = (nuint)header->GetNextLocator();
        var nextCached = (nuint)header->GetNextLocator();

        // Assert
        nextCached.Should().Be(next);
        next.Should().NotBe(0);
    }
}
