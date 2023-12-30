extern crate alloc;

use alloc::ffi::CString;
use alloc::string::String;
use core::mem::MaybeUninit;
use core::ptr::null_mut;

#[cfg(not(feature = "no_format"))]
use errno::errno;

use libc::c_char;
use libc::mkdir;
use libc::stat;
use libc::S_IFDIR;
use libc::{
    c_int, c_void, close, ftruncate, mmap, munmap, open, MAP_SHARED, O_CREAT, O_RDWR, PROT_READ,
    PROT_WRITE, S_IRWXU,
};

#[cfg(target_os = "macos")]
use libc::c_uint;

use crate::internal::memory_mapped_file::MemoryMappedFile;

#[cfg(not(target_os = "android"))]
pub const BASE_DIR: &str = "/tmp/.reloaded/memory.buffers";

#[cfg(target_os = "android")] // needs storage permission, no idea if it will even allow it though
pub const BASE_DIR: &str = "/sdcard/.reloaded/memory.buffers";

pub struct UnixMemoryMappedFile {
    pub file_descriptor: i32,
    pub already_existed: bool,
    pub data: *mut u8,
    pub length: usize,
    pub file_path: String,
}

impl UnixMemoryMappedFile {
    pub fn new(name: &str, length: usize) -> UnixMemoryMappedFile {
        let mut new_name = String::with_capacity(BASE_DIR.len() + name.len());
        new_name.push_str(BASE_DIR);
        new_name.push_str(name);

        let file_name = CString::new(new_name.as_str()).expect("CString::new failed");

        let mut file_descriptor = unsafe { open(file_name.as_ptr(), O_RDWR) };
        let already_existed = file_descriptor != -1;

        // If it doesn't exist, create a new shared memory.
        if !already_existed {
            unsafe {
                Self::create_dir_all(BASE_DIR);
            }

            #[cfg(not(any(target_os = "macos", target_os = "ios")))]
            Self::open_unix(file_name, &mut file_descriptor);

            #[cfg(any(target_os = "macos", target_os = "ios"))]
            Self::open_macos(file_name, &mut file_descriptor);

            if file_descriptor == -1 {
                #[cfg(feature = "no_format")]
                panic!("Failed to open shared memory file.");

                #[cfg(not(feature = "no_format"))]
                {
                    panic!("Failed to open shared memory file, errno: {}", errno().0)
                }
            }
            unsafe { ftruncate(file_descriptor, length as _) };
        }

        let data = unsafe {
            mmap(
                null_mut::<c_void>(),
                length,
                PROT_READ | PROT_WRITE,
                MAP_SHARED,
                file_descriptor,
                0,
            )
        };

        if data == libc::MAP_FAILED {
            #[cfg(feature = "no_format")]
            panic!("Failed to mmap shared memory file.");

            #[cfg(not(feature = "no_format"))]
            {
                let err_no = errno().0;
                panic!("Failed to mmap shared memory file, error no: {}", err_no);
            }
        }

        UnixMemoryMappedFile {
            file_descriptor,
            already_existed,
            data: data as *mut u8,
            length,
            file_path: new_name,
        }
    }

    #[cfg(any(target_os = "macos", target_os = "ios"))]
    fn open_macos(file_name: CString, x: &mut c_int) {
        unsafe { *x = open(file_name.as_ptr(), O_RDWR | O_CREAT, S_IRWXU as c_uint) }
    }

    #[cfg(unix)]
    #[cfg(not(any(target_os = "macos", target_os = "ios")))]
    fn open_unix(file_name: CString, x: &mut c_int) {
        unsafe { *x = open(file_name.as_ptr(), O_RDWR | O_CREAT, S_IRWXU) }
    }

    unsafe fn create_dir_all(path: &str) {
        let mut current_path = String::with_capacity(path.len());
        current_path.push('/');
        for component in path.split('/') {
            if !component.is_empty() {
                current_path.push_str(component);

                // Convert current_path to C string
                let c_path = CString::new(current_path.as_str()).unwrap();

                // Properly handle MaybeUninit
                let mut stat_buf = MaybeUninit::uninit();
                let stat_result = stat(c_path.as_ptr(), stat_buf.as_mut_ptr());

                if stat_result != 0 {
                    // stat failed, directory does not exist, try to create it
                    if mkdir(c_path.as_ptr(), S_IRWXU) != 0 {
                        // Handle error or break as needed
                        break;
                    }
                } else {
                    // stat succeeded, ensure that the path is a directory
                    let stat_buf = stat_buf.assume_init();
                    if stat_buf.st_mode & S_IFDIR == 0 {
                        // Path exists but is not a directory
                        break;
                    }
                }

                current_path.push('/');
            }
        }
    }
}

// Implement Dispose
impl Drop for UnixMemoryMappedFile {
    fn drop(&mut self) {
        let _ = unsafe { munmap(self.data as *mut c_void, self.length) };
        unsafe { close(self.file_descriptor) };
        if !self.already_existed {
            unsafe {
                libc::unlink(self.file_path.as_ptr() as *const c_char);
            }
        }
    }
}

impl MemoryMappedFile for UnixMemoryMappedFile {
    fn already_existed(&self) -> bool {
        self.already_existed
    }
    unsafe fn data(&self) -> *mut u8 {
        self.data
    }
    fn length(&self) -> usize {
        self.length
    }
}

#[cfg(test)]
mod tests {

    #[cfg(not(target_os = "android"))]
    use {super::*, crate::utilities::cached::get_sys_info};

    #[test]
    #[cfg(not(target_os = "android"))]
    fn test_memory_mapped_file_creation() {
        // Let's create a memory mapped file with a specific size.
        let file_name = format!(
            "/Reloaded.Memory.Buffers.MemoryBuffer.Test, PID {}",
            get_sys_info().this_process_id
        );
        let file_length = get_sys_info().allocation_granularity as usize;
        let mmf = UnixMemoryMappedFile::new(&file_name, file_length);

        assert!(!mmf.already_existed);
        assert_eq!(mmf.length, file_length);

        // Assert the file can be opened again (i.e., it exists)
        let mmf_existing = UnixMemoryMappedFile::new(&file_name, file_length);
        assert!(mmf_existing.already_existed);
    }

    #[test]
    #[cfg(not(target_os = "android"))]
    fn test_memory_mapped_file_data() {
        let file_name = format!(
            "/test_memory_mapped_file_data PID {}",
            get_sys_info().this_process_id
        );

        let file_length = get_sys_info().allocation_granularity as usize;
        println!("file_length: {:?}", file_length);
        let mmf = UnixMemoryMappedFile::new(&file_name, file_length);

        // Let's test we can read and write to the data.
        unsafe {
            let data_ptr = mmf.data();
            assert_ne!(data_ptr, std::ptr::null_mut::<u8>());
            println!("data_ptr: {:?}", data_ptr);

            // Write a value
            *data_ptr = 123;

            // Read it back
            assert_eq!(*data_ptr, 123);
        }
    }
}
