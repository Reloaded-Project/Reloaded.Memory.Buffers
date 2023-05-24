using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Reloaded.Memory.Buffers.Native;

#if NET5_0_OR_GREATER
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
#endif
[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
internal partial class UnixMemoryMappedFile : IMemoryMappedFile
{
    private const int PROT_READ = 0x1; // Pages can be read.
    private const int PROT_WRITE = 0x2; // Pages can be written.
    private const int MAP_SHARED = 0x01; // Share changes.

    private const int O_CREAT = 0x40; // Create the file if it doesn't exist.
    private const int O_RDWR = 0x2; // Open for read and write.
    private const int S_IRUSR = 0x100; // User has read permission.
    private const int S_IWUSR = 0x80; // User has write permission.

#if NET7_0_OR_GREATER
    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int shm_open(string name, int oflag, int mode);
#else
    [DllImport("libc", SetLastError = true)]
    public static extern int shm_open(string name, int oflag, int mode);
#endif
#if NET7_0_OR_GREATER
    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int shm_unlink(string name);
#else
    [DllImport("libc", SetLastError = true)]
    public static extern int shm_unlink(string name);
#endif

#if NET7_0_OR_GREATER
    [LibraryImport("libc", SetLastError = true)]
    public static partial int ftruncate(int fd, long length);
#else
    [DllImport("libc", SetLastError = true)]
    public static extern int ftruncate(int fd, long length);
#endif

#if NET7_0_OR_GREATER
    [LibraryImport("libc", SetLastError = true)]
    public static partial nint mmap(nint addr, long length, int prot, int flags, int fd, long offset);
#else
    [DllImport("libc", SetLastError = true)]
    public static extern IntPtr mmap(IntPtr addr, long length, int prot, int flags, int fd, long offset);
#endif

#if NET7_0_OR_GREATER
    [LibraryImport("libc", SetLastError = true)]
    public static partial int munmap(nint addr, nint length);
#else
    [DllImport("libc", SetLastError = true)]
    public static extern int munmap(IntPtr addr, IntPtr length);
#endif

    public int FileDescriptor { get; }
    public bool AlreadyExisted { get; }
    public unsafe byte* Data { get; }
    public int Length { get; }
    public string FileName { get; }

    public unsafe UnixMemoryMappedFile(string name, int length)
    {
        FileName = name;
        Length = length;
        FileDescriptor = shm_open(FileName, O_RDWR, 0);

        if (FileDescriptor == -1)
        {
            // If it doesn't exist, create a new shared memory.
            FileDescriptor = shm_open(FileName, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR);
            ftruncate(FileDescriptor, Length);
        }

        Data = (byte*)mmap(IntPtr.Zero, Length, PROT_READ | PROT_WRITE, MAP_SHARED, FileDescriptor, 0);
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (Data != null)
            munmap((nint)Data, (nint)Length);

        shm_unlink(FileName);
    }
}
