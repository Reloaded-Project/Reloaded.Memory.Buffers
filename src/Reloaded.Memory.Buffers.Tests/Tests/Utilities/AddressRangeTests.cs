using FluentAssertions;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests.Utilities;

public class AddressRangeTests
{
    [Fact]
    public void Contains_ShouldBeTrue_WhenOtherRangeIsInside()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(120, 180);

        range.Contains(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldBeFalse_WhenOtherRangeIsNotInside()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(80, 220);

        range.Contains(otherRange).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenOtherRangeOverlaps()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(150, 220);

        range.Overlaps(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ShouldBeFalse_WhenOtherRangeDoesNotOverlap()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(300, 400);

        range.Overlaps(otherRange).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenRangesAreSame()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(100, 200);

        range.Overlaps(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenOneRangeIsFullyInsideOther()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(120, 180);

        range.Overlaps(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenRangesAreAdjacent()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(200, 300);

        range.Overlaps(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenOtherRangeStartsInsideRange()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(150, 250);

        range.Overlaps(otherRange).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_ShouldBeTrue_WhenOtherRangeEndsInsideRange()
    {
        var range = new AddressRange(100, 200);
        var otherRange = new AddressRange(50, 150);

        range.Overlaps(otherRange).Should().BeTrue();
    }
}
