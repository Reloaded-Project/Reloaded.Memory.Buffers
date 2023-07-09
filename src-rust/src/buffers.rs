use crate::internal::buffer_allocator;
use crate::internal::locator_header_finder::LocatorHeaderFinder;
use crate::structs::errors::BufferAllocationError;
use crate::structs::internal::LocatorHeader;
use crate::structs::params::{BufferAllocatorSettings, BufferSearchSettings};
use crate::structs::{PrivateAllocation, SafeLocatorItem};
use std::ptr::NonNull;

pub struct Buffers {}

impl Buffers {
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
    pub fn allocate_private_memory(
        settings: &mut BufferAllocatorSettings,
    ) -> Result<PrivateAllocation, BufferAllocationError> {
        let alloc = buffer_allocator::allocate(settings)?;
        Ok(PrivateAllocation::new(
            NonNull::new(alloc.base_address.0 as *mut u8).unwrap(),
            alloc.size as usize,
            settings.target_process_id,
        ))
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
    pub fn get_buffer(
        settings: &BufferSearchSettings,
    ) -> Result<SafeLocatorItem, BufferAllocationError> {
        unsafe { Self::get_buffer_recursive(settings, LocatorHeaderFinder::find()) }
    }

    unsafe fn get_buffer_recursive(
        settings: &BufferSearchSettings,
        locator: *mut LocatorHeader,
    ) -> Result<SafeLocatorItem, BufferAllocationError> {
        let item = (*locator).get_first_available_item_locked(
            settings.size,
            settings.min_address,
            settings.max_address,
        );

        // If not null and we have an item, return.
        if item.is_some() {
            return Ok(item.unwrap_unchecked());
        }

        // Otherwise try to allocate a new one.
        if let Ok(new_item) =
            (*locator).try_allocate_item(settings.size, settings.min_address, settings.max_address)
        {
            return Ok(new_item);
        }

        // If we can't allocate a new one
        Self::get_buffer_recursive(settings, unsafe { (*locator).get_next_locator() })
    }
}

#[cfg(test)]
mod tests {
    use super::Buffers;
    use crate::structs::params::BufferSearchSettings;

    /// Baseline test to ensure that the buffer get logic is ok.
    #[test]
    fn test_get_buffer() {
        let settings = BufferSearchSettings {
            min_address: 0_usize,
            max_address: i32::MAX as usize,
            size: 4096,
        };

        // Automatically dropped.
        let item = Buffers::get_buffer(&settings).unwrap();

        // Append some data.
        let data = [0x0; 4096];
        unsafe {
            item.append_bytes(&data);
        }
    }
}
