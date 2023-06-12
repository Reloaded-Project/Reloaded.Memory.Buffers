use std::cell::{Cell};
use crate::structs::internal::LocatorItem;

/// An individual item in the buffer locator that can be dropped (disposed).
///
/// The item behind this struct.
///
/// Use at your own risk.
/// Unsafe.
pub struct SafeLocatorItem {
    pub item: Cell<*mut LocatorItem>
}

impl SafeLocatorItem {
    /// Creates a disposable locator item.
    ///
    /// The item used with this constructor must be locked.
    pub unsafe fn new(item: *mut LocatorItem) -> Self {
        Self { item: Cell::new(item) }
    }

    /// Appends the data to this buffer.
    ///
    /// It is the caller's responsibility to ensure there is sufficient space in the buffer.
    /// When returning buffers from the library, the library will ensure there's at least 
    /// the requested amount of space; so if the total size of your data falls under that 
    /// space, you are good.
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
    pub unsafe fn append_copy<T>(&self, data: &T) -> usize
        where T: Copy {
        (*self.item.get()).append_copy(data)
    }
}

/// Safely dispose.
impl Drop for SafeLocatorItem {
    fn drop(&mut self) {
        unsafe { (*self.item.get()).unlock(); }
    }
}
