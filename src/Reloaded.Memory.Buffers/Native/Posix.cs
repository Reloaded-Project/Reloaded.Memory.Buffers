using System.Runtime.InteropServices;

namespace Reloaded.Memory.Buffers.Native;

internal static partial class Posix
{
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
}