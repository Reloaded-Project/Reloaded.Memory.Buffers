#[cfg(target_os = "windows")]
use windows_sys::Win32::System::{
    SystemInformation::{GetSystemInfo, SYSTEM_INFO},
    Threading::GetCurrentProcessId,
};

static mut CACHED: Option<Cached> = None;

pub fn get_sys_info() -> &'static Cached {
    // No thread safety needed here (we're running code with no side effects), so we omit lazy_static to save on library space.
    unsafe {
        if CACHED.is_some() {
            return unsafe { CACHED.as_ref().unwrap_unchecked() };
        }

        CACHED = Some(Cached::new());
        return unsafe { CACHED.as_ref().unwrap_unchecked() };
    }
}

pub struct Cached {
    pub max_address: usize,
    pub allocation_granularity: i32,
    pub page_size: u32,
    pub this_process_id: u32,
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
            this_process_id: Self::get_process_id(),
            page_size: 4096,
        }
    }

    #[cfg(unix)]
    fn get_process_id() -> u32 {
        unsafe { libc::getpid() as u32 }
    }

    #[cfg(target_os = "windows")]
    fn get_process_id() -> u32 {
        unsafe { GetCurrentProcessId() }
    }

    #[cfg(target_os = "windows")]
    fn get_address_and_allocation_granularity_windows(
        allocation_granularity: &mut i32,
        max_address: &mut usize,
        page_size: &mut i32,
    ) {
        use core::mem::zeroed;

        unsafe {
            let mut info: SYSTEM_INFO = zeroed();
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

        use core::cmp::max;

        use mmap_rs_with_map_from_existing::MmapOptions;
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

        *allocation_granularity = max(MmapOptions::allocation_granularity() as i32, *page_size);
    }
}
