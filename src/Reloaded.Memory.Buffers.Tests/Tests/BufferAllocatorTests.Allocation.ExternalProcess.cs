using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using Reloaded.Memory.Buffers.Internal;
using Reloaded.Memory.Buffers.Structs.Params;
using Reloaded.Memory.Buffers.Utilities;
using Xunit;

namespace Reloaded.Memory.Buffers.Tests.Tests;

[SuppressMessage("ReSharper", "RedundantCast")]
[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
public class BufferAllocatorTestsAllocationExternalProcess
{
    [Fact]
    public void CanAllocateIn2GiB()
    {
        // Windows Only.
        if (!Polyfills.IsWindows() || IntPtr.Size < 4)
            return;

        // Arrange
        using var target = new TemporaryProcess();
        var settings = new BufferAllocatorSettings()
        {
            MinAddress = 0,
            MaxAddress = int.MaxValue,
            Size = 4096,
            TargetProcess = target.Process
        };

        var item = BufferAllocator.Allocate(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
    }

    [Fact]
    public void CanAllocate_UpToMaxAddress()
    {
        // Windows Only.
        if (!Polyfills.IsWindows())
            return;

        // Arrange
        using var target = new TemporaryProcess();
        var settings = new BufferAllocatorSettings()
        {
            MinAddress = Cached.GetMaxAddress() / 2,
            MaxAddress = Cached.GetMaxAddress(),
            Size = 4096,
            TargetProcess = target.Process
        };

        var item = BufferAllocator.Allocate(settings);
        item.BaseAddress.Should().NotBeNull();
        item.Size.Should().BeGreaterOrEqualTo(settings.Size);
    }

    private class TemporaryProcess : IDisposable
    {
        // Create dummy HelloWorld.exe
        public Process Process { get; }

        public TemporaryProcess()
        {
            var filePath = Path.GetFullPath(Assets.GetHelloWorldExePath());

            try
            {
                Process = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath, CreateNoWindow = true, UseShellExecute = false
                })!;
            }
            catch (Win32Exception)
            {
                throw new Exception($"Failed to start process with path: {filePath}");
            }
        }

        // Dispose of HelloWorld.exe
        public void Dispose()
        {
            Process.Kill();
            Process.Dispose();
        }
    }
}
