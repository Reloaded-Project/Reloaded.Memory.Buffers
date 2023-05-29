using System.Diagnostics;
using Reloaded.Memory.Buffers.Exceptions;
using Reloaded.Memory.Buffers.Native.Linux;
using Reloaded.Memory.Extensions;
using Reloaded.Memory.Utilities;
using static Reloaded.Memory.Buffers.Utilities.Polyfills;

namespace Reloaded.Memory.Buffers.Utilities;

/// <summary>
///     Parses Linux' /proc/{id}/maps file.
/// </summary>
internal static class LinuxMapParser
{
    /// <summary>
    /// Returns all free regions based on the found regions.
    /// </summary>
    /// <param name="targetProcess">The process to get free regions from.</param>
    public static List<MemoryMapEntry> GetFreeRegions(Process targetProcess)
    {
        using var regs = ParseMemoryMap(targetProcess);
        return GetFreeRegions(regs.Span);
    }

    /// <summary>
    /// Returns all free regions based on the found regions.
    /// </summary>
    /// <param name="regions">The found regions.</param>
    public static List<MemoryMapEntry> GetFreeRegions(Span<MemoryMapEntry> regions)
    {
        nuint lastEndAddress = 0;
        var freeRegions = new List<MemoryMapEntry>(regions.Length + 2); // +2 for start and finish

        for (int x = 0; x < regions.Length; x++)
        {
            MemoryMapEntry entry = regions.DangerousGetReferenceAt(x);
            if (entry.StartAddress > lastEndAddress)
            {
                freeRegions.Add(new MemoryMapEntry
                {
                    StartAddress = lastEndAddress,
                    EndAddress = entry.StartAddress - 1
                });
            }

            lastEndAddress = entry.EndAddress;
        }

        // After the last region, up to the end of memory
        if (lastEndAddress < Cached.GetMaxAddress())
        {
            freeRegions.Add(new MemoryMapEntry
            {
                StartAddress = lastEndAddress,
                EndAddress = Cached.GetMaxAddress()
            });
        }

        return freeRegions;
    }

    /// <summary>
    /// Parses the contents of the /proc/{id}/maps file and returns an array of memory mapping entries.
    /// </summary>
    /// <param name="process">The process to get mapping ranges for.</param>
    /// <returns>An array of memory mapping entries.</returns>
    /// <exception cref="FormatException">One of the lines in the memory map could not be correctly parsed.</exception>
    public static ArrayRentalSlice<MemoryMapEntry> ParseMemoryMap(Process process)
    {
        var mapsPath = $"/proc/{process.Id}/maps";
        return ParseMemoryMap(File.ReadAllLines(mapsPath));
    }

    /// <summary>
    /// Parses the contents of the /proc/self/maps file and returns an array of memory mapping entries.
    /// </summary>
    /// <param name="lines">Contents of file in /proc/self/maps or equivalent.</param>
    /// <returns>An array of memory mapping entries.</returns>
    /// <exception cref="FormatException">One of the lines in the memory map could not be correctly parsed.</exception>
    public static ArrayRentalSlice<MemoryMapEntry> ParseMemoryMap(string[] lines)
    {
        var items = new ArrayRental<MemoryMapEntry>(lines.Length);
        for (int x = 0; x < lines.Length; x++)
            items.Array.DangerousGetReferenceAt(x) = ParseMemoryMapEntry(lines[x]);

        return new ArrayRentalSlice<MemoryMapEntry>(items, lines.Length);
    }

    internal static MemoryMapEntry ParseMemoryMapEntry(string line)
    {
        // Example line: "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file"
        ReadOnlySpan<char> lineSpan = line.AsSpan();
        var dashIndex = lineSpan.IndexOf('-');
        if (dashIndex == -1)
            ThrowHelpers.ThrowLinuxBadMemoryMapEntry();

        var spaceIndex = lineSpan.Slice(dashIndex).IndexOf(' ');
        if (spaceIndex == -1)
            ThrowHelpers.ThrowLinuxBadMemoryMapEntry();

        ReadOnlySpan<char> startAddressSpan = lineSpan.SliceFast(..dashIndex);
        ReadOnlySpan<char> endAddressSpan = lineSpan.SliceFast(dashIndex + 1, spaceIndex - 1);

        return new MemoryMapEntry
        {
            StartAddress = ParseHexAddress(startAddressSpan),
            EndAddress = ParseHexAddress(endAddressSpan)
        };
    }
}
