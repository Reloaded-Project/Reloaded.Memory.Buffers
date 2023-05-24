namespace Reloaded.Memory.Buffers.Native;

internal interface IMemoryMappedFile : IDisposable
{
    bool AlreadyExisted { get; }
    unsafe byte* Data { get; }
    public int Length { get; }
    public string FileName { get; }
}
