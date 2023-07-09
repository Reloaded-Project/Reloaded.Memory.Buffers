use crate::internal::buffer_allocator::get_possible_buffer_addresses;
use crate::structs::errors::BufferAllocationError;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::utilities::cached::CACHED;
use crate::utilities::linux_map_parser::{get_free_regions_from_process_id, MemoryMapEntry};
use libc::{
    mmap, munmap, MAP_ANONYMOUS, MAP_FIXED_NOREPLACE, MAP_PRIVATE, PROT_EXEC, PROT_READ, PROT_WRITE,
};

// Implementation //
pub fn allocate_linux(
    settings: &BufferAllocatorSettings,
) -> Result<LocatorItem, BufferAllocationError> {
    for _ in 0..settings.retry_count {
        let regions = get_free_regions_from_process_id(settings.target_process_id as i32);
        for region in regions {
            if region.start_address > settings.max_address {
                break;
            }

            unsafe {
                match try_allocate_buffer(&region, &settings) {
                    Ok(item) => return Ok(item),
                    Err(_) => continue,
                }
            }
        }
    }

    Err(BufferAllocationError::new(
        *settings,
        "Failed to allocate buffer on Linux",
    ))
}

unsafe fn try_allocate_buffer(
    entry: &MemoryMapEntry,
    settings: &BufferAllocatorSettings,
) -> Result<LocatorItem, &'static str> {
    let buffer: &mut [usize; 4] = &mut [0; 4];

    for addr in get_possible_buffer_addresses(
        settings.min_address,
        settings.max_address,
        entry.start_address,
        entry.end_address,
        settings.size as usize,
        CACHED.get_allocation_granularity() as usize,
        buffer,
    ) {
        let allocated = mmap(
            *addr as *mut _,
            settings.size as usize,
            PROT_READ | PROT_WRITE | PROT_EXEC,
            MAP_PRIVATE | MAP_ANONYMOUS | MAP_FIXED_NOREPLACE,
            -1,
            0,
        );

        if allocated.is_null() {
            continue;
        }

        if allocated as usize != *addr {
            munmap(allocated, settings.size as usize);
            continue;
        }

        return Ok(LocatorItem::new(allocated as usize, settings.size));
    }

    Err("Failed to allocate buffer")
}
