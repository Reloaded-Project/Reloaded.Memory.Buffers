use crate::structs::errors::BufferAllocationError;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::utilities::address_range::AddressRange;
use crate::utilities::mathematics::{
    add_with_overflow_cap, round_down, round_up, subtract_with_underflow_cap,
};

#[cfg_attr(feature = "size_opt", optimize(size))]
pub fn allocate(
    settings: &mut BufferAllocatorSettings,
) -> Result<LocatorItem, BufferAllocationError> {
    settings.sanitize();

    #[cfg(target_os = "windows")]
    return crate::internal::buffer_allocator_windows::allocate_windows(settings);

    #[cfg(target_os = "linux")]
    return crate::internal::buffer_allocator_linux::allocate_linux(settings);

    #[cfg(target_os = "macos")]
    return crate::internal::buffer_allocator_osx::allocate_osx(settings);

    // Fallback for non-hot-path OSes.
    #[cfg(not(any(target_os = "macos", target_os = "windows", target_os = "linux")))]
    crate::internal::buffer_allocator_mmap_rs::allocate_mmap_rs(settings)
}

pub unsafe fn get_possible_buffer_addresses(
    minimum_ptr: usize,
    maximum_ptr: usize,
    page_start: usize,
    page_end: usize,
    buf_size: usize,
    allocation_granularity: usize,
    results: &mut [usize; 4],
) -> &[usize] {
    // Get range for page and min-max region.
    let min_max_range = AddressRange::new(minimum_ptr, maximum_ptr);
    let page_range = AddressRange::new(page_start, page_end);

    // Check if there is any overlap at all.
    if !page_range.overlaps(&min_max_range) {
        return &results[0..0];
    }

    // Three possible cases here:
    //   1. Page fits entirely inside min-max range and is smaller.
    if buf_size > page_range.size() {
        return &results[0..0]; // does not fit.
    }

    // Note: We have to test aligned to both page boundaries and min-max range boundaries;
    //       because, they may not perfectly overlap, e.g. min-max may be way greater than
    //       page size, so testing from start/end of that will not even overlap with available pages.
    //       Or the opposite can happen... min-max range may be smaller than page size.

    //   2. Min-max range is inside page, test aligned to page boundaries.

    // Round up from page min.
    let mut num_items = 0;

    let page_min_aligned = round_up(page_range.start_pointer, allocation_granularity);
    let page_min_range = AddressRange::new(
        page_min_aligned,
        add_with_overflow_cap(page_min_aligned, buf_size),
    );

    if page_range.contains(&page_min_range) && min_max_range.contains(&page_min_range) {
        results[num_items] = page_min_range.start_pointer;
        num_items += 1;
    }

    // Round down from page max.
    let page_max_aligned = round_down(
        subtract_with_underflow_cap(page_range.end_pointer, buf_size),
        allocation_granularity,
    );
    let page_max_range = AddressRange::new(page_max_aligned, page_max_aligned + buf_size);

    if page_range.contains(&page_max_range) && min_max_range.contains(&page_max_range) {
        results[num_items] = page_max_range.start_pointer;
        num_items += 1;
    }

    //   3. Min-max range is inside page, test aligned to Min-max range.

    // Round up from ptr min.
    let ptr_min_aligned = round_up(minimum_ptr, allocation_granularity);
    let ptr_min_range = AddressRange::new(
        ptr_min_aligned,
        add_with_overflow_cap(ptr_min_aligned, buf_size),
    );

    if page_range.contains(&ptr_min_range) && min_max_range.contains(&ptr_min_range) {
        results[num_items] = ptr_min_range.start_pointer;
        num_items += 1;
    }

    // Round down from ptr max.
    let ptr_max_aligned = round_down(
        subtract_with_underflow_cap(maximum_ptr, buf_size),
        allocation_granularity,
    );
    let ptr_max_range = AddressRange::new(ptr_max_aligned, ptr_max_aligned + buf_size);

    if page_range.contains(&ptr_max_range) && min_max_range.contains(&ptr_max_range) {
        results[num_items] = ptr_max_range.start_pointer;
        num_items += 1;
    }

    &results[0..num_items]
}

#[cfg(test)]
mod tests {
    use super::*;
    #[cfg(target_os = "windows")]
    use crate::internal::buffer_allocator_windows::{Kernel32, LocalKernel32};
    use crate::utilities::cached::CACHED;
    use std::ffi::c_void;

    const ALLOCATION_GRANULARITY: usize = 65536; // Assuming 64KB Allocation Granularity

    #[test]
    fn page_does_not_overlap_with_min_max() {
        let min_ptr = 100000;
        let max_ptr = 200000;
        let page_size = 50000;
        let buf_size = 30000;

        // No overlap between min-max range and page
        let page_start = max_ptr + 1;
        let page_end = page_start + page_size;

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )
            .len();
            assert_eq!(0, result);
        }
    }

    #[test]
    fn buffer_size_greater_than_page() {
        let min_ptr = 100000;
        let max_ptr = 200000;
        let page_size = 30000;
        let buf_size = 50000; // Greater than page_size

        // Page is within min-max range
        let page_start = min_ptr;
        let page_end = page_start + page_size;

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )
            .len();
            assert_eq!(0, result);
        }
    }

    #[test]
    fn round_up_from_ptr_min() {
        let min_ptr = 100000;
        let max_ptr = 200000;
        let page_size = 200000;
        let buf_size = 30000;

        // Page is bigger than min-max range
        let page_start = min_ptr - 50000;
        let page_end = page_start + page_size;

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )[0];
            assert!(result > 0);
        }
    }

    #[test]
    fn round_up_from_page_min() {
        let min_ptr = 1;
        let max_ptr = 200000;
        let page_size = 100000;
        let buf_size = 30000;

        // Page start is not aligned with allocation granularity
        let page_start = min_ptr + 5000; // Not multiple of 65536
        let page_end = page_start + page_size;

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )[0];
            assert_eq!(result, round_up(page_start, ALLOCATION_GRANULARITY));
        }
    }

    #[test]
    fn round_down_from_ptr_max() {
        let min_ptr = 10000;
        let mut max_ptr = 200000;
        let page_size = 1000000;
        let buf_size = 30000;

        // Max pointer is not aligned with allocation granularity
        max_ptr -= 5000; // Not multiple of 65536

        // Page start is aligned with allocation granularity
        let page_start = 80000;
        let page_end = page_start + page_size;

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )[0];
            assert_eq!(
                result,
                round_down(max_ptr - buf_size, ALLOCATION_GRANULARITY)
            );
        }
    }

    #[test]
    fn round_down_from_page_max() {
        let min_ptr = 1;
        let max_ptr = 200000;
        let page_size = 120000;
        let buf_size = 30000;

        // Page end is not aligned with allocation granularity
        let page_start = min_ptr;
        let page_end = page_start + page_size - 5000; // Not multiple of 65536

        unsafe {
            let buffer: &mut [usize; 4] = &mut [0; 4];
            let result = get_possible_buffer_addresses(
                min_ptr,
                max_ptr,
                page_start,
                page_end,
                buf_size,
                ALLOCATION_GRANULARITY,
                buffer,
            )[0];
            assert_eq!(
                result,
                round_down(page_end - buf_size, ALLOCATION_GRANULARITY)
            );
        }
    }

    // Allocation Tests

    #[test]
    #[cfg(not(target_os = "macos"))]
    fn can_allocate_in_2gib() {
        let mut settings = BufferAllocatorSettings {
            min_address: 0,
            max_address: i32::MAX as usize,
            size: 4096,
            target_process_id: CACHED.this_process_id,
            retry_count: 8,
            brute_force: false,
        };

        let item = allocate(&mut settings).unwrap();
        let base_addr = item.base_address.value;
        assert_ne!(base_addr, 0);
        assert!(item.size >= settings.size);
        free(item);
    }

    #[test]
    fn can_allocate_up_to_max_address() {
        let mut settings = BufferAllocatorSettings {
            min_address: CACHED.max_address / 2,
            max_address: CACHED.max_address,
            size: 4096,
            target_process_id: CACHED.this_process_id,
            retry_count: 8,
            brute_force: false,
        };

        let item = allocate(&mut settings).unwrap();
        let base_addr = item.base_address.value;
        assert_ne!(base_addr, 0);
        assert!(item.size >= settings.size);
        free(item);
    }

    // For testing use only.
    fn free(item: LocatorItem) {
        #[cfg(target_os = "windows")]
        free_windows(item);

        #[cfg(unix)]
        free_libc(item);
    }

    // For testing use only.
    #[cfg(target_os = "windows")]
    fn free_windows(item: LocatorItem) {
        let k32 = LocalKernel32 {};
        let success = k32.virtual_free(item.base_address.value as *mut c_void, 0);
        assert!(success);
    }

    #[cfg(unix)]
    fn free_libc(item: LocatorItem) {
        unsafe {
            libc::munmap(item.base_address.value as *mut c_void, item.size as usize);
        }
    }
}
