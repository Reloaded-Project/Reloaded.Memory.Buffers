use crate::structs::{
    errors::BufferAllocationError,
    params::{BufferAllocatorSettings, BufferSearchSettings},
    PrivateAllocation, SafeLocatorItem,
};

pub trait BuffersApi {
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
    fn allocate_private_memory(
        settings: &mut BufferAllocatorSettings,
    ) -> Result<PrivateAllocation, BufferAllocationError>;

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
    fn get_buffer_aligned(
        settings: &BufferSearchSettings,
        alignment: u32,
    ) -> Result<SafeLocatorItem, BufferAllocationError>;

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
    fn get_buffer(
        settings: &BufferSearchSettings,
    ) -> Result<SafeLocatorItem, BufferAllocationError>;
}
