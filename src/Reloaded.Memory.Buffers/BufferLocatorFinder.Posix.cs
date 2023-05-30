using System.Diagnostics;
using Reloaded.Memory.Buffers.Utilities;
using Reloaded.Memory.Native.Unix;
using Posix = Reloaded.Memory.Buffers.Native.Posix;

namespace Reloaded.Memory.Buffers;

/// <summary>
///     Class that locates the buffer locator.
/// </summary>
public static unsafe partial class BufferLocatorFinder
{
    private static void Cleanup()
    {
        // Keep the view around forever for other mods/programs/etc. to use.

        // Note: At runtime this is only ever executed once per library instance, so this should be okay.
        // On Linux we need to execute a runtime check to ensure that after a crash, no MMF was left over.
        // because the OS does not auto dispose them.
        if (Polyfills.IsLinux())
        {
            const string shmDirectoryPath = "/dev/shm";
            const string memoryMappedFilePrefix = "Reloaded.Memory.Buffers.MemoryBuffer, PID ";

            // Read all files in /dev/shm
            var files = Directory.EnumerateFiles(shmDirectoryPath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!fileName.StartsWith(memoryMappedFilePrefix))
                    continue;

                // Extract PID from the file name
                var pidStr = fileName.Substring(memoryMappedFilePrefix.Length);
                if (!int.TryParse(pidStr, out var pid))
                    continue;

                // Check if the process is still running
                if (!IsProcessRunning(pid))
                    Posix.shm_unlink(fileName);
            }
        }
    }

    private static bool IsProcessRunning(int pid)
    {
        try
        {
            Process.GetProcessById(pid);
            return true;
        }
        catch (ArgumentException)
        {
            // Process is not running
            return false;
        }
    }
}
