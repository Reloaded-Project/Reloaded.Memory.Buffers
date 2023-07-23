use crate::utilities::mathematics::add_with_overflow_cap;
use crate::utilities::wrappers::Unaligned;
use std::sync::atomic::{AtomicI32, Ordering};
use std::thread;

/// Individual item in the locator.
#[repr(C)]
pub struct LocatorItem {
    /// Address of the buffer in memory.
    pub base_address: Unaligned<usize>,
    /// Size of the data in memory.
    pub size: u32,
    /// Current position of the buffer.
    pub position: u32,
    /// True if this item is locked, else false.
    pub is_taken: AtomicI32,
}

impl LocatorItem {
    /// Creates a new instance of `LocatorItem` given base address and size.
    ///
    /// # Arguments
    ///
    /// * `base_address` - The base address.
    /// * `size` - The size.
    pub fn new(base_address: usize, size: u32) -> Self {
        Self {
            base_address: Unaligned::new(base_address),
            size,
            position: 0,
            is_taken: AtomicI32::new(0),
        }
    }

    /// Returns the amount of bytes left in the buffer.
    pub fn bytes_left(&self) -> u32 {
        self.size - self.position
    }

    /// Returns the minimum address of this locator item.
    pub fn min_address(&self) -> usize {
        self.base_address.value
    }

    /// Returns the minimum address of this locator item.
    pub fn max_address(&self) -> usize {
        self.base_address.value + self.size as usize
    }

    /// Returns true if the buffer is allocated, false otherwise.
    pub fn is_allocated(&self) -> bool {
        self.base_address.value != 0
    }

    /// Returns true if the current item is locked, else false.
    pub fn is_taken(&self) -> bool {
        let result = self.is_taken.load(Ordering::SeqCst);
        result == 1
    }

    /// Tries to acquire the lock.
    ///
    /// Returns true if the lock was successfully acquired, false otherwise.
    pub fn try_lock(&mut self) -> bool {
        let was_taken = self.is_taken.swap(1, Ordering::SeqCst);
        was_taken == 0
    }

    /// Acquires the lock, blocking until it can do so.
    pub fn lock(&mut self) {
        while !self.try_lock() {
            thread::yield_now();
        }
    }

    /// Unlocks the object in a thread-safe manner.
    pub fn unlock(&mut self) {
        // Need to amend C API if we ever need to do anything more here, since it forgets item.
        self.is_taken.store(0, Ordering::SeqCst);
    }

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
    pub fn can_use(&self, size: u32, min_address: usize, max_address: usize) -> bool {
        if !self.is_allocated() || self.bytes_left() < size {
            return false;
        }

        // Calculate the start and end positions within the buffer
        let start_available_address = self.base_address.value + self.position as usize;
        let end_available_address =
            add_with_overflow_cap(self.base_address.value, self.size as usize);

        // Check if the requested memory lies within the remaining buffer and within the specified address range
        // If any of the checks fail, the buffer can't be used
        // [start_available_address >= min_address] checks if in range.
        // [end_available_address <= max_address] checks if in range.
        start_available_address >= min_address && end_available_address <= max_address
    }

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
    pub unsafe fn append_bytes(&mut self, data: &[u8]) -> usize {
        let address = self.base_address.value + self.position as usize;
        let data_len = data.len();

        std::ptr::copy_nonoverlapping(data.as_ptr(), address as *mut u8, data_len);
        self.position += data_len as u32;

        address
    }

    /// Appends the variable to this buffer.
    ///
    /// # Arguments
    ///
    /// * `data` - The item to append to the buffer.
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
    pub unsafe fn append_copy<T>(&mut self, data: T) -> usize
    where
        T: Copy,
    {
        let address = (self.base_address.value + self.position as usize) as *mut T;
        *address = data;
        self.position += std::mem::size_of::<T>() as u32;
        address as usize
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use memoffset::offset_of;
    use std::mem::size_of;

    #[test]
    fn is_correct_size() {
        assert_eq!(0, offset_of!(LocatorItem, base_address));
        assert_eq!(size_of::<usize>(), offset_of!(LocatorItem, size));
        assert_eq!(size_of::<usize>() + 4, offset_of!(LocatorItem, position));
        assert_eq!(size_of::<usize>() + 8, offset_of!(LocatorItem, is_taken));

        let struct_size = if size_of::<usize>() == 4 { 16 } else { 20 };
        let size = size_of::<LocatorItem>();
        assert_eq!(size, struct_size);
    }

    #[test]
    fn try_lock_should_lock_item_when_lock_is_available() {
        // Arrange
        let mut item = LocatorItem::new(0, 0);

        // Act
        let result = item.try_lock();

        // Assert
        assert!(result);
        assert!(item.is_taken());
    }

    #[test]
    fn try_lock_should_not_lock_item_when_lock_is_already_acquired() {
        // Arrange
        let mut item = LocatorItem::new(0, 0);
        item.try_lock();

        // Act
        let result = item.try_lock();

        // Assert
        assert!(!result);
        assert!(item.is_taken());
    }

    #[test]
    fn lock_should_acquire_lock_when_lock_is_available() {
        // Arrange
        let mut item = LocatorItem::new(0, 0);

        // Act
        item.lock();

        // Assert
        assert!(item.is_taken());
    }

    #[test]
    fn unlock_should_release_lock_when_item_is_locked() {
        // Arrange
        let mut item = LocatorItem::new(0, 0);
        item.lock();

        // Act
        item.unlock();

        // Assert
        assert!(!item.is_taken());
    }

    #[test]
    fn min_address_should_return_base_address_when_called() {
        // Arrange
        let base_address: usize = 10;
        let size: u32 = 20;
        let item = LocatorItem::new(base_address, size);

        // Act
        let result = item.min_address();

        // Assert
        assert_eq!(result, base_address);
    }

    #[test]
    fn max_address_should_return_sum_of_base_address_and_size_when_called() {
        // Arrange
        let base_address: usize = 10;
        let size: u32 = 20;
        let item = LocatorItem::new(base_address, size);

        // Act
        let result = item.max_address();

        // Assert
        assert_eq!(result, base_address + size as usize);
    }

    #[test]
    fn can_use_should_return_expected_result() {
        // Test cases
        let test_cases = [
            (50, 100, 200, 50, true),   // size needed is available
            (50, 100, 200, 80, false),  // size needed is not available
            (50, 100, 200, 300, false), // size needed is beyond maxAddress
            (0, 100, 150, 0, true),     // no size needed, and at start
        ];

        for (position, base_address, max_address, size, expected) in &test_cases {
            // Arrange
            let locator_item = LocatorItem {
                base_address: Unaligned::new(*base_address),
                position: *position as u32,
                size: (max_address - base_address) as u32,
                is_taken: AtomicI32::new(0),
            };

            // Act
            let result = locator_item.can_use(*size, *base_address, *max_address);

            // Assert
            assert_eq!(result, *expected);
        }
    }

    #[test]
    fn append_bytes_should_append_data_to_buffer() {
        // Arrange
        let mut base_address: [u8; 100] = [0; 100];
        let size: u32 = 20;
        let data = [1, 2, 3, 4];
        let mut item = LocatorItem::new(base_address.as_mut_ptr() as usize, size);

        // Act
        let result = unsafe { item.append_bytes(&data) };

        // Assert
        assert_eq!(result, base_address.as_mut_ptr() as usize);
        assert_eq!(item.position, data.len() as u32);
    }

    #[test]
    fn append_copy_should_append_data_to_buffer() {
        // Arrange
        let mut base_address: [u8; 100] = [0; 100];
        let size: u32 = 20;
        let data: u32 = 42;
        let mut item = LocatorItem::new(base_address.as_mut_ptr() as usize, size);

        // Act
        let result = unsafe { item.append_copy(data) };

        // Assert
        assert_eq!(result, base_address.as_mut_ptr() as usize);
        assert_eq!(item.position, std::mem::size_of::<u32>() as u32);
    }
}
