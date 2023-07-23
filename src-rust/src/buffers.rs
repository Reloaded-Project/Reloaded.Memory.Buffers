use crate::internal::buffer_allocator;
use crate::internal::locator_header_finder::LocatorHeaderFinder;
use crate::structs::errors::{BufferAllocationError, BufferSearchError, ItemAllocationError};
use crate::structs::internal::LocatorHeader;
use crate::structs::params::{BufferAllocatorSettings, BufferSearchSettings};
use crate::structs::{PrivateAllocation, SafeLocatorItem};
use crate::utilities::mathematics::round_up;
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
            NonNull::new(alloc.base_address.value as *mut u8).unwrap(),
            alloc.size as usize,
            settings.target_process_id,
        ))
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
    pub fn get_buffer_aligned(
        settings: &BufferSearchSettings,
        alignment: u32,
    ) -> Result<SafeLocatorItem, BufferSearchError> {
        // Add expected size.
        let mut new_settings = *settings;
        new_settings.size += alignment;

        let result = Self::get_buffer(&new_settings);

        if result.is_ok() {
            // No error. (Hot Path)
            return result;
        }

        // If we have an error, pass it back.
        unsafe {
            let locator_item_cell = &result.as_ref().unwrap_unchecked().item;
            let locator_item = locator_item_cell.get();
            let base_address = (*locator_item).base_address.value;
            let aligned_address = round_up(base_address, alignment as usize);
            (*locator_item).base_address.value = aligned_address;
            result
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
    pub fn get_buffer(
        settings: &BufferSearchSettings,
    ) -> Result<SafeLocatorItem, BufferSearchError> {
        unsafe { Self::get_buffer_recursive(settings, LocatorHeaderFinder::find()) }
    }
}

impl Buffers {
    unsafe fn get_buffer_recursive(
        settings: &BufferSearchSettings,
        locator: *mut LocatorHeader,
    ) -> Result<SafeLocatorItem, BufferSearchError> {
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
        let result =
            (*locator).try_allocate_item(settings.size, settings.min_address, settings.max_address);

        match result {
            Ok(new_item) => {
                return Ok(new_item);
            }
            Err(error) => {
                if error == ItemAllocationError::CannotAllocateMemory {
                    return Err(BufferSearchError {
                        settings: *settings,
                        text: error.as_string(),
                    });
                }

                let next_locator = (*locator).get_next_locator();
                match next_locator {
                    Ok(locator) => Self::get_buffer_recursive(settings, locator),
                    Err(error) => Err(BufferSearchError {
                        settings: *settings,
                        text: error,
                    }),
                }
            }
        }
        // If we can't allocate a new one
    }
}

#[cfg(test)]
mod tests {
    use rstest::rstest;

    use super::Buffers;
    use crate::{
        internal::locator_header_finder::LocatorHeaderFinder,
        structs::params::{BufferAllocatorSettings, BufferSearchSettings},
        utilities::cached::CACHED,
    };
    use std;

    #[cfg(not(target_os = "macos"))]
    #[test]
    fn allocate_private_memory_in_2gib() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = 0;
        settings.max_address = std::i32::MAX as usize;

        let result = Buffers::allocate_private_memory(&mut settings);
        assert!(result.is_ok());

        let item = result.unwrap();
        assert!(!item.base_address.as_ptr().is_null());
        assert!(item.size >= settings.size as usize);
    }

    #[test]
    fn allocate_private_memory_up_to_max_address() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = CACHED.max_address / 2;
        settings.max_address = CACHED.max_address;

        let result = Buffers::allocate_private_memory(&mut settings);
        assert!(result.is_ok());

        let item = result.unwrap();
        assert!(!item.base_address.as_ptr().is_null());
        assert!(item.size >= settings.size as usize);
    }

    /// Baseline test to ensure that the buffer get logic is ok.
    #[test]
    fn get_buffer_baseline() {
        let settings = BufferSearchSettings {
            min_address: (CACHED.max_address / 2),
            max_address: CACHED.max_address,
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

    #[rstest]
    #[case(64)]
    #[case(128)]
    #[case(256)]
    fn get_buffer_aligned_test(#[case] alignment: u32) {
        let settings = BufferSearchSettings {
            min_address: (CACHED.max_address / 2),
            max_address: CACHED.max_address,
            size: 4096,
        };

        // The function should succeed with these settings.
        let item = Buffers::get_buffer_aligned(&settings, alignment);

        match item {
            Ok(item) => {
                // Check that the address is aligned as expected.
                unsafe {
                    assert_eq!(
                        (*item.item.get()).base_address.value % alignment as usize,
                        0
                    );
                }
            }
            Err(err) => {
                // Handle the error (just print here).
                println!("Error getting buffer: {}", err);
                unreachable!();
            }
        }
    }

    #[test]
    fn get_buffer_with_proximity() {
        const SIZE: usize = 4096;
        let base_address = CACHED.max_address - (std::i32::MAX as usize);

        unsafe {
            LocatorHeaderFinder::reset();
        }

        let item = Buffers::get_buffer(&BufferSearchSettings::from_proximity(
            std::i32::MAX as usize,
            base_address,
            SIZE,
        ));

        assert!(item.is_ok());

        unsafe {
            let locator_item = item.unwrap().item.get();
            assert!((*locator_item).size as usize >= SIZE);

            let offset = ((*locator_item).base_address.value as i64 - base_address as i64).abs();
            assert!(offset < (i32::MAX as i64));
        }
    }
}
