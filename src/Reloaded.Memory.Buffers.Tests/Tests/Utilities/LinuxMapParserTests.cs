using System;
using FluentAssertions;
using Reloaded.Memory.Buffers.Native.Linux;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests.Utilities;

#pragma warning disable CS8778 // Constant may overflow at runtime
public unsafe class LinuxMapParserTests
{
    [Fact]
    public void GetFreeRegions_WithNoGap()
    {
        var regions = new MemoryMapEntry[]
        {
            new() { StartAddress = 0, EndAddress = 10 },
            new() { StartAddress = 10, EndAddress = 20 },
            new() { StartAddress = 20, EndAddress = Cached.GetMaxAddress() }
        };

        var freeRegions = LinuxMapParser.GetFreeRegions(regions);
        freeRegions.Should().BeEmpty();
    }

    [Fact]
    public void GetFreeRegions_SingleGap()
    {
        var regions = new MemoryMapEntry[]
        {
            new() { StartAddress = 0, EndAddress = 10 },
            new() { StartAddress = 10, EndAddress = 20 },
            new() { StartAddress = 30, EndAddress = Cached.GetMaxAddress() }
        };

        var freeRegions = LinuxMapParser.GetFreeRegions(regions);
        freeRegions.Should().HaveCount(1);
        freeRegions[0].StartAddress.Should().Be((nuint)20);
        freeRegions[0].EndAddress.Should().Be((nuint)29);
    }

    [Fact]
    public void GetFreeRegions_Multiple_Gaps()
    {
        var regions = new MemoryMapEntry[]
        {
            new() { StartAddress = 0, EndAddress = 10 },
            new() { StartAddress = 20, EndAddress = 30 },
            new() { StartAddress = 40, EndAddress = Cached.GetMaxAddress() }
        };

        var freeRegions = LinuxMapParser.GetFreeRegions(regions);

        freeRegions.Should().HaveCount(2);
        freeRegions[0].StartAddress.Should().Be((nuint)10);
        freeRegions[0].EndAddress.Should().Be((nuint)19);
        freeRegions[1].StartAddress.Should().Be((nuint)30);
        freeRegions[1].EndAddress.Should().Be((nuint)39);
    }

    [Fact]
    public void ParseMemoryMapEntry_ValidLine_ReturnsCorrectEntries()
    {
        if (sizeof(nuint) < 8) return;

        var line = "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file";
        var result = LinuxMapParser.ParseMemoryMapEntry(line);

        // Replace the numbers with whatever you expect the result to be
        result.StartAddress.Should().Be((nuint)0x7f9c89991000);
        result.EndAddress.Should().Be((nuint)0x7f9c89993000);
    }

    [Fact]
    public void ParseMemoryMapEntry_ValidLine_ReturnsCorrectEntry()
    {
        if (sizeof(nuint) < 8) return;

        var line = "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file";
        var result = LinuxMapParser.ParseMemoryMapEntry(line);

        // Replace the numbers with whatever you expect the result to be
        result.StartAddress.Should().Be((nuint)0x7f9c89991000);
        result.EndAddress.Should().Be((nuint)0x7f9c89993000);
    }

    [Fact]
    public void ParseMemoryMapEntry_InvalidLine_ThrowsException()
    {
        if (sizeof(nuint) < 8) return;

        var line = "Invalid line";
        Action act = () => LinuxMapParser.ParseMemoryMapEntry(line);
        act.Should().Throw<FormatException>().WithMessage("Invalid Memory Map Entry");
    }

    [Fact]
    public void ParseMemoryMap_ValidLines_ReturnsCorrectEntries()
    {
        if (sizeof(nuint) < 8) return;

        var lines = new[] {
            "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file",
            "7f9c89994000-7f9c89995000 r--p 00000000 08:01 3932178                    /path/to/file"
        };
        var result = LinuxMapParser.ParseMemoryMap(lines);

        result.Length.Should().Be(2);
        result.Span[0].StartAddress.Should().Be((nuint)0x7f9c89991000);
        result.Span[0].EndAddress.Should().Be((nuint)0x7f9c89993000);
        result.Span[1].StartAddress.Should().Be((nuint)0x7f9c89994000);
        result.Span[1].EndAddress.Should().Be((nuint)0x7f9c89995000);
    }

    [Fact]
    public void ParseMemoryMapEntry_ValidLine_ReturnsCorrectEntry_32Bit()
    {
        var line = "7f899100-7f899300 r--p 00000000 08:01 3932177                    /path/to/file";
        var result = LinuxMapParser.ParseMemoryMapEntry(line);

        // Replace the numbers with whatever you expect the result to be
        result.StartAddress.Should().Be((nuint)0x7f899100);
        result.EndAddress.Should().Be((nuint)0x7f899300);
    }

    [Fact]
    public void ParseMemoryMapEntry_InvalidLine_ThrowsException_32Bit()
    {
        var line = "Invalid line";
        Action act = () => LinuxMapParser.ParseMemoryMapEntry(line);
        act.Should().Throw<Exception>().WithMessage("Invalid Memory Map Entry");
    }

    [Fact]
    public void ParseMemoryMap_ValidLines_ReturnsCorrectEntries_32Bit()
    {
        var lines = new[] {
            "7f899100-7f899300 r--p 00000000 08:01 3932177                    /path/to/file",
            "7f899400-7f899500 r--p 00000000 08:01 3932178                    /path/to/file"
        };
        var result = LinuxMapParser.ParseMemoryMap(lines);

        result.Length.Should().Be(2);
        result.Span[0].StartAddress.Should().Be((nuint)0x7f899100);
        result.Span[0].EndAddress.Should().Be((nuint)0x7f899300);
        result.Span[1].StartAddress.Should().Be((nuint)0x7f899400);
        result.Span[1].EndAddress.Should().Be((nuint)0x7f899500);
    }
}
