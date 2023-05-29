using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using JetBrains.Annotations;
// ReSharper disable FieldCanBeMadeReadOnly.Global

// ReSharper disable InconsistentNaming

namespace Reloaded.Memory.Buffers.Native.Windows;

/// <summary>
///     Contains all Kernel32 API methods used by this library.
/// </summary>
[PublicAPI]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
// ReSharper disable once PartialTypeWithSinglePart
public static partial class Kernel32
{
    /// <summary>Determines whether the specified process is running under WOW64.</summary>
    /// <param name="hProcess">
    /// <para>
    /// A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right. For more information,
    /// see Process Security and Access Rights.
    /// </para>
    /// <para><c>Windows Server 2003 and Windows XP:</c> The handle must have the PROCESS_QUERY_INFORMATION access right.</para>
    /// </param>
    /// <param name="Wow64Process">
    /// A pointer to a value that is set to TRUE if the process is running under WOW64. If the process is running under 32-bit Windows, the value is set to
    /// FALSE. If the process is a 64-bit application running under 64-bit Windows, the value is also set to FALSE.
    /// </param>
    /// <returns>
    /// <para>If the function succeeds, the return value is a nonzero value.</para>
    /// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
    /// </returns>
    [SuppressUnmanagedCodeSecurity]
#if NET5_0_OR_GREATER
    [SuppressGCTransition]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWow64Process(nint hProcess,
        [MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process(IntPtr hProcess,
        [MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);
#endif

    /// <summary>
    /// <para>Retrieves information about the current system.</para>
    /// <para>To retrieve accurate information for an application running on WOW64, call the <c>GetNativeSystemInfo</c> function.</para>
    /// </summary>
    /// <param name="lpSystemInfo">A pointer to a <c>SYSTEM_INFO</c> structure that receives the information.</param>
    /// <returns>This function does not return a value.</returns>
    [SuppressUnmanagedCodeSecurity]
#if NET5_0_OR_GREATER
    [SuppressGCTransition]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport("kernel32.dll")]
    public static partial void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#else
    [DllImport("kernel32.dll")]
    public static extern void GetSystemInfo(out Kernel32.SYSTEM_INFO lpSystemInfo);
#endif

    /// <summary>
    /// Retrieves information about a range of pages within the virtual address space of a specified process.
    /// </summary>
    /// <param name="hProcess">A handle to the process for which information is needed.</param>
    /// <param name="lpAddress">A pointer to the base address of the region of pages to be queried. </param>
    /// <param name="lpBuffer">A pointer to a buffer that receives the information. The buffer is a MEMORY_BASIC_INFORMATION structure.</param>
    /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
    /// <returns>If the function succeeds, the return value is the actual number of bytes returned. If the function fails, the return value is zero.</returns>
    [SuppressUnmanagedCodeSecurity]
#if NET5_0_OR_GREATER
    [SuppressGCTransition]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static unsafe partial nuint VirtualQueryEx(nint hProcess, nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, nuint dwLength);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    public static unsafe extern nuint VirtualQueryEx(IntPtr hProcess, nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, nuint dwLength);
#endif

    /// <summary>
    /// Retrieves information about a range of pages in the virtual address space of the calling process.
    /// </summary>
    /// <param name="lpAddress">A pointer to the base address of the region of pages to be queried.</param>
    /// <param name="lpBuffer">A pointer to a buffer that receives the information. The buffer is a MEMORY_BASIC_INFORMATION structure.</param>
    /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
    /// <returns>If the function succeeds, the return value is the actual number of bytes returned. If the function fails, the return value is zero.</returns>
    [SuppressUnmanagedCodeSecurity]
#if NET5_0_OR_GREATER
    [SuppressGCTransition]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static unsafe partial nuint VirtualQuery(nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, nuint dwLength);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    public static unsafe extern nuint VirtualQuery(nuint lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, nuint dwLength);
#endif

    /// <summary>
    /// Contains information about the current computer system. This includes the architecture and type of the processor, the number of
    /// processors in the system, the page size, and other such information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct SYSTEM_INFO
    {
        /// <summary>
        /// <para>The processor architecture of the installed operating system. This member can be one of the following values.</para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_AMD649</term>
        /// <term>x64 (AMD or Intel)</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_ARM5</term>
        /// <term>ARM</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_ARM6412</term>
        /// <term>ARM64</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_IA646</term>
        /// <term>Intel Itanium-based</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_INTEL0</term>
        /// <term>x86</term>
        /// </item>
        /// <item>
        /// <term>PROCESSOR_ARCHITECTURE_UNKNOWN0xffff</term>
        /// <term>Unknown architecture.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public ProcessorArchitecture wProcessorArchitecture;

        /// <summary>This member is reserved for future use.</summary>
        public ushort wReserved;

        /// <summary>
        /// The page size and the granularity of page protection and commitment. This is the page size used by the <c>VirtualAlloc</c> function.
        /// </summary>
        public uint dwPageSize;

        /// <summary>A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).</summary>
        public nuint lpMinimumApplicationAddress;

        /// <summary>A pointer to the highest memory address accessible to applications and DLLs.</summary>
        public nuint lpMaximumApplicationAddress;

        /// <summary>
        /// A mask representing the set of processors configured into the system. Bit 0 is processor 0; bit 31 is processor 31.
        /// </summary>
        public nuint dwActiveProcessorMask;

        /// <summary>
        /// The number of logical processors in the current group. To retrieve this value, use the <c>GetLogicalProcessorInformation</c> function.
        /// </summary>
        public uint dwNumberOfProcessors;

        /// <summary>
        /// An obsolete member that is retained for compatibility. Use the <c>wProcessorArchitecture</c>, <c>wProcessorLevel</c>, and
        /// <c>wProcessorRevision</c> members to determine the type of processor.
        /// </summary>
        public uint dwProcessorType;

        /// <summary>
        /// The granularity for the starting address at which virtual memory can be allocated. For more information, see <c>VirtualAlloc</c>.
        /// </summary>
        public uint dwAllocationGranularity;

        /// <summary>
        /// <para>
        /// The architecture-dependent processor level. It should be used only for display purposes. To determine the feature set of a
        /// processor, use the <c>IsProcessorFeaturePresent</c> function.
        /// </para>
        /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_INTEL, <c>wProcessorLevel</c> is defined by the CPU vendor.</para>
        /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_IA64, <c>wProcessorLevel</c> is set to 1.</para>
        /// </summary>
        public ushort wProcessorLevel;

        /// <summary>
        /// <para>
        /// The architecture-dependent processor revision. The following table shows how the revision value is assembled for each type of
        /// processor architecture.
        /// </para>
        /// <para>
        /// <list type="table">
        /// <listheader>
        /// <term>Processor</term>
        /// <term>Value</term>
        /// </listheader>
        /// <item>
        /// <term>Intel Pentium, Cyrix, or NextGen 586</term>
        /// <term>
        /// The high byte is the model and the low byte is the stepping. For example, if the value is xxyy, the model number and stepping
        /// can be displayed as
        /// follows: Model xx, Stepping yy
        /// </term>
        /// </item>
        /// <item>
        /// <term>Intel 80386 or 80486</term>
        /// <term>
        /// A value of the form xxyz. If xx is equal to 0xFF, y - 0xA is the model number, and z is the stepping identifier.If xx is not
        /// equal to 0xFF, xx + 'A' is the stepping letter and yz is the minor stepping.
        /// </term>
        /// </item>
        /// <item>
        /// <term>ARM</term>
        /// <term>Reserved.</term>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public ushort wProcessorRevision;
    }

    /// <summary>
    /// <para>
    /// Contains information about a range of pages in the virtual address space of a process. The VirtualQuery and VirtualQueryEx
    /// functions use this structure.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// To enable a debugger to debug a target that is running on a different architecture (32-bit versus 64-bit), use one of the
    /// explicit forms of this structure.
    /// </para>
    /// </remarks>
    public struct MEMORY_BASIC_INFORMATION
    {
        /// <summary>
        /// <para>A pointer to the base address of the region of pages.</para>
        /// </summary>
        public nuint BaseAddress;

        /// <summary>
        /// <para>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed to by the
        /// <c>BaseAddress</c> member is contained within this allocation range.
        /// </para>
        /// </summary>
        public nuint AllocationBase;

        /// <summary>
        /// <para>
        /// The memory protection option when the region was initially allocated. This member can be one of the memory protection
        /// constants or 0 if the caller does not have access.
        /// </para>
        /// </summary>
        public Reloaded.Memory.Native.Windows.Kernel32.MEM_PROTECTION AllocationProtect;

        /// <summary>
        /// <para>The size of the region beginning at the base address in which all pages have identical attributes, in bytes.</para>
        /// </summary>
        public nuint RegionSize;

        /// <summary>
        /// <para>The state of the pages in the region. This member can be one of the following values.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>State</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>MEM_COMMIT 0x1000</term>
        /// <term>
        /// Indicates committed pages for which physical storage has been allocated, either in memory or in the paging file on disk.
        /// </term>
        /// </item>
        /// <item>
        /// <term>MEM_FREE 0x10000</term>
        /// <term>
        /// Indicates free pages not accessible to the calling process and available to be allocated. For free pages, the information in
        /// the AllocationBase, AllocationProtect, Protect, and Type members is undefined.
        /// </term>
        /// </item>
        /// <item>
        /// <term>MEM_RESERVE 0x2000</term>
        /// <term>
        /// Indicates reserved pages where a range of the process's virtual address space is reserved without any physical storage being
        /// allocated. For reserved pages, the information in the Protect member is undefined.
        /// </term>
        /// </item>
        /// </list>
        /// </summary>
        public MEM_STATE State;

        /// <summary>
        /// <para>
        /// The access protection of the pages in the region. This member is one of the values listed for the <c>AllocationProtect</c> member.
        /// </para>
        /// </summary>
        public uint Protect;

        /// <summary>
        /// <para>The type of pages in the region. The following types are defined.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Type</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>MEM_IMAGE 0x1000000</term>
        /// <term>Indicates that the memory pages within the region are mapped into the view of an image section.</term>
        /// </item>
        /// <item>
        /// <term>MEM_MAPPED 0x40000</term>
        /// <term>Indicates that the memory pages within the region are mapped into the view of a section.</term>
        /// </item>
        /// <item>
        /// <term>MEM_PRIVATE 0x20000</term>
        /// <term>Indicates that the memory pages within the region are private (that is, not shared by other processes).</term>
        /// </item>
        /// </list>
        /// </summary>
        public MEM_TYPE Type;
    }

    /// <summary>
    /// Represents the state of the memory pages within the region.
    /// </summary>
    public enum MEM_STATE : uint
    {
        /// <summary>
        /// Indicates committed pages for which physical storage has been allocated, either in memory or in the paging file on disk.
        /// </summary>
        COMMIT = 0x1000,

        /// <summary>
        /// Indicates free pages not accessible to the calling process and available to be allocated.
        /// </summary>
        FREE = 0x10000,

        /// <summary>
        /// Indicates reserved pages where a range of the process's virtual address space is reserved without any physical storage being allocated.
        /// </summary>
        RESERVE = 0x2000
    }

    /// <summary>
    /// Represents the type of the memory pages within the region.
    /// </summary>
    public enum MEM_TYPE : uint
    {
        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of an image section.
        /// </summary>
        IMAGE = 0x1000000,

        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of a section.
        /// </summary>
        MAPPED = 0x40000,

        /// <summary>
        /// Indicates that the memory pages within the region are private (that is, not shared by other processes).
        /// </summary>
        PRIVATE = 0x20000
    }

    /// <summary>Processor architecture</summary>
    public enum ProcessorArchitecture : ushort
    {
        /// <summary>x86</summary>
        PROCESSOR_ARCHITECTURE_INTEL = 0,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_MIPS = 1,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ALPHA = 2,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_PPC = 3,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_SHX = 4,

        /// <summary>ARM</summary>
        PROCESSOR_ARCHITECTURE_ARM = 5,

        /// <summary>Intel Itanium-based</summary>
        PROCESSOR_ARCHITECTURE_IA64 = 6,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ALPHA64 = 7,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_MSIL = 8,

        /// <summary>x64 (AMD or Intel)</summary>
        PROCESSOR_ARCHITECTURE_AMD64 = 9,

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10, // 0x000A

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_NEUTRAL = 11, // 0x000B

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ARM64 = 12, // 0x000C

        /// <summary>Unspecified</summary>
        PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13, // 0x000D

        /// <summary>Unknown architecture.</summary>
        PROCESSOR_ARCHITECTURE_UNKNOWN = 65535, // 0xFFFF
    }
}
