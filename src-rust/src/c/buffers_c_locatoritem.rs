use crate::structs::internal::LocatorItem;

/// Returns the amount of bytes left in the buffer.
#[no_mangle]
pub extern "C" fn locatoritem_bytes_left(item: *const LocatorItem) -> u32 {
    unsafe { (*item).bytes_left() }
}

/// Returns the minimum address of this locator item.
#[no_mangle]
pub extern "C" fn locatoritem_min_address(item: *const LocatorItem) -> usize {
    unsafe { (*item).min_address() }
}

/// Returns the minimum address of this locator item.
#[no_mangle]
pub extern "C" fn locatoritem_max_address(item: *const LocatorItem) -> usize {
    unsafe { (*item).max_address() }
}

/// Returns true if the buffer is allocated, false otherwise.
#[no_mangle]
pub extern "C" fn locatoritem_is_allocated(item: *const LocatorItem) -> bool {
    unsafe { (*item).is_allocated() }
}

/// Returns true if the current item is locked, else false.
#[no_mangle]
pub extern "C" fn locatoritem_is_taken(item: *const LocatorItem) -> bool {
    unsafe { (*item).is_taken() }
}

/// Tries to acquire the lock.
///
/// Returns true if the lock was successfully acquired, false otherwise.
#[no_mangle]
pub extern "C" fn locatoritem_try_lock(item: *mut LocatorItem) -> bool {
    unsafe { (*item).try_lock() }
}

/// Acquires the lock, blocking until it can do so.
#[no_mangle]
pub extern "C" fn locatoritem_lock(item: *mut LocatorItem) {
    unsafe { (*item).lock() }
}

/// Unlocks the object in a thread-safe manner.
#[no_mangle]
pub extern "C" fn locatoritem_unlock(item: *mut LocatorItem) {
    unsafe { (*item).unlock() }
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
#[no_mangle]
pub extern "C" fn locatoritem_can_use(
    item: *const LocatorItem,
    size: u32,
    min_address: usize,
    max_address: usize,
) -> bool {
    unsafe { (*item).can_use(size, min_address, max_address) }
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
#[no_mangle]
pub unsafe extern "C" fn locatoritem_append_bytes(
    item: *mut LocatorItem,
    data: *const u8,
    data_len: usize,
) -> usize {
    (*item).append_bytes(std::slice::from_raw_parts(data, data_len))
}
