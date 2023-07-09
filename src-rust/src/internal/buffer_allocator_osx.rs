use crate::internal::buffer_allocator::get_possible_buffer_addresses;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::utilities::cached::CACHED;
use crate::utilities::wrappers::Unaligned;
use libc::{mach_msg_type_number_t, mach_port_t, mach_task_self, mach_vm_size_t};
use mach::vm::{mach_vm_allocate, mach_vm_region};
use mach::vm_region;
use mach::vm_region::vm_region_basic_info_data_64_t;
use mach::vm_types::mach_vm_address_t;
use std::cmp::min;
use std::mem;

// Implementation //
pub fn allocate_osx(
    settings: &BufferAllocatorSettings,
) -> Result<LocatorItem, BufferAllocationError> {
    let max_address = min(CACHED.max_address, settings.max_address);
    let current_address = settings.min_address;

    unsafe {
        let self_task = mach_task_self();
        for _ in 0..settings.retry_count {
            let pages = get_free_pages(current_address, max_address, self_task)?;
            for page in pages {
                let mut result_addr: usize = 0;
                if try_allocate_buffer(page.0, page.1, &settings, self_task, &mut result_addr) {
                    return Ok(LocatorItem {
                        base_address: Unaligned(result_addr),
                        size: settings.size,
                        position: 0,
                        is_taken: Default::default(),
                    });
                }
            }
        }

        Err(BufferAllocationError::new(
            *settings,
            "Failed to allocate buffer on OSX",
        ))
    }
}

fn get_free_pages(
    min_address: usize,
    max_addr: usize,
    self_task: mach_port_t,
) -> Result<Vec<(usize, usize)>, &'static str> {
    let mut result: Vec<(usize, usize)> = Vec::new();
    let mut count = mem::size_of::<vm_region_basic_info_data_64_t>() as mach_msg_type_number_t;
    let mut object_name: mach_port_t = 0;

    let max_address = max_addr as u64;
    let mut current_address = min_address as u64;

    while current_address <= max_address {
        let mut actual_address = current_address;
        let mut available_size: u64 = 0;
        let mut region_info = vm_region_basic_info_data_64_t::default();
        let kr = unsafe {
            mach_vm_region(
                self_task,
                &mut actual_address,
                &mut available_size,
                vm_region::VM_REGION_BASIC_INFO_64,
                mem::transmute(&mut region_info),
                &mut count,
                &mut object_name,
            )
        };

        if kr == 1 {
            let padding = max_addr - current_address as usize;
            if padding > 0 {
                result.push((current_address as usize, padding));
            }
            break;
        }

        if kr != 0 {
            break;
        }

        let free_bytes = actual_address - current_address;
        if free_bytes > 0 {
            result.push((current_address as usize, free_bytes as usize));
        }

        current_address = actual_address + available_size;
    }

    Ok(result)
}

fn try_allocate_buffer(
    page_address: usize,
    page_size: usize,
    settings: &BufferAllocatorSettings,
    self_task: mach_port_t,
    result_addr: &mut usize,
) -> bool {
    let mut results: [usize; 4] = [0; 4];
    let buffer_pointers = get_buffer_pointers_in_page_range(
        page_address,
        page_size,
        settings.size as usize,
        settings.min_address,
        settings.max_address,
        &mut results,
    );

    for addr in buffer_pointers {
        let mut allocated: mach_vm_address_t = *addr as mach_vm_address_t;
        let kr = unsafe {
            mach_vm_allocate(
                self_task,
                &mut allocated,
                settings.size as mach_vm_size_t,
                0,
            )
        };

        if kr != 0 {
            continue;
        }

        *result_addr = allocated as usize;
        return true;
    }

    return false;
}

fn get_buffer_pointers_in_page_range(
    page_address: usize,
    page_size: usize,
    buffer_size: usize,
    minimum_ptr: usize,
    maximum_ptr: usize,
    results: &mut [usize; 4],
) -> &[usize] {
    let page_start = page_address;
    let page_end = page_address + page_size;
    let allocation_granularity = CACHED.allocation_granularity;
    unsafe {
        get_possible_buffer_addresses(
            minimum_ptr,
            maximum_ptr,
            page_start,
            page_end,
            buffer_size,
            allocation_granularity as usize,
            results,
        )
    }
}
