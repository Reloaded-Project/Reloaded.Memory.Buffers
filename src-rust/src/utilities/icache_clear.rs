/// Clears the instruction cache for the specified range.
///
/// # Arguments
///
/// * `start` - The start address of the range to clear.
/// * `end` - The end address of the range to clear.
///
/// # Remarks
///
/// This function is provided by LLVM. It might not work in non-LLVM backends.
#[cfg(not(any(target_arch = "x86", target_arch = "x86_64")))]
#[cfg(not(target_os = "windows"))]
#[inline(always)]
pub fn clear_instruction_cache(start: *const u8, end: *const u8) {
    clf::cache_line_flush_with_ptr(start, end);
}

/// Clears the instruction cache for the specified range.
///
/// # Arguments
///
/// * `start` - The start address of the range to clear.
/// * `end` - The end address of the range to clear.
#[cfg(not(any(target_arch = "x86", target_arch = "x86_64")))]
#[cfg(target_os = "windows")] // MSVC fix
#[inline(always)]
pub fn clear_instruction_cache(start: *const u8, end: *const u8) {
    use windows_sys::Win32::System::{
        Diagnostics::Debug::FlushInstructionCache, Threading::GetCurrentProcess,
    };

    unsafe {
        FlushInstructionCache(
            GetCurrentProcess(),
            start as *const core::ffi::c_void,
            end as usize - start as usize,
        );
    }
}

#[cfg(any(target_arch = "x86", target_arch = "x86_64"))]
#[inline(always)]
pub fn clear_instruction_cache(_start: *const u8, _end: *const u8) {
    // x86 & x86_64 have unified data and instruction cache, thus flushing is not needed.
    // Therefore it is a no-op
}
