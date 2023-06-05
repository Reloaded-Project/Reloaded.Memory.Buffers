using System.Diagnostics;
using FluentAssertions;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests;

public class BufferTests
{
    [Fact]
    public void AllocatePrivateMemory_In2GiB()
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

        using var item = Buffers.AllocatePrivateMemory(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
    }

    [Fact]
    public void AllocatePrivateMemory_UpToMaxAddress()
    {
        // Arrange
        var settings = new BufferAllocatorSettings()
        {
            MinAddress = Cached.GetMaxAddress() / 2,
            MaxAddress = Cached.GetMaxAddress(),
            Size = 4096,
            TargetProcess = Process.GetCurrentProcess()
        };

        using var item = Buffers.AllocatePrivateMemory(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
    }

    [Fact]
    public unsafe void GetBuffer_Baseline()
    {
        // Arrange
        var settings = new BufferSearchSettings()
        {
            MinAddress = Cached.GetMaxAddress() / 2,
            MaxAddress = Cached.GetMaxAddress(),
            Size = 4096
        };

        // In case left over uninitialized by previous test.
        LocatorHeaderFinder.Reset();
        using var item = Buffers.GetBuffer(settings);
        ((nuint)item.Item).Should().NotBeNull();
        item.Item->Size.Should().BeGreaterOrEqualTo(settings.Size);
    }
}
