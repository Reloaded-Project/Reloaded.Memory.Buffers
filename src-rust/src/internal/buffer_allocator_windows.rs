﻿// Windows specific code for buffer allocator.
use std::ffi::c_void;
use std::sync::atomic::AtomicI32;
use Threading::IsWow64Process;
use windows::Win32::Foundation::{BOOL, CloseHandle, HANDLE};
use windows::Win32::System::Memory::{VirtualQuery, VirtualAlloc, VirtualFree, VirtualQueryEx, VirtualAllocEx, VirtualFreeEx, MEM_RESERVE, MEM_COMMIT, MEM_RELEASE, PAGE_EXECUTE_READWRITE, MEMORY_BASIC_INFORMATION, MEM_FREE};
use windows::Win32::System::SystemInformation::{GetSystemInfo, SYSTEM_INFO};
use windows::Win32::System::Threading;
use windows::Win32::System::Threading::{OpenProcess, PROCESS_ALL_ACCESS};
use crate::internal::buffer_allocator::get_possible_buffer_addresses;
use crate::structs::internal::LocatorItem;
use crate::structs::params::BufferAllocatorSettings;
use crate::utilities::cached::CACHED;
use crate::utilities::mathematics::min;
use crate::utilities::wrappers::Unaligned;

// Abstractions for kernel32 functions //
pub trait Kernel32 {
    fn virtual_query(&self, lp_address: *const c_void, lp_buffer: &mut MEMORY_BASIC_INFORMATION) -> usize;
    fn virtual_alloc(&self, lp_address: *const c_void, dw_size: usize) -> *mut c_void;
    fn virtual_free(&self, lp_address: *mut c_void, dw_size: usize) -> bool;
}

pub struct LocalKernel32;

impl Kernel32 for LocalKernel32 {
    fn virtual_query(&self, lp_address: *const c_void, lp_buffer: &mut MEMORY_BASIC_INFORMATION) -> usize {
        unsafe { VirtualQuery(Some(lp_address), lp_buffer, std::mem::size_of::<MEMORY_BASIC_INFORMATION>()) }
    }

    fn virtual_alloc(&self, lp_address: *const c_void, dw_size: usize) -> *mut c_void {
        unsafe { VirtualAlloc(Some(lp_address), dw_size, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE) }
    }

    fn virtual_free(&self, lp_address: *mut c_void, dw_size: usize) -> bool {
        unsafe { VirtualFree(lp_address, dw_size, MEM_RELEASE).as_bool() }
    }
}

pub struct RemoteKernel32 {
    handle: HANDLE,
}

impl Kernel32 for RemoteKernel32 {
    fn virtual_query(&self, lp_address: *const c_void, lp_buffer: &mut MEMORY_BASIC_INFORMATION) -> usize {
        unsafe { VirtualQueryEx(self.handle, Some(lp_address), lp_buffer, std::mem::size_of::<MEMORY_BASIC_INFORMATION>()) }
    }

    fn virtual_alloc(&self, lp_address: *const c_void, dw_size: usize) -> *mut c_void {
        unsafe { VirtualAllocEx(self.handle, Some(lp_address), dw_size, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE) }
    }

    fn virtual_free(&self, lp_address: *mut c_void, dw_size: usize) -> bool {
        unsafe { VirtualFreeEx(self.handle, lp_address, dw_size, MEM_RELEASE).as_bool() }
    }
}

// Abstractions for Process //
pub(crate) struct ProcessHandle {
    handle: HANDLE,
}

impl ProcessHandle {
    // open_process opens a new process with the given id and returns a ProcessHandle.
    pub unsafe fn open_process(id: u32) -> Result<Self, windows::core::Error> {
        let handle = OpenProcess(PROCESS_ALL_ACCESS, false, id);
        
        match handle {
            Ok(x) => Ok(Self { handle: x }),
            Err(x) => Err(x)
        }
    }

    // get_handle returns the internal HANDLE.
    pub fn get_handle(&self) -> HANDLE {
        self.handle
    }
}

impl Drop for ProcessHandle {
    fn drop(&mut self) {
        unsafe {
            CloseHandle(self.handle);
        }
    }
}

// Helpers //

fn get_max_windows_address(process_id: u32, handle: HANDLE) -> usize {
    if CACHED.this_process_id == process_id {
        // Note: In WOW64 mode, the following rules apply:
        // - If current process is large address aware, this will return 0xFFFEFFFF.
        // - If it is not LAA, this should return 0x7FFEFFFF.
        return CACHED.max_address;
    }

    unsafe {
        // Is this Windows on Windows 64? (x86 app running on x64 Windows)
        let mut is_wow64: BOOL = Default::default();
        let has_is_wow64 = IsWow64Process(handle, &mut is_wow64).as_bool();
        
        let mut system_info: SYSTEM_INFO = Default::default();
        GetSystemInfo( &mut system_info);
        
        let mut max_address = 0x7FFFFFFF; // 32bit max
        
        // If target is not using WoW64 emulation layer, trust GetSystemInfo for max address.
        if !is_wow64.as_bool() && has_is_wow64 {
            max_address = system_info.lpMaximumApplicationAddress as usize;
        }

        max_address
    }
}

// Implementation //
pub fn allocate_windows(settings: &BufferAllocatorSettings) -> Result<LocatorItem, &'static str> {
    unsafe {
        let process_handle = ProcessHandle::open_process(settings.target_process_id);

        if process_handle.is_err() {
            return Err("Failed to open process");
        }
        
        let handle =  process_handle.unwrap_unchecked().handle;
        let max_address = get_max_windows_address(settings.target_process_id, handle);
        return if CACHED.get_this_process_id() == settings.target_process_id {
            allocate_fast(&LocalKernel32 {}, max_address, &settings)
        } else {
            allocate_fast(&RemoteKernel32 { handle, }, max_address, &settings)
        }
    }
}

fn allocate_fast<T: Kernel32>(
    k32: &T,
    mut max_address: usize,
    settings: &BufferAllocatorSettings,
) -> Result<LocatorItem, &'static str> {
    max_address = min(max_address, settings.max_address as usize);

    for _ in 0..settings.retry_count {

        // Until we get all of the pages.
        let mut current_address = settings.min_address as usize;
        let mut memory_information = MEMORY_BASIC_INFORMATION::default();
        while current_address <= max_address {
            // Get our info from VirtualQueryEx.
            let has_page = k32.virtual_query(
                current_address as *const c_void,
                &mut memory_information,
            );
            
            if has_page == 0 {
                break;
            }

            // Add the page and increment address iterator to go to next page.
            match try_allocate_buffer(k32, &mut memory_information, settings) {
                Some(item) => return Ok(item),
                None => {
                    current_address += memory_information.RegionSize as usize;
                }
            };
        }
    }

    // See remarks on 'BruteForce' in BufferAllocatorSettings, as for why this code exists.
    // I'm not particularly fond of it, but what can you do?
    if settings.brute_force {
        let mut current_address = settings.min_address as usize;
        let mut memory_information = MEMORY_BASIC_INFORMATION::default();
        while current_address <= max_address {
            let has_item = k32.virtual_query(
                current_address as *const c_void,
                &mut memory_information,
            );
            if has_item == 0 {
                break;
            }

            match try_allocate_buffer(k32, &mut memory_information, settings) {
                Some(item) => return Ok(item),
                None => {
                    current_address += CACHED.get_allocation_granularity() as usize;
                }
            };
        }
    }

    Err("Failed to allocate buffer")
}

fn try_allocate_buffer<T: Kernel32>(
    k32: &T,
    mut page_info: &mut MEMORY_BASIC_INFORMATION,
    settings: &BufferAllocatorSettings,
) -> Option<LocatorItem> {

    // Fast return if page is not free.
    if page_info.State != MEM_FREE {
        return None;
    }

    let mut results = [0 as usize; 4];
    for addr in get_buffer_pointers_in_page_range(
        &mut page_info,
        settings.size as usize,
        settings.min_address as usize,
        settings.max_address as usize,
        &mut results,
    ) {
        let allocated = k32.virtual_alloc(*addr as *const c_void, settings.size as usize);

        if allocated.is_null() {
            continue;
        }

        // Sanity test just in case.
        if allocated as usize != *addr {
            k32.virtual_free(allocated, settings.size as usize);
            continue;
        }
        
        return Some(LocatorItem {
            base_address: Unaligned(allocated as usize),
            size: settings.size,
            position: 0,
            is_taken: AtomicI32::from(0),
        });
    }
    
    None
}

fn get_buffer_pointers_in_page_range<'a>(
    page_info: &mut MEMORY_BASIC_INFORMATION,
    buffer_size: usize,
    minimum_ptr: usize,
    maximum_ptr: usize,
    results: &'a mut [usize; 4],
) -> &'a [usize] {
    
    let page_start = page_info.BaseAddress as usize;
    let page_end = page_info.BaseAddress as usize + page_info.RegionSize as usize;
    let allocation_granularity = CACHED.get_allocation_granularity();
    
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