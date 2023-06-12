use std::process;
use lazy_static::lazy_static;
#[cfg(target_os = "windows")]
use windows::Win32::System::SystemInformation::{GetSystemInfo, SYSTEM_INFO};

#[cfg(not(target_os = "windows"))]
use libc;

#[cfg(not(target_os = "windows"))]
use std::mem::size_of;

#[cfg(target_os = "linux")]
const SC_PAGESIZE: i32 = 30; // from `man 3 sysconf`

#[cfg(target_os = "macos")]
const SC_PAGESIZE: i32 = 29; // from `man 3 sysconf`

lazy_static! {
    pub static ref CACHED: Cached = Cached::new();
}

pub struct Cached {
    pub max_address: usize,
    pub allocation_granularity: i32,
    pub this_process_id: u32,
}

impl Cached {
    pub fn new() -> Cached {
        let mut allocation_granularity: i32 = Default::default();
        let mut max_address: usize = Default::default();
        
        #[cfg(target_os = "windows")]
        Self::get_address_and_allocation_granularity_windows(&mut allocation_granularity, &mut max_address);

        #[cfg(any(target_os = "linux", target_os = "macos"))]
        Self::get_address_and_allocation_granularity_posix(&mut allocation_granularity, &mut max_address);

        #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
        panic!("Platform not supported");

        Cached {
            max_address,
            allocation_granularity,
            this_process_id: process::id(),
        }
    }

    #[cfg(target_os = "windows")]
    fn get_address_and_allocation_granularity_windows(allocation_granularity: &mut i32, max_address: &mut usize) {
        unsafe {
            let mut info: SYSTEM_INFO = Default::default();
            GetSystemInfo(&mut info);

            *max_address = info.lpMaximumApplicationAddress as usize;
            *allocation_granularity = info.dwAllocationGranularity as i32;
        }
    }

    #[cfg(any(target_os = "linux", target_os = "macos"))]
    fn get_address_and_allocation_granularity_posix(allocation_granularity: &mut i32, max_address: &mut usize) {
        // Note: On POSIX, applications are aware of full address space by default.
        // Technically a chunk of address space is reserved for kernel, however for our use case that's not a concern.
        // Note 2: There is no API on Linux (or OSX) to get max address; so we'll restrict to signed 48-bits on x64 for now.

        *max_address = if size_of::<usize>() == 4 {
            0xFFFF_FFFF
        } else {
            0x7FFFFFFFFFFF
        };

        *allocation_granularity = unsafe {
            libc::sysconf(SC_PAGESIZE) as i32
        };
    }

    pub fn get_allocation_granularity(&self) -> i32 {
        self.allocation_granularity
    }

    pub fn get_this_process_id(&self) -> u32 {
        self.this_process_id
    }
}
