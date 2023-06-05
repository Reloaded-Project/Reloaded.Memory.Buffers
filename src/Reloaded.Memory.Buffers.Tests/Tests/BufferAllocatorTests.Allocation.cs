using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Structs.Internal;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Native.Unix;
using Reloaded.Memory.Native.Windows;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests;

[SuppressMessage("ReSharper", "RedundantCast")]
public class BufferAllocatorTestsAllocation
{
    [Fact]
    public void CanAllocateIn2GiB()
    {
        // Not supported on OSX.
        if (Polyfills.IsMacOS())
            return;

        // Arrange
        var settings = new BufferAllocatorSettings()
        {
            MinAddress = 0,
            MaxAddress = int.MaxValue,
            Size = 4096,
            TargetProcess = Process.GetCurrentProcess()
        };

        var item = BufferAllocator.Allocate(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
        Free(item, settings);
    }

    [Fact]
    public void CanAllocate_UpToMaxAddress()
    {
        // Arrange
        var settings = new BufferAllocatorSettings()
        {
            MinAddress = Cached.GetMaxAddress() / 2,
            MaxAddress = Cached.GetMaxAddress(),
            Size = 4096,
            TargetProcess = Process.GetCurrentProcess()
        };

        var item = BufferAllocator.Allocate(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
        Free(item, settings);
    }

    /// <summary>
    ///     For testing use only.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="settings"></param>
    internal static void Free(LocatorItem item, BufferAllocatorSettings settings)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        if (Polyfills.IsWindows())
        {
            if (settings.TargetProcess.Id == Polyfills.GetProcessId())
                Kernel32.VirtualFree(item.BaseAddress, (nuint)item.Size, Kernel32.MEM_ALLOCATION_TYPE.MEM_FREE);
            else
                Kernel32.VirtualFreeEx((nint)settings.TargetProcess.Id, item.BaseAddress, (nuint)item.Size, Kernel32.MEM_ALLOCATION_TYPE.MEM_FREE);
        }
        else if (Polyfills.IsLinux() || Polyfills.IsMacOS())
        {
            Posix.munmap(item.BaseAddress, (nuint)item.Size);
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
