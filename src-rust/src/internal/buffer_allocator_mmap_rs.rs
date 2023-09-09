use core::cmp::min;
use core::mem;

use crate::structs::errors::BufferAllocationError;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::utilities::cached::CACHED;
use crate::utilities::map_parser_utilities::get_free_regions;
use crate::{
    internal::buffer_allocator::get_possible_buffer_addresses,
    utilities::map_parser_utilities::MemoryMapEntry,
};
use mmap_rs::{MemoryAreas, MmapOptions, UnsafeMmapFlags};

// Implementation //
pub fn allocate_mmap_rs(
    settings: &BufferAllocatorSettings,
) -> Result<LocatorItem, BufferAllocationError> {
    for _ in 0..settings.retry_count {
        let maps = MemoryAreas::open(None).map_err(|x| BufferAllocationError {
            settings: *settings,
            text: "Failed to Query Memory Pages via mmap-rs. Probably unsupported or lacking permissions.",
        })?;

        let mapped_regions: Vec<MemoryMapEntry> = maps
            .filter(|x| x.is_ok())
            .map(|x: Result<mmap_rs::MemoryArea, mmap_rs::Error>| unsafe {
                let area = x.unwrap_unchecked();
                MemoryMapEntry::new(area.start(), area.end())
            })
            .collect();

        let free_regions = get_free_regions(&mapped_regions);

        for region in free_regions {
            if region.start_address > settings.max_address {
                break;
            }

            unsafe {
                match try_allocate_buffer(&region, settings) {
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
        let mmapoptions = MmapOptions::new(settings.size as usize)
            .map_err(|_x| "Failed to create mmap options")?
            .with_address(*addr)
            .with_unsafe_flags(UnsafeMmapFlags::MAP_FIXED)
            .with_unsafe_flags(UnsafeMmapFlags::JIT);

        let map: Result<mmap_rs::MmapMut, mmap_rs::Error> = unsafe { mmapoptions.map_exec_mut() };
        if map.is_err() {
            continue;
        }

        let mapped = map.unwrap();
        let mapped_addr = mapped.start();

        if mapped.start() != *addr {
            continue; // dropped
        }

        mem::forget(mapped);
        return Ok(LocatorItem::new(mapped_addr, settings.size));
    }

    Err("Failed to allocate buffer")
}
