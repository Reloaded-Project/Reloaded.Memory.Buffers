using System.Diagnostics.CodeAnalysis;
using Reloaded.Memory.Buffers.Utilities;
using static Reloaded.Memory.Buffers.Native.Posix;
using static Reloaded.Memory.Native.Unix.Posix;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Reloaded.Memory.Buffers.Native;

#if NET5_0_OR_GREATER
[SupportedOSPlatform("linux")]
#endif
[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
internal partial class LinuxMemoryMappedFile : IMemoryMappedFile
{
    private const int PROT_READ = 0x1; // Pages can be read.
    private const int PROT_WRITE = 0x2; // Pages can be written.
    private const int PROT_EXEC = 0x4; // Pages can be written.
    private const int MAP_SHARED = 0x01; // Share changes.

    private const int O_CREAT_OSX = 0x200; // Create the file if it doesn't exist.
    private const int O_CREAT = 0x40; // Create the file if it doesn't exist.
    private const int O_RDWR = 0x2;    // Open for read and write.
    private const int S_IRWXU = 0x1C0; // User has read, write, execute permission.

    public int FileDescriptor { get; }
    public bool AlreadyExisted { get; } = true;
    public unsafe byte* Data { get; private set; }
    public int Length { get; }
    public string FileName { get; }

    public unsafe LinuxMemoryMappedFile(string name, int length)
    {
        FileName = name;
        Length = length;
        FileDescriptor = shm_open(FileName, O_RDWR, 0);

        if (FileDescriptor == -1)
        {
            // If it doesn't exist, create a new shared memory.
            var creat = Polyfills.IsMacOS() ? O_CREAT_OSX : O_CREAT;
            FileDescriptor = shm_open(FileName, creat | O_RDWR, S_IRWXU);
            ftruncate(FileDescriptor, Length);
            AlreadyExisted = false;
        }

        Data = (byte*)mmap(0, (nuint)Length, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_SHARED, FileDescriptor, 0);
        AppDomain.CurrentDomain.ProcessExit += CleanupOnExit;
    }

    ~LinuxMemoryMappedFile() => Dispose();

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (Data != null)
        {
            munmap((nuint)Data, (nuint)Length);
            if (!AlreadyExisted)
                shm_unlink(FileName);
        }

        Data = null!;
    }

    private void CleanupOnExit(object? sender, EventArgs e)
    {
        Dispose();
        AppDomain.CurrentDomain.ProcessExit -= CleanupOnExit;
    }
}

