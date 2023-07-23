using System.IO.MemoryMappedFiles;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Reloaded.Memory.Buffers.Native;

#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
internal class WindowsMemoryMappedFile : IMemoryMappedFile
{
    public bool AlreadyExisted { get; }
    public unsafe byte* Data { get; }
    public int Length { get; }
    public string FileName { get; }

    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _view;

    public unsafe WindowsMemoryMappedFile(string name, int length)
    {
        AlreadyExisted = true;
        FileName = name;
        Length = length;

        try
        {
            _memoryMappedFile = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWriteExecute);
        }
        catch (FileNotFoundException)
        {
            _memoryMappedFile = MemoryMappedFile.CreateNew(name, Length, MemoryMappedFileAccess.ReadWriteExecute);
            AlreadyExisted = false;
        }

        _view = _memoryMappedFile.CreateViewAccessor(0, Length, MemoryMappedFileAccess.ReadWriteExecute);
        Data = (byte*)_view.SafeMemoryMappedViewHandle.DangerousGetHandle();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _view.Dispose();

        if (!AlreadyExisted)
            _memoryMappedFile.Dispose();
    }
}
