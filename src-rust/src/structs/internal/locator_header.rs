use crate::internal::buffer_allocator::allocate;
use crate::structs::errors::ItemAllocationError;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::structs::SafeLocatorItem;
use crate::utilities::cached::CACHED;
use crate::utilities::wrappers::Unaligned;
use std::alloc::{alloc, Layout};
use std::cell::Cell;
use std::cmp::min;
use std::mem::size_of;
use std::sync::atomic::{AtomicI32, Ordering};

/// Static length of this locator.
pub(crate) const LENGTH: usize = 4096;

/// Length of buffers preallocated in this locator.
///
/// # Remarks
///
/// On Windows there is an allocation granularity (normally 64KB) which means that
/// minimum amount of bytes you can allocate is 64KB; even if you only need 1 byte.
///
/// Our locator is a 4096 byte structure which means that it would be a waste to not
/// do anything with the remaining data. So we chunk the remaining data by this amount
/// and pre-register them as buffers.
pub(crate) const LENGTH_OF_PREALLOCATED_CHUNKS: u32 = 16384;

/// Returns the maximum possible amount of items in this locator.
pub(crate) const MAX_ITEM_COUNT: u32 =
    ((LENGTH - size_of::<LocatorHeader>()) / size_of::<LocatorItem>()) as u32;

/// Represents the header of an individual memory locator.
#[repr(C, align(1))]
pub struct LocatorHeader {
    pub this_address: Unaligned<*mut LocatorHeader>,
    pub next_locator_ptr: Unaligned<*mut LocatorHeader>,
    pub is_locked: AtomicI32,
    pub flags: u8,
    pub num_items: u8,
    padding: [u8; 2],
}

impl LocatorHeader {
    /// Creates a new LocatorHeader instance.
    #[allow(clippy::new_without_default)]
    #[cfg(test)]
    pub fn new() -> Self {
        LocatorHeader {
            this_address: Unaligned::new(std::ptr::null_mut()),
            next_locator_ptr: Unaligned::new(std::ptr::null_mut::<LocatorHeader>()),
            is_locked: AtomicI32::new(0),
            flags: 0,
            num_items: 0,
            padding: [0; 2],
        }
    }

    /// Initializes the locator header values at a specific address.
    /// # Arguments
    ///
    /// * `length` - Number of bytes available.
    pub(crate) fn initialize(&mut self, length: usize) {
        self.set_default_values();
        let remaining_bytes = (length - LENGTH) as u32;

        // We allocate to allocation_granularity, however, under some platforms (*cough* M1 macOS)
        // W^X policy is enforced, in which case, we cannot allocate executable memory here,
        // as the header would also be affected.

        // We will use the remaining space for more headers on these affected platforms, and
        // on non-W^X platforms, we will use it for buffers.
        #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
        initialize_remaining_space_as_headers(&self as *mut LocatorHeader, remaining_bytes);

        #[cfg(not(all(target_os = "macos", target_arch = "aarch64")))]
        self.initialize_remaining_space_as_buffers(remaining_bytes);
    }

    fn set_default_values(&mut self) {
        self.this_address = Unaligned::new(self as *mut LocatorHeader);
        self.next_locator_ptr = Unaligned::new(std::ptr::null_mut());
        self.is_locked = AtomicI32::new(0);
        self.flags = 0;
        self.num_items = 0;
    }

    fn initialize_remaining_space_as_buffers(&mut self, mut remaining_bytes: u32) {
        let mut num_items = 0u8;
        unsafe {
            let buffer_address = (self.this_address.value as *mut u8).add(LENGTH);
            let mut current_item = self.get_first_item();

            while remaining_bytes > 0 {
                let this_length = min(LENGTH_OF_PREALLOCATED_CHUNKS, remaining_bytes);
                *current_item = LocatorItem::new(buffer_address as usize, this_length);
                current_item = current_item.offset(1);
                remaining_bytes -= this_length;
                num_items += 1;
            }
        }

        self.num_items = num_items;
    }

    #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
    fn initialize_remaining_space_as_headers(header: *mut LocatorHeader, mut remaining_bytes: u32) {
        unsafe {
            let mut current_header = header;
            while remaining_bytes > LENGTH {
                let next_header = (current_header as *mut u8).add(LENGTH) as *mut LocatorHeader;
                (*next_header).set_default_values();
                (*current_header).next_locator_ptr = next_header;
                current_header = next_header;
                remaining_bytes -= LENGTH;
            }
        }
    }

    /// Returns the version represented by the first 3 bits of `flags`.
    #[allow(dead_code)]
    pub fn version(&self) -> u8 {
        self.flags & 0x07
    }

    /// Sets the version represented by the first 3 bits of `flags`.
    #[allow(dead_code)]
    pub fn set_version(&mut self, value: u8) {
        self.flags = (self.flags & 0xF8) | (value & 0x07);
    }

    /// Returns true if next locator is present.
    pub fn has_next_locator(&self) -> bool {
        !self.next_locator_ptr.value.is_null()
    }

    /// Returns true if this buffer is full.
    pub fn is_full(&self) -> bool {
        self.num_items as usize >= MAX_ITEM_COUNT as usize
    }

    /// Tries to acquire the lock.
    ///
    /// Returns: True if the lock was successfully acquired, false otherwise.
    pub fn try_lock(&mut self) -> bool {
        // Since Rust doesn't have a direct equivalent of C#'s `Interlocked.CompareExchange`,
        // we need to use the atomic operations from the `std::sync::atomic` module.
        self.is_locked
            .compare_exchange(0, 1, Ordering::AcqRel, Ordering::Acquire)
            .is_ok()
    }

    /// Acquires the lock, blocking until it can do so.
    pub fn lock(&mut self) {
        while !self.try_lock() {
            std::thread::yield_now();
        }
    }

    /// Unlocks the object in a thread-safe manner.
    ///
    /// # Panics
    ///
    /// If the buffer is already unlocked, this error is thrown.
    /// It is only thrown in debug mode.
    pub fn unlock(&mut self) {
        // Set _is_locked to 0 and return the original value.
        let original = self.is_locked.swap(0, Ordering::AcqRel);

        // If the original value was already 0, something went wrong.
        debug_assert_ne!(
            original, 0,
            "Attempted to unlock a LocatorHeader that wasn't locked"
        );
    }

    /// Gets the first item.
    pub fn get_first_item(&self) -> *mut LocatorItem {
        // Add 1 to the address to get the address of the first item.
        unsafe { (self as *const LocatorHeader).add(1) as *mut LocatorItem }
    }

    /// Gets the item at a specific index.
    ///
    /// index: Index to get item at.
    pub fn get_item(&self, index: usize) -> *mut LocatorItem {
        unsafe { self.get_first_item().add(index) }
    }

    /// Gets the first available item with a lock.
    ///
    /// # Arguments
    ///
    /// * `size` - Required size of the buffer.
    /// * `min_address` - Minimum address for the allocation.
    /// * `max_address` - Maximum address for the allocation.
    ///
    /// # Safety
    ///
    /// Uses raw pointers, thus is technically unsafe.
    ///
    /// # Returns
    ///
    /// Returns a locked locator item. Make sure to properly dispose of it using the appropriate method,
    /// as disposing will release the lock.
    pub unsafe fn get_first_available_item_locked(
        &self,
        size: u32,
        min_address: usize,
        max_address: usize,
    ) -> Option<SafeLocatorItem> {
        let mut current_item = self.get_first_item();
        let final_item = current_item.add(self.num_items as usize);
        while current_item < final_item {
            let item_ref = &mut *current_item;
            if item_ref.can_use(size, min_address, max_address) && item_ref.try_lock() {
                return Some({
                    let item: *mut LocatorItem = item_ref;
                    SafeLocatorItem {
                        item: Cell::new(item),
                    }
                });
            }

            current_item = current_item.offset(1);
        }

        None
    }

    /// Tries to allocate an additional item in the header, if possible.
    ///
    /// # Arguments
    ///
    /// * `size` - Required size of buffer.
    /// * `min_address` - Minimum address for the allocation.
    /// * `max_address` - Maximum address for the allocation.
    ///
    /// # Returns
    ///
    /// Returns the successfully allocated buffer (locked) wrapped in an Option.
    ///
    /// # Remarks
    ///
    /// If an item can't be allocated, there are simply no slots left.
    ///
    /// # Errors
    ///
    /// If the memory cannot be allocated within the needed constraints, this function will return `Err(MemoryBufferAllocationException)`.
    ///
    /// # Safety
    ///
    /// This function is unsafe as it requires the caller to ensure that calls are properly synchronized.
    /// Concurrent access without synchronization can lead to undefined behavior.
    pub fn try_allocate_item(
        &mut self,
        size: u32,
        min_address: usize,
        max_address: usize,
    ) -> Result<SafeLocatorItem, ItemAllocationError> {
        if self.is_full() {
            return Err(ItemAllocationError::NoSpaceInHeader);
        }

        // Note: We don't need to check if an item was created while we were waiting for the lock,
        // because the item in question will be locked by the one who created it.
        // We only need to (re)check if there's space.
        self.lock();

        if self.is_full() {
            self.unlock();
            return Err(ItemAllocationError::NoSpaceInHeader);
        }

        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = min_address;
        settings.max_address = max_address;
        settings.size = size;
        let result = allocate(&mut settings);

        match result {
            Ok(mut allocated_memory) => {
                allocated_memory.lock();

                unsafe {
                    let target = self.get_item(self.num_items as usize);
                    *target = allocated_memory;
                    let item = SafeLocatorItem {
                        item: Cell::new(target),
                    };

                    self.num_items += 1;
                    self.unlock();
                    Ok(item)
                }
            }
            Err(_) => {
                self.unlock();
                Err(ItemAllocationError::CannotAllocateMemory)
            }
        }
    }

    /// Gets the next header in the chain, allocating it if necessary.
    ///
    /// # Returns
    ///
    /// Result with the address of next header, or error string.
    ///
    pub fn get_next_locator(&mut self) -> Result<*mut LocatorHeader, &'static str> {
        // No-op if already exists.
        if self.has_next_locator() {
            return Ok(self.next_locator_ptr.value);
        }

        self.lock();

        // Check again, in case it was created while we were waiting for the lock.
        if self.has_next_locator() {
            self.unlock();
            return Ok(self.next_locator_ptr.value);
        }

        // Allocate the next locator.
        let alloc_size = CACHED.get_allocation_granularity();
        unsafe {
            let addr = alloc(
                Layout::from_size_align(alloc_size as usize, CACHED.page_size as usize).unwrap(),
            );
            if addr.is_null() {
                self.unlock();
                return Err(
                    "Failed to allocate memory for LocatorHeader. Is this process out of memory?",
                );
            }

            self.next_locator_ptr.value = addr as *mut LocatorHeader;
            (*self.next_locator_ptr.value).initialize(alloc_size as usize);
            self.unlock();

            Ok(self.next_locator_ptr.value)
        }
    }
}

#[cfg(test)]
mod tests {
    use crate::structs::internal::locator_header::{Unaligned, LENGTH, MAX_ITEM_COUNT};
    use crate::structs::internal::LocatorHeader;
    use crate::utilities::cached::CACHED;
    use memoffset::offset_of;
    use std::alloc::{alloc, Layout};
    use std::mem::{align_of, size_of};
    use std::sync::atomic::Ordering;

    // Ternary Operator
    macro_rules! expected_offset {
        ($true_value:expr, $false_value:expr) => {
            if size_of::<usize>() == 8 {
                $true_value
            } else {
                $false_value
            }
        };
    }

    #[test]
    fn is_correct_size() {
        let expected = if size_of::<usize>() == 4 { 16 } else { 24 };
        assert_eq!(size_of::<LocatorHeader>(), expected);

        assert_eq!(
            expected_offset!(0, 0),
            offset_of!(LocatorHeader, this_address)
        );
        assert_eq!(
            expected_offset!(8, 4),
            offset_of!(LocatorHeader, next_locator_ptr)
        );
        assert_eq!(
            expected_offset!(16, 8),
            offset_of!(LocatorHeader, is_locked)
        );
        assert_eq!(expected_offset!(20, 12), offset_of!(LocatorHeader, flags));
        assert_eq!(
            expected_offset!(21, 13),
            offset_of!(LocatorHeader, num_items)
        );
    }

    #[test]
    fn has_correct_max_item_count() {
        let expected = if size_of::<usize>() == 4 { 255 } else { 203 };
        assert_eq!(MAX_ITEM_COUNT, expected);
    }

    #[test]
    fn try_lock_should_lock_header_when_lock_is_available() {
        // Arrange
        let mut header = LocatorHeader::new();

        // Act
        let result = header.try_lock();

        // Assert
        assert!(result);
        assert_eq!(1, header.is_locked.load(Ordering::Acquire));
    }

    #[test]
    fn try_lock_should_not_lock_header_when_lock_is_already_acquired() {
        // Arrange
        let mut header = LocatorHeader::new();
        header.try_lock();

        // Act
        let result = header.try_lock();

        // Assert
        assert!(!result);
        assert_eq!(1, header.is_locked.load(Ordering::Acquire));
    }

    #[test]
    fn lock_should_acquire_lock_when_lock_is_available() {
        // Arrange
        let mut header = LocatorHeader::new();

        // Act
        header.lock();

        // Assert
        assert_eq!(1, header.is_locked.load(Ordering::Acquire));
    }

    #[test]
    fn unlock_should_release_lock_when_header_is_locked() {
        // Arrange
        let mut header = LocatorHeader::new();
        header.lock();

        // Act
        header.unlock();

        // Assert
        assert_eq!(0, header.is_locked.load(Ordering::Acquire));
    }

    #[test]
    fn version_should_be_3_bits() {
        let mut header = LocatorHeader::new();

        for value in 0..8 {
            // 3 bits can represent 8 different values
            header.set_version(value);
            assert_eq!(header.version(), value);
        }

        // Values larger than 3 bits should overflow and only retain the least significant 3 bits
        header.set_version(8);
        assert_eq!(header.version(), 0);
    }

    #[cfg(debug_assertions)] // This code will only be compiled in debug mode
    #[test]
    #[should_panic(expected = "Attempted to unlock a LocatorHeader that wasn't locked")]
    fn unlock_should_throw_exception_when_header_is_not_locked() {
        let mut header = LocatorHeader::new();
        header.unlock();
    }

    #[test]
    fn get_first_available_item_locked_should_return_expected_result() {
        unsafe {
            // Arrange
            let mut header_buf: [u8; LENGTH] = [0; LENGTH];
            let header: *mut LocatorHeader = header_buf.as_mut_ptr() as *mut LocatorHeader;

            (*header).this_address = Unaligned::new(header);
            (*header).num_items = 2;

            let first_item = (*header).get_first_item();
            (*first_item).base_address = Unaligned::new(100);
            (*first_item).size = 50;
            (*first_item).position = 25;

            let second_item = (*header).get_item(1);
            (*second_item).base_address = Unaligned::new(200);
            (*second_item).size = 50;
            (*second_item).position = 25;

            // Act
            let result = (*header).get_first_available_item_locked(25, 100, 300);

            // Assert
            assert!(result.is_some());
            let result = result.unwrap();
            let locator_item = result.item.get();
            let base_address = (*locator_item).base_address.value;
            assert_eq!(base_address, 100);
            assert!((*locator_item).is_taken());
        }
    }

    #[test]
    fn get_first_available_item_locked_should_return_null_if_no_available_item_because_size_is_insufficient(
    ) {
        unsafe {
            // Arrange
            let mut header_buf: [u8; LENGTH] = [0; LENGTH];
            let header: *mut LocatorHeader = header_buf.as_mut_ptr() as *mut LocatorHeader;

            (*header).this_address = Unaligned::new(header);
            (*header).num_items = 2;

            let first_item = (*header).get_first_item();
            (*first_item).base_address = Unaligned::new(100);
            (*first_item).size = 50;
            (*first_item).position = 30;

            let second_item = (*header).get_item(1);
            (*second_item).base_address = Unaligned::new(200);
            (*second_item).size = 50;
            (*second_item).position = 30;

            // Act
            let result = (*header).get_first_available_item_locked(25, 100, 300);

            // Assert
            assert!(result.is_none());
        }
    }

    #[test]
    fn get_first_available_item_locked_should_return_null_if_no_available_item_because_no_buffer_fits_range(
    ) {
        unsafe {
            // Arrange
            let mut header_buf: [u8; LENGTH] = [0; LENGTH];
            let header: *mut LocatorHeader = header_buf.as_mut_ptr() as *mut LocatorHeader;

            (*header).this_address = Unaligned::new(header);
            (*header).num_items = 2;

            let first_item = (*header).get_first_item();
            (*first_item).base_address = Unaligned::new(100);
            (*first_item).size = 50;
            (*first_item).position = 0;

            let second_item = (*header).get_item(1);
            (*second_item).base_address = Unaligned::new(200);
            (*second_item).size = 50;
            (*second_item).position = 0;

            // Act
            let result = (*header).get_first_available_item_locked(25, 0, 100);

            // Assert
            assert!(result.is_none());
        }
    }

    #[test]
    fn try_allocate_item_should_allocate_item_when_header_is_not_full_and_within_address_limits() {
        // Arrange
        let ptr =
            unsafe { alloc(Layout::from_size_align(LENGTH, align_of::<LocatorHeader>()).unwrap()) };
        let header_ptr = ptr as *mut LocatorHeader;
        let header = unsafe { &mut *header_ptr };
        header.initialize(LENGTH);

        let size = 100;
        let min_address = CACHED.max_address / 2;
        let max_address = CACHED.max_address;

        // Act
        let item_count = header.num_items;
        let result = header.try_allocate_item(size, min_address, max_address);
        assert_eq!(item_count + 1, header.num_items);

        // Assert
        assert!(result.is_ok());
        unsafe {
            let item = result.unwrap_unchecked();
            let address = (*item.item.get()).base_address.value;
            assert!(address >= min_address);
            assert!(address <= max_address);
        }
    }

    #[test]
    fn try_allocate_item_should_not_allocate_item_when_header_is_full() {
        // Arrange
        let ptr =
            unsafe { alloc(Layout::from_size_align(LENGTH, align_of::<LocatorHeader>()).unwrap()) };
        let header_ptr = ptr as *mut LocatorHeader;
        let header = unsafe { &mut *header_ptr };
        header.initialize(LENGTH);
        header.num_items = MAX_ITEM_COUNT as u8;

        let size = 100;
        let min_address = CACHED.max_address / 2;
        let max_address = CACHED.max_address;

        // Act
        let item_count = header.num_items;
        let result = header.try_allocate_item(size, min_address, max_address);
        assert_eq!(item_count, header.num_items);

        // Assert
        assert!(result.is_err());
    }

    #[test]
    fn try_allocate_item_should_not_allocate_item_when_outside_address_limits() {
        // Arrange
        let ptr =
            unsafe { alloc(Layout::from_size_align(LENGTH, align_of::<LocatorHeader>()).unwrap()) };
        let header_ptr = ptr as *mut LocatorHeader;
        let header = unsafe { &mut *header_ptr };
        header.initialize(LENGTH);

        let size = 100;
        let min_address = 0;
        let max_address = 10; // Set maxAddress to a small value to make allocation impossible

        // Act
        assert!(header
            .try_allocate_item(size, min_address, max_address)
            .is_err());
    }

    #[test]
    fn get_next_locator_should_allocate_when_newly_created() {
        // Arrange
        let ptr =
            unsafe { alloc(Layout::from_size_align(LENGTH, align_of::<LocatorHeader>()).unwrap()) };
        let header_ptr = ptr as *mut LocatorHeader;
        let header = unsafe { &mut *header_ptr };
        header.initialize(LENGTH);
        header.num_items = MAX_ITEM_COUNT as u8;

        // Act
        let next = header.get_next_locator().unwrap();
        let next_cached = header.get_next_locator().unwrap();

        // Assert
        assert_eq!(next as usize, next_cached as usize);
        assert_ne!(next as usize, 0);
    }
}
