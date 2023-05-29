using System;
using FluentAssertions;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests;

[Collection("NonParallel")]
public unsafe class BufferLocatorTests
{
    // Reset the state between tests.
    public BufferLocatorTests() => BufferLocatorFinder.Reset();

    [Fact]
    public void Find_ShouldReturnAddress_WhenPreviouslyExists()
    {
        // Arrange
        BufferLocatorFinder.OpenOrCreateMemoryMappedFile();

        // Act
        LocatorHeader* address = BufferLocatorFinder.Find(out BufferLocatorFinder.FindReason reason);

        // Assert
        ((nuint)address).Should().NotBeNull();
        reason.Should().Be(BufferLocatorFinder.FindReason.PreviouslyExisted);
    }

    [Fact]
    public void Find_ShouldReturnAddress_WhenCreated()
    {
        // Act
        LocatorHeader* address = BufferLocatorFinder.Find(out BufferLocatorFinder.FindReason reason);

        // Assert
        ((nuint)address).Should().NotBeNull();
        reason.Should().Be(BufferLocatorFinder.FindReason.Created);
    }

    [Fact]
    public void Find_ShouldInitializeCorrectly_WhenCreated()
    {
        // Act
        LocatorHeader* address = BufferLocatorFinder.Find(out BufferLocatorFinder.FindReason _);

        // Assert
        var expected = Math.Round((Cached.GetAllocationGranularity() - sizeof(LocatorHeader)) / (float) LocatorHeader.LengthOfPreallocatedChunks);
        address->NumItems.Should().Be((byte)expected);

        for (int x = 0; x < address->NumItems; x++)
        {
            var item = address->GetItem(x);
            item->Position.Should().Be(0);
            item->BaseAddress.Should().NotBeNull();
            item->Size.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Find_ShouldReturnCachedAddress_WhenCalledTwice()
    {
        // Act
        LocatorHeader* firstAddress = BufferLocatorFinder.Find(out BufferLocatorFinder.FindReason firstReason);
        LocatorHeader* secondAddress = BufferLocatorFinder.Find(out BufferLocatorFinder.FindReason secondReason);

        // Assert
        ((nuint)firstAddress).Should().NotBeNull();
        firstReason.Should().Be(BufferLocatorFinder.FindReason.Created);

        ((nuint)secondAddress).Should().NotBeNull();
        secondReason.Should().Be(BufferLocatorFinder.FindReason.Cached);

        ((nuint)firstAddress).Should().Be((nuint)secondAddress);
    }
}
