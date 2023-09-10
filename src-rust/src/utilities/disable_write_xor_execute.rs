// An utility to disable write xor execute protection on a memory region.
// This method contains the code to disable W^X on platforms where it's enforced.

#[cfg(target_os = "macos")]
use {
    libc::mach_task_self, mach::vm::mach_vm_protect, mach::vm_prot::VM_PROT_EXECUTE,
    mach::vm_prot::VM_PROT_READ, mach::vm_prot::VM_PROT_WRITE, mach::vm_types::mach_vm_size_t,
};

/// Temporarily disables write XOR execute protection with an OS specialized
/// API call (if available).
///
/// # Parameters
///
/// - `address`: The address of the memory to disable write XOR execute protection for.
/// - `size`: The size of the memory to disable write XOR execute protection for.
///
/// # Returns
///
/// - `usize`: The old memory protection (if needed for call to [`self::restore_write_xor_execute`]).
///
/// # Remarks
///
/// This is not currently used on any platform, but is intended for environments
/// which enforce write XOR execute, such as M1 macs.
///
/// The idea is that you use memory which is read_write_execute (MAP_JIT if mmap),
/// then disable W^X for the current thread. Then we write the code, and re-enable W^X.
#[allow(unused_variables)]
pub(crate) fn disable_write_xor_execute(address: *const u8, size: usize) {
    #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
    unsafe {
        mach_vm_protect(
            mach_task_self(),
            address as u64,
            size as mach_vm_size_t,
            0,
            VM_PROT_READ | VM_PROT_WRITE,
        );
    }
}

/// Restores write XOR execute protection.
///
/// # Parameters
///
/// - `address`: The address of the memory to disable write XOR execute protection for.
/// - `size`: The size of the memory to disable write XOR execute protection for.
/// - `protection`: The protection returned in the result of the call to [`self::disable_write_xor_execute`].
///
/// # Returns
///
/// Success or error.
#[allow(unused_variables)]
pub(crate) fn restore_write_xor_execute(address: *const u8, size: usize) {
    #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
    unsafe {
        mach_vm_protect(
            mach_task_self(),
            address as u64,
            size as mach_vm_size_t,
            0,
            VM_PROT_READ | VM_PROT_EXECUTE,
        );
    }
}
