use lazy_static::lazy_static;
use std::process;
#[cfg(target_os = "windows")]
use windows::Win32::System::SystemInformation::{GetSystemInfo, SYSTEM_INFO};

lazy_static! {
    pub static ref CACHED: Cached = Cached::new();
}

pub struct Cached {
    pub max_address: usize,
    pub allocation_granularity: i32,
    pub this_process_id: u32,
    pub page_size: u32,
}

#[allow(dead_code)]
impl Cached {
    pub fn new() -> Cached {
        let mut allocation_granularity: i32 = Default::default();
        let mut page_size: i32 = Default::default();
        let mut max_address: usize = Default::default();

        #[cfg(target_os = "windows")]
        Self::get_address_and_allocation_granularity_windows(
            &mut allocation_granularity,
            &mut max_address,
            &mut page_size,
        );

        #[cfg(not(target_os = "windows"))]
        Self::get_address_and_allocation_granularity_mmap_rs(
            &mut allocation_granularity,
            &mut max_address,
            &mut page_size,
        );

        Cached {
            max_address,
            allocation_granularity,
            this_process_id: process::id(),
            page_size: 4096,
        }
    }

    #[cfg(target_os = "windows")]
    fn get_address_and_allocation_granularity_windows(
        allocation_granularity: &mut i32,
        max_address: &mut usize,
        page_size: &mut i32,
    ) {
        unsafe {
            let mut info: SYSTEM_INFO = Default::default();
            GetSystemInfo(&mut info);

            *max_address = info.lpMaximumApplicationAddress as usize;
            *allocation_granularity = info.dwAllocationGranularity as i32;
            *page_size = info.dwPageSize as i32;
        }
    }

    #[allow(overflowing_literals)]
    #[cfg(not(target_os = "windows"))]
    fn get_address_and_allocation_granularity_mmap_rs(
        allocation_granularity: &mut i32,
        max_address: &mut usize,
        page_size: &mut i32,
    ) {
        // Note: This is a fallback mechanism dependent on mmap-rs.

        use mmap_rs::MmapOptions;
        if cfg!(target_pointer_width = "32") {
            *max_address = 0xFFFF_FFFF;
        } else if cfg!(target_pointer_width = "64") {
            *max_address = 0x7FFFFFFFFFFF; // no max-address API, so restricted to Linux level
        }

        #[cfg(not(all(target_os = "macos", target_arch = "aarch64")))]
        {
            *page_size = MmapOptions::page_size() as i32;
        }

        #[cfg(all(target_os = "macos", target_arch = "aarch64"))]
        {
            // Apple lies about page size in libc on M1 says it's 4096 instead of 16384
            *page_size = MmapOptions::page_size() as i32;
        }

        *allocation_granularity =
            std::cmp::max(MmapOptions::allocation_granularity() as i32, *page_size);
    }

    pub fn get_allocation_granularity(&self) -> i32 {
        self.allocation_granularity
    }

    pub fn get_this_process_id(&self) -> u32 {
        self.this_process_id
    }
}
