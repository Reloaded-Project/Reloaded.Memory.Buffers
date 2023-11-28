extern crate alloc;

use core::ffi::c_void;

use crate::internal::memory_mapped_file::MemoryMappedFile;
use alloc::ffi::CString;
use windows_sys::Win32::Foundation::{CloseHandle, HANDLE, INVALID_HANDLE_VALUE};
use windows_sys::Win32::Security::SECURITY_ATTRIBUTES;
use windows_sys::Win32::System::Memory::{
    CreateFileMappingA, MapViewOfFile, OpenFileMappingA, UnmapViewOfFile, FILE_MAP_ALL_ACCESS,
    MEMORY_MAPPED_VIEW_ADDRESS, PAGE_EXECUTE_READWRITE,
};

pub struct WindowsMemoryMappedFile {
    already_existed: bool,
    data: *mut u8,
    length: usize,
    map_handle: HANDLE,
}

impl WindowsMemoryMappedFile {
    pub fn new(name: &str, length: usize) -> WindowsMemoryMappedFile {
        let file_name = CString::new(name).unwrap();
        let mut already_existed = true;

        unsafe {
            let mut map_handle =
                OpenFileMappingA(FILE_MAP_ALL_ACCESS, 0, file_name.as_ptr() as *const u8);

            // No file existed, as open failed. Try create a new one.
            if map_handle == 0 {
                map_handle = CreateFileMappingA(
                    INVALID_HANDLE_VALUE,
                    core::ptr::null::<SECURITY_ATTRIBUTES>(),
                    PAGE_EXECUTE_READWRITE,
                    0,
                    length as u32,
                    file_name.as_ptr() as *const u8,
                );

                already_existed = false;
            }

            let data =
                MapViewOfFile(map_handle, FILE_MAP_ALL_ACCESS, 0, 0, length).Value as *mut u8;

            WindowsMemoryMappedFile {
                already_existed,
                data,
                length,
                map_handle,
            }
        }
    }
}

impl Drop for WindowsMemoryMappedFile {
    fn drop(&mut self) {
        unsafe {
            UnmapViewOfFile(MEMORY_MAPPED_VIEW_ADDRESS {
                Value: self.data as *mut c_void,
            });
            CloseHandle(self.map_handle);
        }
    }
}

impl MemoryMappedFile for WindowsMemoryMappedFile {
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
    use super::*;
    use crate::utilities::cached::get_sys_info;

    #[test]
    fn test_windows_memory_mapped_file_creation() {
        // Let's create a memory mapped file with a specific size.
        let file_name = format!(
            "/Reloaded.Memory.Buffers.MemoryBuffer.Test, PID {}",
            get_sys_info().this_process_id
        );
        let file_length = get_sys_info().allocation_granularity as usize;
        let mmf = WindowsMemoryMappedFile::new(&file_name, file_length);

        assert_eq!(mmf.already_existed, false);
        assert_eq!(mmf.length, file_length);

        // Assert the file can be opened again (i.e., it exists)
        let mmf_existing = WindowsMemoryMappedFile::new(&file_name, file_length);
        assert_eq!(mmf_existing.already_existed, true);
    }

    #[test]
    fn test_windows_memory_mapped_file_data() {
        let file_name = format!(
            "/Reloaded.Memory.Buffers.MemoryBuffer.Test, PID {}",
            get_sys_info().this_process_id
        );

        let file_length = get_sys_info().allocation_granularity as usize;
        let mmf = WindowsMemoryMappedFile::new(&file_name, file_length);

        // Let's test we can read and write to the data.
        unsafe {
            let data_ptr = mmf.data;
            assert_ne!(data_ptr, std::ptr::null_mut());

            // Write a value
            *data_ptr = 123;

            // Read it back
            assert_eq!(*data_ptr, 123);
        }
    }
}
