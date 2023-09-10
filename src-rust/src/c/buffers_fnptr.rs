use std::ffi::c_char;

use super::buffers_c_buffers::{AllocationResult, GetBufferResult};
use crate::structs::{
    internal::LocatorItem,
    params::{BufferAllocatorSettings, BufferSearchSettings},
    PrivateAllocation,
};

#[repr(C)]
pub struct BuffersFunctions {
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
    pub buffers_allocate_private_memory:
        extern "C" fn(settings: &mut BufferAllocatorSettings) -> AllocationResult,

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
    /// Make sure you free it when done, by using `free_get_buffer_result` function.
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
    pub buffers_get_buffer_aligned:
        extern "C" fn(settings: &BufferSearchSettings, alignment: u32) -> GetBufferResult,

    /// Gets a buffer with user specified requirements.
    ///
    /// # Arguments
    ///
    /// * `settings` - Settings with which to allocate the memory.
    ///
    /// # Returns
    ///
    /// Item allowing you to write to the buffer.
    /// Make sure you free it when done, by using `free_get_buffer_result` function.
    ///
    /// # Remarks
    ///
    /// Allocating inside another process is only supported on Windows.
    ///
    /// # Errors
    ///
    /// Returns an error if the memory cannot be allocated within the needed constraints when there
    /// is no existing suitable buffer.
    pub buffers_get_buffer: extern "C" fn(settings: &BufferSearchSettings) -> GetBufferResult,

    /// Frees an error string returned from the library.
    pub free_string: extern "C" fn(string: *mut c_char),

    /// Frees a private allocation returned from the library.
    pub free_private_allocation: extern "C" fn(item: PrivateAllocation),

    /// Frees an error string returned from the library.
    pub free_locator_item: unsafe extern "C" fn(item: *mut LocatorItem),

    /// Frees an allocation result returned from the 'buffers' operation.
    pub free_allocation_result: extern "C" fn(AllocationResult),

    /// Frees a get buffer result returned from the 'buffers' operation.
    pub free_get_buffer_result: unsafe extern "C" fn(GetBufferResult),

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
    pub buffersearchsettings_from_proximity:
        extern "C" fn(usize, usize, usize) -> BufferSearchSettings,

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
    pub bufferallocatorsettings_from_proximity:
        extern "C" fn(usize, usize, usize) -> BufferAllocatorSettings,

    /// Returns the amount of bytes left in the buffer.
    pub locatoritem_bytes_left: unsafe extern "C" fn(item: *const LocatorItem) -> u32,

    /// Returns the minimum address of this locator item.
    pub locatoritem_min_address: unsafe extern "C" fn(item: *const LocatorItem) -> usize,

    /// Returns the minimum address of this locator item.
    pub locatoritem_max_address: unsafe extern "C" fn(item: *const LocatorItem) -> usize,

    /// Returns true if the buffer is allocated, false otherwise.
    pub locatoritem_is_allocated: unsafe extern "C" fn(item: *const LocatorItem) -> bool,

    /// Returns true if the current item is locked, else false.
    pub locatoritem_is_taken: unsafe extern "C" fn(item: *const LocatorItem) -> bool,

    /// Tries to acquire the lock.
    ///
    /// Returns true if the lock was successfully acquired, false otherwise.
    pub locatoritem_try_lock: unsafe extern "C" fn(item: *mut LocatorItem) -> bool,

    /// Acquires the lock, blocking until it can do so.
    pub locatoritem_lock: unsafe extern "C" fn(item: *mut LocatorItem),

    /// Unlocks the object in a thread-safe manner.
    pub locatoritem_unlock: unsafe extern "C" fn(item: *mut LocatorItem),

    /// Determines if this locator item can be used given the constraints.
    ///
    /// # Arguments
    ///
    /// * `size` - Available bytes between `min_address` and `max_address`.
    /// * `min_address` - Minimum address accepted.
    /// * `max_address` - Maximum address accepted.
    ///
    /// # Returns
    ///
    /// Returns `true` if this buffer can be used given the parameters, and `false` otherwise.
    pub locatoritem_can_use: unsafe extern "C" fn(
        item: *const LocatorItem,
        size: u32,
        min_address: usize,
        max_address: usize,
    ) -> bool,

    /// Appends the data to this buffer.
    ///
    /// # Arguments
    ///
    /// * `data` - The data to append to the item.
    ///
    /// # Returns
    ///
    /// The address of the written data.
    ///
    /// # Remarks
    ///
    /// It is the caller's responsibility to ensure there is sufficient space in the buffer.
    /// When returning buffers from the library, the library will ensure there's at least the requested amount of space;
    /// so if the total size of your data falls under that space, you are good.
    ///
    /// # Safety
    ///
    /// This function is safe provided that the caller ensures that the buffer is large enough to hold the data.
    /// There is no error thrown if size is insufficient.
    pub locatoritem_append_bytes:
        unsafe extern "C" fn(item: *mut LocatorItem, data: *const u8, data_len: usize) -> usize,

    /// Clears the instruction cache for the specified range.
    ///
    /// # Arguments
    ///
    /// * `start` - The start address of the range to clear.
    /// * `end` - The end address of the range to clear.
    pub utilities_clear_instruction_cache: unsafe extern "C" fn(start: *mut u8, end: *mut u8),

    /// Call this method in order to safely be able to overwrite existing code that was
    /// allocated by the library inside one of its buffers. (e.g. Hooking/detours code.)
    ///
    /// This callback handles various edge cases, (such as flushing caches), and flipping page permissions
    /// on relevant platforms.
    ///
    /// # Parameters
    ///
    /// * `address` - The address of the code your callback will overwrite.
    /// * `size` - The size of the code your callback will overwrite.
    /// * `callback` - Your method to overwrite the code.
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
    pub overwrite_allocated_code:
        extern "C" fn(address: *const u8, size: usize, callback: extern "C" fn(*const u8, usize)),
}
