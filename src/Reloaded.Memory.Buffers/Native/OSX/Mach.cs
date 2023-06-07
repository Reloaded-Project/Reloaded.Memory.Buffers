using System.Runtime.InteropServices;

namespace Reloaded.Memory.Buffers.Native.OSX;

// ReSharper disable once PartialTypeWithSinglePart
internal partial class Mach
{
#if NET7_0_OR_GREATER
    [LibraryImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static partial int mach_vm_region(nuint task, ref nuint address, ref nuint size, int flavor, out vm_region_basic_info_64 basicInfo64, ref uint infoCount, out int objectName);
#else
    [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static extern int mach_vm_region(nuint task, ref nuint address, ref nuint size, int flavor, out vm_region_basic_info_64 basicInfo64, ref uint infoCount, out int objectName);
#endif

#if NET7_0_OR_GREATER
    [LibraryImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static partial int mach_vm_allocate(nuint task, nuint address, nuint size, int anywhere);
#else
    [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static extern int mach_vm_allocate(nuint task, nuint address, nuint size, int anywhere);
#endif

#if NET7_0_OR_GREATER
    [LibraryImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static partial nuint mach_task_self();
#else
    [DllImport("/usr/lib/system/libsystem_kernel.dylib")]
    public static extern nuint mach_task_self();
#endif

    [StructLayout(LayoutKind.Sequential)]
    public struct vm_region_basic_info_64
    {
        public int protection;
        public int max_protection;
        public uint inheritance;
        public int shared;
        public int reserved;
        public ulong offset;
        public int behavior;
        public ushort user_wired_count;
    }

    public const int VM_REGION_BASIC_INFO_64 = 9;
    public const int VM_REGION_BASIC_INFO_COUNT = 64; // sizeof(vm_region_basic_info_64)
}
