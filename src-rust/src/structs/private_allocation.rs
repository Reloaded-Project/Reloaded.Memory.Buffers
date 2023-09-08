#[cfg(any(target_os = "windows", target_os = "linux"))]
use std::ffi::c_void;
use std::ptr::NonNull;

use crate::utilities::cached::CACHED;

#[cfg(target_os = "windows")]
use {
    crate::internal::buffer_allocator_windows::ProcessHandle,
    windows::Win32::System::Memory::{VirtualFree, VirtualFreeEx, MEM_RELEASE},
};

#[cfg(target_os = "macos")]
use {
    mach::kern_return::KERN_SUCCESS,
    mach::traps::mach_task_self,
    mach::vm::mach_vm_deallocate,
    mach::vm_types::{mach_vm_address_t, mach_vm_size_t},
};

/// Provides information about a recently made allocation.
///
/// The memory is automatically deallocated when this struct is dropped,
/// if you wish to keep it around, make sure to store it in a field or somewhere else.
///
/// # Remarks
///
/// This structure is used as result of allocation options.
#[repr(C)]
pub struct PrivateAllocation {
    /// Address of the buffer in memory.
    pub base_address: NonNull<u8>,

    /// Exact size of allocated data.
    pub size: usize,

    /// Function that frees the memory.
    _this_process_id: u32,
}

impl PrivateAllocation {
    /// Creates a private allocation returned to user upon allocating a region of memory.
    ///
    /// # Arguments
    ///
    /// * `base_address`: The base address of the allocated memory.
    /// * `size`: The size of the allocated memory.
    /// * `process_id`: The process id of the process that allocated the memory.
    ///
    /// # Returns
    ///
    /// Returns an instance of `PrivateAllocation`.
    ///
    /// # Remarks
    ///
    /// If the current process id is equal to the actual process id, it uses the local process
    /// deallocation logic, otherwise it uses the external process deallocation logic. The external
    /// process memory management is not implemented in this example.
    pub fn new(base_address: NonNull<u8>, size: usize, process_id: u32) -> Self {
        Self {
            base_address,
            size,
            _this_process_id: process_id,
        }
    }

    /// Gets the base address of the allocation.
    ///
    /// # Returns
    ///
    /// Returns the base address of the allocation.
    pub fn base_address(&self) -> NonNull<u8> {
        self.base_address
    }

    /// Gets the size of the allocation.
    ///
    /// # Returns
    ///
    /// Returns the size of the allocation.
    pub fn size(&self) -> usize {
        self.size
    }

    /// Returns an empty allocation, intended to be used as a non-result when an error is present.
    pub(crate) fn null() -> Self {
        unsafe {
            Self {
                base_address: NonNull::new_unchecked(std::ptr::null_mut()),
                size: Default::default(),
                _this_process_id: Default::default(),
            }
        }
    }

    /// Frees the allocated memory when the `PrivateAllocation` instance is dropped.
    #[cfg(target_os = "windows")]
    fn drop_windows(&mut self) {
        unsafe {
            if self._this_process_id == CACHED.this_process_id {
                let result = VirtualFree(self.base_address.as_ptr() as *mut c_void, 0, MEM_RELEASE);
                if result.0 == 0 {
                    // "Failed to free memory on Windows"
                }
            } else {
                let process_handle = ProcessHandle::open_process(self._this_process_id);
                let result = VirtualFreeEx(
                    process_handle.unwrap().get_handle(),
                    self.base_address.as_ptr() as *mut c_void,
                    0,
                    MEM_RELEASE,
                );
                if result.0 == 0 {
                    // "Failed to free memory on Windows in External Process"
                }
            };
        }
    }

    /// Frees the allocated memory when the `PrivateAllocation` instance is dropped.
    #[cfg(target_os = "macos")]
    pub(crate) fn drop_macos(&mut self) {
        unsafe {
            if self._this_process_id == CACHED.this_process_id {
                let result = mach_vm_deallocate(
                    mach_task_self(),
                    self.base_address.as_ptr() as mach_vm_address_t,
                    self.size as mach_vm_size_t,
                );
                if result != KERN_SUCCESS {
                    // "Failed to free memory on MacOS"
                }
            } else {
                // Not Implemented
            };
        }
    }

    /// Frees the allocated memory when the `PrivateAllocation` instance is dropped.
    #[cfg(target_os = "linux")]
    pub(crate) fn drop_linux(&mut self) {
        unsafe {
            if self._this_process_id == CACHED.this_process_id {
                let result = libc::munmap(self.base_address.as_ptr() as *mut c_void, self.size);
                if result != 0 {
                    // Failed to free memory on Linux
                }
            } else {
                // Not Implemented
            };
        }
    }
}

impl Drop for PrivateAllocation {
    /// Frees the allocated memory when the `PrivateAllocation` instance is dropped.
    fn drop(&mut self) {
        #[cfg(target_os = "windows")]
        return PrivateAllocation::drop_windows(self);

        #[cfg(target_os = "linux")]
        return PrivateAllocation::drop_linux(self);

        #[cfg(target_os = "macos")]
        return PrivateAllocation::drop_macos(self);
    }
}

#[cfg(test)]
mod tests {
    use crate::{internal::buffer_allocator, structs::params::BufferAllocatorSettings};

    use super::*;

    #[test]
    fn test_private_allocation() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = CACHED.max_address / 2;
        settings.max_address = CACHED.max_address;

        let alloc = buffer_allocator::allocate(&mut settings).unwrap();
        let result = PrivateAllocation::new(
            NonNull::<u8>::new(alloc.base_address.value as *mut u8).unwrap(),
            alloc.size as usize,
            CACHED.this_process_id,
        );

        assert_ne!(result.base_address().as_ptr() as usize, 0);
        assert!(result.size() >= 4096);
    }
}
