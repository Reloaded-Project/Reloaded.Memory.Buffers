using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Reloaded.Memory.Buffers.Native;

#if NET5_0_OR_GREATER
[SupportedOSPlatform("macos")]
#endif
[SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
[SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
internal class UnixMemoryMappedFile : IMemoryMappedFile
{
    public static readonly string BaseDir;

    static UnixMemoryMappedFile() => BaseDir = "/tmp/.reloaded/memory.buffers";

    public bool AlreadyExisted { get; }
    public unsafe byte* Data { get; }
    public int Length { get; }
    public string FileName { get; }

    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _view;
    private readonly FileStream _stream;

    public unsafe UnixMemoryMappedFile(string name, int length)
    {
        name = name.TrimStart('/');
        AlreadyExisted = true;
        FileName = name;
        Length = length;

        // Override or create existing file.
        var filePath = Path.Combine(BaseDir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        AlreadyExisted = File.Exists(filePath);

        _stream = new FileStream(
            filePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete,
            length);

        _stream.SetLength(length);

        _memoryMappedFile = MemoryMappedFile.CreateFromFile(
            _stream,
            null,
            length,
            MemoryMappedFileAccess.ReadWriteExecute,
            HandleInheritability.Inheritable,
            true);

        _view = _memoryMappedFile.CreateViewAccessor(0, Length, MemoryMappedFileAccess.ReadWriteExecute);
        Data = (byte*)_view.SafeMemoryMappedViewHandle.DangerousGetHandle();
    }

    ~UnixMemoryMappedFile() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        _memoryMappedFile.Dispose();
        _view.Dispose();
        _stream.Dispose();

        if (!AlreadyExisted)
            File.Delete(_stream.Name);
    }
}
