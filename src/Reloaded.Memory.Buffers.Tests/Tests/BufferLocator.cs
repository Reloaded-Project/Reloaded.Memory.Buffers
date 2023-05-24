using FluentAssertions;
using Reloaded.Memory.Buffers.Structs;
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
