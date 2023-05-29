using FluentAssertions;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;
using static Reloaded.Memory.Buffers.BufferAllocator;

namespace Reloaded.Memory.Buffers.Tests.Tests;

public class BufferAllocatorTestsCommon
{
    private readonly int _allocationGranularity = 65536; // Assuming 64KB Allocation Granularity

    [Fact]
    public void PageDoesNotOverlapWithMinMax()
    {
        var minPtr = (nuint)100000;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)50000;
        var bufSize = (nuint)30000;

        // No overlap between min-max range and page
        var pageStart = maxPtr + 1;
        var pageEnd = pageStart + pageSize;

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4]).Length;
        Assert.Equal(0, result);
    }

    [Fact]
    public void BufferSizeGreaterThanPage()
    {
        var minPtr = (nuint)100000;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)30000;
        var bufSize = (nuint)50000;  // Greater than pageSize

        // Page is within min-max range
        var pageStart = minPtr;
        var pageEnd = pageStart + pageSize;

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4]).Length;
        Assert.Equal(0, result);
    }

    [Fact]
    public void RoundUpFromPtrMin()
    {
        var minPtr = (nuint)100000;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)200000;
        var bufSize = (nuint)30000;

        // Page is bigger than min-max range
        var pageStart = minPtr - 50000;
        var pageEnd = pageStart + pageSize;

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4])[0];
        Assert.True(result > 0);
    }

    [Fact]
    public void RoundUpFromPageMin()
    {
        var minPtr = (nuint)1;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)100000;
        var bufSize = (nuint)30000;

        // Page start is not aligned with allocation granularity
        var pageStart = minPtr + 5000; // Not multiple of 65536
        var pageEnd = pageStart + pageSize;

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4])[0];
        result.Should().Be(Mathematics.RoundUp(pageStart, _allocationGranularity));
    }

    [Fact]
    public void RoundDownFromPtrMax()
    {
        var minPtr = (nuint)10000;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)1000000;
        var bufSize = (nuint)30000;

        // Max pointer is not aligned with allocation granularity
        maxPtr -= 5000; // Not multiple of 65536

        // Page start is aligned with allocation granularity
        var pageStart = (nuint)80000;
        var pageEnd = pageStart + pageSize;

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4])[0];
        result.Should().Be(Mathematics.RoundDown(maxPtr - bufSize, _allocationGranularity));
    }

    [Fact]
    public void RoundDownFromPageMax()
    {
        var minPtr = (nuint)1;
        var maxPtr = (nuint)200000;
        var pageSize = (nuint)120000;
        var bufSize = (nuint)30000;

        // Page end is not aligned with allocation granularity
        var pageStart = minPtr;
        var pageEnd = pageStart + pageSize - 5000; // Not multiple of 65536

        var result = GetPossibleBufferAddresses(minPtr, maxPtr, pageStart, pageEnd, bufSize, _allocationGranularity, new nuint[4])[0];
        result.Should().Be(Mathematics.RoundDown(pageEnd - bufSize, _allocationGranularity));
    }
}
