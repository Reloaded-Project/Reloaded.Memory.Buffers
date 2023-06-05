using System;
using FluentAssertions;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests;

[Collection("NonParallel")]
public unsafe class BufferLocatorTests
{
    // Reset the state between tests.
    public BufferLocatorTests() => LocatorHeaderFinder.Reset();

    [Fact]
    public void Find_ShouldReturnAddress_WhenPreviouslyExists()
    {
        // Arrange
        LocatorHeaderFinder.OpenOrCreateMemoryMappedFile();

        // Act
        LocatorHeader* address = LocatorHeaderFinder.Find(out LocatorHeaderFinder.FindReason reason);

        // Assert
        ((nuint)address).Should().NotBeNull();
        reason.Should().Be(LocatorHeaderFinder.FindReason.PreviouslyExisted);
    }

    [Fact]
    public void Find_ShouldReturnAddress_WhenCreated()
    {
        // Act
        LocatorHeader* address = LocatorHeaderFinder.Find(out LocatorHeaderFinder.FindReason reason);

        // Assert
        ((nuint)address).Should().NotBeNull();
        reason.Should().Be(LocatorHeaderFinder.FindReason.Created);
    }

    [Fact]
    public void Find_ShouldInitializeCorrectly_WhenCreated()
    {
        // Act
        LocatorHeader* address = LocatorHeaderFinder.Find(out LocatorHeaderFinder.FindReason _);

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
        LocatorHeader* firstAddress = LocatorHeaderFinder.Find(out LocatorHeaderFinder.FindReason firstReason);
        LocatorHeader* secondAddress = LocatorHeaderFinder.Find(out LocatorHeaderFinder.FindReason secondReason);

        // Assert
        ((nuint)firstAddress).Should().NotBeNull();
        firstReason.Should().Be(LocatorHeaderFinder.FindReason.Created);

        ((nuint)secondAddress).Should().NotBeNull();
        secondReason.Should().Be(LocatorHeaderFinder.FindReason.Cached);

        ((nuint)firstAddress).Should().Be((nuint)secondAddress);
    }
}
