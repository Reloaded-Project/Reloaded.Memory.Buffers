extern "C" {
    /// This function is provided by LLVM to clear the instruction cache for the specified range.
    fn __clear_cache(start: *mut core::ffi::c_void, end: *mut core::ffi::c_void);
}

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
pub fn clear_instruction_cache(start: *mut u8, end: *mut u8) {
    unsafe {
        __clear_cache(
            start as *mut core::ffi::c_void,
            end as *mut core::ffi::c_void,
        )
    }
}
