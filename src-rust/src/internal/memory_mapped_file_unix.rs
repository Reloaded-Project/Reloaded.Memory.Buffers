use errno::errno;

use libc::{
    c_int, close, ftruncate, mmap, munmap, open, MAP_SHARED, O_CREAT, O_RDWR, PROT_EXEC, PROT_READ,
    PROT_WRITE, S_IRWXU,
};
use std::ffi::{c_void, CString};
use std::path::Path;

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

        let file_name = CString::new(new_name.to_string()).expect("CString::new failed");

        let mut file_descriptor = unsafe { open(file_name.as_ptr(), O_RDWR) };
        let already_existed = file_descriptor != -1;

        // If it doesn't exist, create a new shared memory.
        if !already_existed {
            let dir = Path::new(new_name.as_str()).parent().unwrap();
            std::fs::create_dir_all(dir).unwrap();

            #[cfg(not(any(target_os = "macos", target_os = "ios")))]
            Self::open_unix(file_name, &mut file_descriptor);

            #[cfg(any(target_os = "macos", target_os = "ios"))]
            Self::open_macos(file_name, &mut file_descriptor);

            if file_descriptor == -1 {
                assert_ne!(
                    file_descriptor,
                    -1,
                    "Failed to open shared memory file, errno: {}",
                    errno().0
                );
            }
            unsafe { ftruncate(file_descriptor, length as _) };
        }

        let data = unsafe {
            mmap(
                std::ptr::null_mut::<c_void>(),
                length,
                PROT_READ | PROT_WRITE | PROT_EXEC,
                MAP_SHARED,
                file_descriptor,
                0,
            )
        };

        if data == libc::MAP_FAILED {
            let err_no = errno().0;
            panic!("Failed to mmap shared memory file, error no: {}", err_no);
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
}

// Implement Dispose
impl Drop for UnixMemoryMappedFile {
    fn drop(&mut self) {
        let _ = unsafe { munmap(self.data as *mut c_void, self.length) };
        unsafe { close(self.file_descriptor) };
        if !self.already_existed {
            let _ = std::fs::remove_file(Path::new(&self.file_path));
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
    use {super::*, crate::utilities::cached::CACHED};

    #[test]
    #[cfg(not(target_os = "android"))]
    fn test_memory_mapped_file_creation() {
        // Let's create a memory mapped file with a specific size.
        let file_name = format!(
            "/Reloaded.Memory.Buffers.MemoryBuffer.Test, PID {}",
            CACHED.this_process_id
        );
        let file_length = CACHED.get_allocation_granularity() as usize;
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
            CACHED.this_process_id
        );

        let file_length = CACHED.get_allocation_granularity() as usize;
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
