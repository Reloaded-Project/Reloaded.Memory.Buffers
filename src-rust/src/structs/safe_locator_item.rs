use crate::structs::internal::LocatorItem;
use std::cell::Cell;

/// An individual item in the buffer locator that can be dropped (disposed).
///
/// The item behind this struct.
///
/// Use at your own risk.
/// Unsafe.
#[repr(C)]
pub struct SafeLocatorItem {
    pub item: Cell<*mut LocatorItem>,
}

impl SafeLocatorItem {
    /// Appends the code to this buffer.
    /// This is same as [`append_bytes`] but automatically clears the instruction cache on given CPU.
    ///
    /// It is the caller's responsibility to ensure there is sufficient space in the buffer.
    /// When returning buffers from the library, the library will ensure there's at least
    /// the requested amount of space; so if the total size of your data falls under that
    /// space, you are good.
    ///
    /// # Arguments
    ///
    /// * `data` - The data to append to the item.
    ///
    /// # Returns
    ///
    /// The address of the written data.
    ///
    /// # Safety
    ///
    /// This function is safe provided that the caller ensures that the buffer is large enough to hold the data.
    /// There is no error thrown if size is insufficient.
    pub unsafe fn append_code(&self, data: &[u8]) -> usize {
        (*self.item.get()).append_code(data)
    }

    /// Appends the data to this buffer.
    ///
    /// It is the caller's responsibility to ensure there is sufficient space in the buffer.
    /// When returning buffers from the library, the library will ensure there's at least
    /// the requested amount of space; so if the total size of your data falls under that
    /// space, you are good.
    ///
    /// # Safety
    ///
    /// This function is unsafe because it writes to raw (untracked by Rust) memory.
    pub unsafe fn append_bytes(&self, data: &[u8]) -> usize {
        (*self.item.get()).append_bytes(data)
    }

    /// Appends the blittable variable to this buffer.
    ///
    /// Type of the item to write.
    /// The item to append to the buffer.
    /// Returns: Address of the written data.
    ///
    /// It is the caller's responsibility to ensure there is sufficient space in the buffer.
    /// When returning buffers from the library, the library will ensure there's at least
    /// the requested amount of space; so if the total size of your data falls under that
    /// space, you are good.
    ///
    /// # Safety
    ///
    /// This function is unsafe because it writes to raw (untracked by Rust) memory.
    pub unsafe fn append_copy<T>(&self, data: &T) -> usize
    where
        T: Copy,
    {
        (*self.item.get()).append_copy(data)
    }
}

/// Safely dispose.
impl Drop for SafeLocatorItem {
    fn drop(&mut self) {
        unsafe {
            // Need to amend C API if we ever need to do anything more here, since it forgets item.
            (*self.item.get()).unlock();
        }
    }
}
