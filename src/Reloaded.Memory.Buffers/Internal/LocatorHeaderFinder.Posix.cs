using System.Diagnostics;
using Reloaded.Memory.Buffers.Native;
using Reloaded.Memory.Buffers.Utilities;
using Posix = Reloaded.Memory.Buffers.Native.Posix;

namespace Reloaded.Memory.Buffers.Internal;

/// <summary>
///     Class that locates the buffer locator.
/// </summary>
internal static partial class LocatorHeaderFinder
{
    private static void Cleanup()
    {
        // Keep the view around forever for other mods/programs/etc. to use.

        // Note: At runtime this is only ever executed once per library instance, so this should be okay.
        // On Linux and OSX we need to execute a runtime check to ensure that after a crash, no MMF was left over.
        // because the OS does not auto dispose them.
#pragma warning disable RCS1075
#pragma warning disable CA1416
        if (Polyfills.IsMacOS() || Polyfills.IsLinux())
        {
            CleanupPosix(UnixMemoryMappedFile.BaseDir, (path) =>
            {
                try { File.Delete(path); }
                catch (Exception) { /* Ignored */ }
            });
        }
#pragma warning restore RCS1075
#pragma warning restore CA1416
    }

    private static void CleanupPosix(string mmfDirectory, Action<string> deleteFile)
    {
        const string memoryMappedFilePrefix = "Reloaded.Memory.Buffers.MemoryBuffer, PID ";
        var files = Directory.EnumerateFiles(mmfDirectory);

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
                deleteFile(fileName);
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
