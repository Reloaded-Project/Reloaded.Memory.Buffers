extern crate alloc;

use crate::{
    buffers::Buffers,
    structs::{
        internal::LocatorItem,
        params::{BufferAllocatorSettings, BufferSearchSettings},
        PrivateAllocation,
    },
    utilities::{
        disable_write_xor_execute::{disable_write_xor_execute, restore_write_xor_execute},
        icache_clear::clear_instruction_cache,
    },
};
use alloc::ffi::CString;
use core::{
    ffi::c_char,
    mem::{self, ManuallyDrop},
    ptr::{self, copy_nonoverlapping, null, null_mut},
};

/// The result of making an allocation.
#[repr(C)]
pub struct AllocationResult {
    /// This is true if the 'ok' field can be consumed.
    /// If this is false, read the error in 'err' field, then free it with 'free_string'.
    pub is_ok: bool,

    /// The details of the successful allocation information.
    pub ok: PrivateAllocation,

    /// An error.
    pub err: *const c_char,
}

/// The result of fetching a buffer.
#[repr(C)]
pub struct GetBufferResult {
    /// This is true if the 'ok' field can be consumed.
    /// If this is false, read the error in 'err' field, then free it with 'free_string'.
    pub is_ok: bool,

    /// The details of the fetched buffer.
    pub ok: *mut LocatorItem,

    /// An error.
    pub err: *const c_char,
}

/// Allocates some memory with user specified settings.
/// The allocated memory is for your use only.
///
/// # Arguments
///
/// * `settings` - Settings with which to allocate the memory.
///
/// # Returns
///
/// Information about the recently made allocation.
///
/// # Remarks
///
/// Allocating inside another process is only supported on Windows.
#[no_mangle]
pub extern "C" fn buffers_allocate_private_memory(
    settings: &mut BufferAllocatorSettings,
) -> AllocationResult {
    match Buffers::allocate_private_memory(settings) {
        Ok(allocation) => {
            let allocation = ManuallyDrop::new(allocation);
            AllocationResult {
                is_ok: true,
                ok: unsafe { ptr::read(&*allocation) },
                err: null(),
            }
        }
        Err(err) => AllocationResult {
            is_ok: false,
            ok: PrivateAllocation::null(),
            err: CString::new(err.to_string().as_str()).unwrap().into_raw(),
        },
    }
}

/// Gets a buffer with user specified requirements and provided alignment.
///
/// # Arguments
///
/// * `settings` - Settings with which to allocate the memory.
/// * `alignment` - Alignment of the buffer. Max 4096, but recommended <= 64.
///
/// # Returns
///
/// Item allowing you to write to the buffer.
/// Make sure you drop it, by using `drop` function.
///
/// # Remarks
///
/// Allocating inside another process is only supported on Windows.
///
/// This function is 'dumb', it will look for buffer with size `settings.size + alignment` and
/// then align it; as there is no current logic which takes alignment into account when searching
/// for a buffer (feel free to PR it though!).
///
/// Thus, this function might miss some buffers which could accomodate the alignment requirement.
/// It is recommended to use this when the alignment requirement is less than '64 bytes'
///
/// # Errors
///
/// Returns an error if the memory cannot be allocated within the needed constraints when there
/// is no existing suitable buffer.
#[no_mangle]
pub extern "C" fn buffers_get_buffer_aligned(
    settings: &BufferSearchSettings,
    alignment: u32,
) -> GetBufferResult {
    match Buffers::get_buffer_aligned(settings, alignment) {
        Ok(locator_item) => {
            let result = GetBufferResult {
                is_ok: true,
                ok: locator_item.item.get(),
                err: null(),
            };
            mem::forget(locator_item);
            result
        }
        Err(err) => GetBufferResult {
            is_ok: false,
            ok: null_mut(),
            err: CString::new(err.to_string().as_str()).unwrap().into_raw(),
        },
    }
}

/// Gets a buffer with user specified requirements.
///
/// # Arguments
///
/// * `settings` - Settings with which to allocate the memory.
///
/// # Returns
///
/// Item allowing you to write to the buffer.
/// Make sure you drop it, by using `drop` function.
///
/// # Remarks
///
/// Allocating inside another process is only supported on Windows.
///
/// # Errors
///
/// Returns an error if the memory cannot be allocated within the needed constraints when there
/// is no existing suitable buffer.
#[no_mangle]
pub extern "C" fn buffers_get_buffer(settings: &BufferSearchSettings) -> GetBufferResult {
    match Buffers::get_buffer(settings) {
        Ok(buffer) => {
            let result = GetBufferResult {
                is_ok: true,
                ok: buffer.item.get(),
                err: null(),
            };
            mem::forget(buffer);
            result
        }
        Err(err) => GetBufferResult {
            is_ok: false,
            ok: null_mut(),
            err: CString::new(err.to_string().as_str()).unwrap().into_raw(),
        },
    }
}

/// Frees an error string returned from the library.
#[no_mangle]
pub extern "C" fn free_string(s: *mut c_char) {
    unsafe {
        if s.is_null() {
            return;
        }
        mem::drop(CString::from_raw(s));
    };
}

/// Frees an error string returned from the library.
#[no_mangle]
pub extern "C" fn free_locator_item(item: *mut LocatorItem) {
    unsafe {
        (*item).unlock();
    }
}

/// Frees a private allocation returned from the library.
#[no_mangle]
pub extern "C" fn free_private_allocation(item: PrivateAllocation) {
    mem::drop(item);
}

/// Frees an allocation result returned from the 'buffers' operation.
#[no_mangle]
pub extern "C" fn free_allocation_result(item: AllocationResult) {
    if item.is_ok {
        free_private_allocation(item.ok);
    } else {
        free_string(item.err as *mut c_char);
    }
}

/// Frees a get buffer result returned from the 'buffers' operation.
#[no_mangle]
pub extern "C" fn free_get_buffer_result(item: GetBufferResult) {
    if item.is_ok {
        free_locator_item(item.ok);
    } else {
        free_string(item.err as *mut c_char);
    }
}

/// Creates settings such that the returned buffer will always be within `proximity` bytes of `target`.
///
/// # Arguments
///
/// * `proximity` - Max proximity (number of bytes) to target.
/// * `target` - Target address.
/// * `size` - Size required in the settings.
///
/// # Returns
///
/// * `BufferSearchSettings` - Settings that would satisfy this search.
#[no_mangle]
pub extern "C" fn buffersearchsettings_from_proximity(
    proximity: usize,
    target: usize,
    size: usize,
) -> BufferSearchSettings {
    BufferSearchSettings::from_proximity(proximity, target, size)
}

/// Creates settings such that the returned buffer will always be within `proximity` bytes of `target`.
///
/// # Arguments
///
/// * `proximity` - Max proximity (number of bytes) to target.
/// * `target` - Target address.
/// * `size` - Size required in the settings.
///
/// # Returns
///
/// * `BufferAllocatorSettings` - Settings that would satisfy this search.
#[no_mangle]
pub extern "C" fn bufferallocatorsettings_from_proximity(
    proximity: usize,
    target: usize,
    size: usize,
) -> BufferAllocatorSettings {
    BufferAllocatorSettings::from_proximity(proximity, target, size)
}

/// Clears the instruction cache for the specified range.
///
/// # Arguments
///
/// * `start` - The start address of the range to clear.
/// * `end` - The end address of the range to clear.
#[no_mangle]
pub extern "C" fn utilities_clear_instruction_cache(start: *mut u8, end: *mut u8) {
    clear_instruction_cache(start, end);
}

/// Call this method in order to safely be able to overwrite existing code that was
/// allocated by the library inside one of its buffers. (e.g. Hooking/detours code.)
///
/// This callback handles various edge cases, (such as flushing caches), and flipping page permissions
/// on relevant platforms.
///
/// # Parameters
///
/// * `source` - Source address to copy bytes from.
/// * `target` - Where these bytes should be copied to. This should be an address inside a buffer.
/// * `size` - The size of the data to copy.
///
/// # Safety
///
/// Only use this with addresses allocated inside a Reloaded.Memory.Buffers buffer.  
/// Usage with any other memory is undefined behaviour.
///
/// # Remarks
///
/// This function can be skipped on some combinations (e.g. Windows/Linux/macOS x86/x64). But
/// should not be skipped on non-x86 architectures.
#[no_mangle]
pub unsafe extern "C" fn overwrite_allocated_code(source: *const u8, target: *mut u8, size: usize) {
    disable_write_xor_execute(target, size);
    copy_nonoverlapping(source, target, size);
    restore_write_xor_execute(target, size);
    clear_instruction_cache(target, source.wrapping_add(size));
}

/// Call this method in order to safely be able to overwrite existing code that was
/// allocated by the library inside one of its buffers. (e.g. Hooking/detours code.)
///
/// This callback handles various edge cases, (such as flushing caches), and flipping page permissions
/// on relevant platforms.
///
/// # Parameters
///
/// * `source` - Source address to copy bytes from.
/// * `target` - Where these bytes should be copied to. This should be an address inside a buffer.
/// * `size` - The size of the data to copy.
/// * `callback` - Your method to overwrite the code present there.
///
/// # Safety
///
/// Only use this with addresses allocated inside a Reloaded.Memory.Buffers buffer.  
/// Usage with any other memory is undefined behaviour.
///
/// # Remarks
///
/// This function can be skipped on some combinations (e.g. Windows/Linux/macOS x86/x64). But
/// should not be skipped on non-x86 architectures.
#[no_mangle]
pub extern "C" fn overwrite_allocated_code_ex(
    source: *const u8,
    target: *mut u8,
    size: usize,
    callback: extern "C" fn(*const u8, *mut u8, usize),
) {
    disable_write_xor_execute(target, size);
    callback(source, target, size);
    restore_write_xor_execute(target, size);
    clear_instruction_cache(target, source.wrapping_add(size));
}

#[cfg(test)]
mod tests {
    use crate::c::buffers_c_buffers::{
        buffers_allocate_private_memory, buffers_get_buffer, buffersearchsettings_from_proximity,
    };
    use crate::c::buffers_c_buffers::{free_allocation_result, free_get_buffer_result};
    use crate::c::buffers_c_locatoritem::{
        locatoritem_append_bytes, locatoritem_bytes_left, locatoritem_min_address,
    };
    use crate::{
        internal::locator_header_finder::LocatorHeaderFinder,
        structs::params::{BufferAllocatorSettings, BufferSearchSettings},
        utilities::cached::get_sys_info,
    };
    use rstest::rstest;
    use std;

    #[cfg(not(target_os = "macos"))]
    #[test]
    fn allocate_private_memory_in_2gib() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = 0;
        settings.max_address = std::i32::MAX as usize;

        let result = buffers_allocate_private_memory(&mut settings);
        assert!(result.is_ok);

        assert!(result.ok.size >= settings.size as usize);
        free_allocation_result(result);
    }

    #[test]
    fn allocate_private_memory_up_to_max_address() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = get_sys_info().max_address / 2;
        settings.max_address = get_sys_info().max_address;

        let result = buffers_allocate_private_memory(&mut settings);
        assert!(result.is_ok);

        assert!(result.ok.size >= settings.size as usize);
        free_allocation_result(result);
    }

    /// Baseline test to ensure that the buffer get logic is ok.
    #[test]
    fn get_buffer_baseline() {
        let settings = BufferSearchSettings {
            min_address: (get_sys_info().max_address / 2),
            max_address: get_sys_info().max_address,
            size: 4096,
        };

        // Automatically dropped.
        let result = buffers_get_buffer(&settings);
        assert!(result.is_ok);

        // Append some data.
        let data = [0x0; 4096];
        unsafe {
            locatoritem_append_bytes(result.ok, &data[0], data.len());
            free_get_buffer_result(result);
        }
    }

    #[rstest]
    #[case(64)]
    #[case(128)]
    #[case(256)]
    fn get_buffer_aligned_test(#[case] alignment: u32) {
        let settings = BufferSearchSettings {
            min_address: (get_sys_info().max_address / 2),
            max_address: get_sys_info().max_address,
            size: 4096,
        };

        // The function should succeed with these settings.
        let result = super::buffers_get_buffer_aligned(&settings, alignment);

        if result.is_ok {
            // Check that the address is aligned as expected.
            assert_eq!(
                crate::c::buffers_c_locatoritem::locatoritem_min_address(result.ok)
                    % alignment as usize,
                0
            );
            free_get_buffer_result(result);
        } else {
            // Handle the error (just print here).
            println!("Error getting buffer: {:?}", result.err);
            unreachable!();
        }
    }

    // This works on MacOS, I just don't know what to use as a consistent address for this test.
    #[cfg(not(target_os = "macos"))]
    #[test]
    fn get_buffer_with_proximity() {
        const SIZE: usize = 4096;
        let base_address = get_sys_info().max_address - (std::i32::MAX as usize);

        unsafe {
            LocatorHeaderFinder::reset();
        }

        let settings =
            buffersearchsettings_from_proximity(std::i32::MAX as usize, base_address, SIZE);

        let result = buffers_get_buffer(&settings);

        assert!(result.is_ok);

        assert!(locatoritem_bytes_left(result.ok) >= SIZE as u32);

        let offset = (locatoritem_min_address(result.ok) as i64 - base_address as i64).abs();
        assert!(offset < (i32::MAX as i64));
        free_get_buffer_result(result);
    }
}
