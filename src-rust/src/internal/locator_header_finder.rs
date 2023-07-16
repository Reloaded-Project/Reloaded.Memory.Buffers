use crate::internal::memory_mapped_file::MemoryMappedFile;
use crate::structs::internal::LocatorHeader;
use crate::utilities::cached::CACHED;
use std::ptr::null_mut;

#[cfg(any(target_os = "linux", target_os = "macos"))]
use crate::internal::memory_mapped_file_unix::UnixMemoryMappedFile;

#[cfg(any(target_os = "linux", target_os = "macos"))]
use std::fs;

#[cfg(any(target_os = "linux", target_os = "macos"))]
use std::path::Path;

#[cfg(any(target_os = "linux", target_os = "macos"))]
use libc::kill;

#[cfg(any(target_os = "linux", target_os = "macos"))]
use super::memory_mapped_file_unix::BASE_DIR;

#[cfg(target_os = "windows")]
use crate::internal::memory_mapped_file_windows::WindowsMemoryMappedFile;

pub struct LocatorHeaderFinder {}

static mut LOCATOR_HEADER_ADDRESS: *mut LocatorHeader = null_mut();
static mut MMF: Option<Box<dyn MemoryMappedFile>> = None;

/// The reason the variable was last found.
#[cfg(test)]
pub(crate) static mut LAST_FIND_REASON: FindReason = FindReason::Cached;

impl LocatorHeaderFinder {
    pub unsafe fn find() -> *mut LocatorHeader {
        #[cfg(test)]
        LocatorHeaderFinder::set_last_find_reason(FindReason::Cached);

        // If in cache, return the cached address.
        if !LOCATOR_HEADER_ADDRESS.is_null() {
            return LOCATOR_HEADER_ADDRESS;
        }

        let mmf = LocatorHeaderFinder::open_or_create_memory_mapped_file();

        // If the MMF previously existed, we need to read the real address from
        // the header, then close our mapping.
        if mmf.already_existed() {
            let header_addr = (*mmf).data();
            LOCATOR_HEADER_ADDRESS = (header_addr) as *mut LocatorHeader;

            #[cfg(test)]
            LocatorHeaderFinder::set_last_find_reason(FindReason::PreviouslyExisted);
            return unsafe { LOCATOR_HEADER_ADDRESS };
        }

        // Otherwise, we got a new MMF going, keep it alive forever.

        #[cfg(any(target_os = "linux", target_os = "macos"))]
        LocatorHeaderFinder::cleanup();

        LOCATOR_HEADER_ADDRESS = mmf.data().cast();
        (*LOCATOR_HEADER_ADDRESS).initialize(mmf.length());
        MMF = Some(mmf);

        #[cfg(test)]
        LocatorHeaderFinder::set_last_find_reason(FindReason::Created);

        LOCATOR_HEADER_ADDRESS
    }

    fn open_or_create_memory_mapped_file() -> Box<dyn MemoryMappedFile> {
        let name = format!(
            "/Reloaded.Memory.Buffers.MemoryBuffer, PID {}",
            CACHED.this_process_id
        );

        #[cfg(target_os = "windows")]
        return Box::new(WindowsMemoryMappedFile::new(
            &name,
            CACHED.allocation_granularity as usize,
        ));

        #[cfg(any(target_os = "linux", target_os = "macos"))]
        return Box::new(UnixMemoryMappedFile::new(
            &name,
            CACHED.allocation_granularity as usize,
        ));

        #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
        panic!("This platform is not supported! Only Windows/Linux/macOS are supported.");
    }

    #[cfg(test)]
    unsafe fn reset() {
        LOCATOR_HEADER_ADDRESS = null_mut();
        MMF = None;
    }

    #[cfg(test)]
    unsafe fn set_last_find_reason(reason: FindReason) {
        LAST_FIND_REASON = reason;
    }

    #[cfg(any(target_os = "linux", target_os = "macos"))]
    fn cleanup() {
        LocatorHeaderFinder::cleanup_posix(BASE_DIR.as_str(), |path| {
            if let Err(err) = fs::remove_file(path) {
                eprintln!("Failed to delete file {}: {}", path.display(), err);
            }
        });
    }

    #[cfg(any(target_os = "linux", target_os = "macos"))]
    fn cleanup_posix<T>(mmf_directory: &str, mut delete_file: T)
    where
        T: FnMut(&Path),
    {
        const MEMORY_MAPPED_FILE_PREFIX: &str = "Reloaded.Memory.Buffers.MemoryBuffer, PID ";

        let dir_entries = fs::read_dir(mmf_directory);
        if dir_entries.is_err() {
            return;
        }

        for entry in dir_entries.unwrap().flatten() {
            let entry_file_name = entry.file_name();
            let file_name = entry_file_name.to_str().unwrap();
            if !file_name.starts_with(MEMORY_MAPPED_FILE_PREFIX) {
                continue;
            }

            // Extract PID from the file name
            if let Some(pid_str) = file_name.strip_prefix(MEMORY_MAPPED_FILE_PREFIX) {
                if let Ok(pid) = pid_str.parse::<i32>() {
                    // Kill the file if needed.
                    if !LocatorHeaderFinder::is_process_running(pid) {
                        delete_file(entry.path().as_ref());
                    }
                }
            }
        }
    }

    #[cfg(any(target_os = "linux", target_os = "macos"))]
    fn is_process_running(pid: i32) -> bool {
        unsafe { kill(pid, 0) != 0 }
    }
}

#[cfg(test)]
#[derive(Debug, PartialEq, Copy, Clone)]
pub(crate) enum FindReason {
    Cached,
    PreviouslyExisted,
    Created,
}

#[cfg(test)]
mod tests {
    use super::FindReason;
    use super::LocatorHeaderFinder;
    use crate::internal::locator_header_finder::LAST_FIND_REASON;
    use crate::structs::internal::locator_header::LENGTH_OF_PREALLOCATED_CHUNKS;
    use crate::structs::internal::LocatorHeader;
    use crate::utilities::cached::CACHED;

    #[test]
    fn find_should_return_address_when_previously_exists() {
        unsafe {
            LocatorHeaderFinder::reset();
            let _map = LocatorHeaderFinder::open_or_create_memory_mapped_file();

            let address = LocatorHeaderFinder::find();
            assert!(!address.is_null());

            assert_eq!(LAST_FIND_REASON, FindReason::PreviouslyExisted);
        }
    }

    #[test]
    fn find_should_return_address_when_created() {
        unsafe {
            LocatorHeaderFinder::reset();

            let address = LocatorHeaderFinder::find();
            assert!(!address.is_null());

            assert_eq!(LAST_FIND_REASON, FindReason::Created);
        }
    }

    #[test]
    fn find_should_return_cached_address_when_called_twice() {
        unsafe {
            LocatorHeaderFinder::reset();

            let first_address = LocatorHeaderFinder::find();
            let first_reason = LAST_FIND_REASON;

            let second_address = LocatorHeaderFinder::find();
            let second_reason = LAST_FIND_REASON;

            assert!(!first_address.is_null());
            assert_eq!(first_reason, FindReason::Created);

            assert!(!second_address.is_null());
            assert_eq!(second_reason, FindReason::Cached);

            assert_eq!(first_address, second_address);
        }
    }

    // This is a placeholder test to test initialization. It's not a 1:1
    // translation of the C# test because Rust memory management is
    // different, so you'll need to adjust it to match your requirements.
    #[test]
    fn find_should_initialize_correctly_when_created() {
        unsafe {
            LocatorHeaderFinder::reset();

            let address = LocatorHeaderFinder::find();
            assert!(!address.is_null());

            let header = &*address;
            let expected_num_items = ((CACHED.allocation_granularity
                - std::mem::size_of::<LocatorHeader>() as i32)
                as f64
                / LENGTH_OF_PREALLOCATED_CHUNKS as f64)
                .round() as u8;

            assert_eq!(header.num_items, expected_num_items);

            for i in 0..header.num_items {
                let item = header.get_item(i as usize);
                assert_eq!((*item).position, 0);

                let base_address = (*item).base_address.0;
                assert_ne!(0, base_address);
                assert!((*item).size > 0);
            }
        }
    }
}