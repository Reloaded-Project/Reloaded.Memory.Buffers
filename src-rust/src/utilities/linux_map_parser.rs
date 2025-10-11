extern crate alloc;
use super::map_parser_utilities::{get_free_regions, MemoryMapEntry};
use alloc::ffi::CString;
use libc::c_void;
use libc::close;
use libc::open;
use libc::read;
use libc::O_RDONLY;

#[cfg(not(feature = "std"))]
use alloc::string::String;

#[cfg(not(feature = "std"))]
use alloc::vec::Vec;

/// Parses the contents of the /proc/{id}/maps file and returns a vector of memory mapping entries.
///
/// # Arguments
///
/// * `process_id` - The process id to get mapping ranges for.
///
/// # Returns
///
/// This function returns a result which is:
///
/// * a vector of memory mapping entries if the function succeeds,
/// * an error if the function fails.
fn parse_memory_map_from_process_id(process_id: i32) -> Vec<MemoryMapEntry> {
    // Construct the path to the maps file for the given process ID.
    // no std!!
    let mut maps_path = String::from("/proc/");
    let mut buffer = itoa::Buffer::new();
    maps_path.push_str(buffer.format(process_id));
    maps_path.push_str("/maps");

    // Read all the lines from the file into a single string.
    let all_lines = unsafe { read_to_string(&maps_path) };

    // Split the string into lines.
    let lines: Vec<&str> = all_lines.split('\n').collect();
    parse_memory_map_from_lines(lines)
}

/// Parses the contents of the /proc/{id}/maps file and returns a vector of memory mapping entries.
///
/// # Arguments
///
/// * `process_id` - The process id to get mapping ranges for.
///
/// # Returns
///
/// This function returns a result which is:
///
/// * a vector of memory mapping entries if the function succeeds,
/// * an error if the function fails.
fn parse_memory_map_from_lines(lines: Vec<&str>) -> Vec<MemoryMapEntry> {
    let mut entries = Vec::new();

    for line in lines {
        if let Some(entry) = parse_memory_map_entry(line) {
            entries.push(entry)
        }
    }

    entries
}

/// Parses a line from the /proc/self/maps file (or equivalent) and returns a memory mapping entry.
///
/// # Arguments
///
/// * `line` - A line from a memory maps file.
///
/// # Returns
///
/// This function returns a result which is:
///
/// * a memory map entry if the function succeeds,
/// * an error if the function fails.
fn parse_memory_map_entry(line: &str) -> Option<MemoryMapEntry> {
    let parts: Vec<&str> = line.split_ascii_whitespace().collect();

    if let Some(address_range) = parts.first() {
        let addresses: Vec<&str> = address_range.split('-').collect();
        if addresses.len() == 2 {
            let start_address = usize::from_str_radix(addresses[0], 16);
            let end_address = usize::from_str_radix(addresses[1], 16);

            if start_address.is_err() || end_address.is_err() {
                return None;
            }

            unsafe {
                return Some(MemoryMapEntry::new(
                    start_address.unwrap_unchecked(),
                    end_address.unwrap_unchecked(),
                ));
            }
        }
    }

    None
}

/// Returns all free regions based on the found regions.
///
/// # Arguments
///
/// * `process_id` - ID of the process to get regions for.
pub fn get_free_regions_from_process_id(process_id: i32) -> Vec<MemoryMapEntry> {
    let regions = parse_memory_map_from_process_id(process_id);
    get_free_regions(&regions)
}

#[allow(clippy::comparison_chain)]
unsafe fn read_to_string(path: &str) -> String {
    const BUFFER_SIZE: usize = 131_072; // 128 KB
    let c_path = CString::new(path).unwrap();
    let fd = open(c_path.as_ptr(), O_RDONLY);
    if fd < 0 {
        panic!("Can't read map file! Your Linux system is weird.");
    }

    let mut content = String::with_capacity(BUFFER_SIZE);

    loop {
        let current_len = content.len();
        let remaining_capacity = content.capacity() - current_len;
        content.reserve(BUFFER_SIZE);

        let buffer_ptr = content.as_mut_vec().as_mut_ptr().add(current_len) as *mut c_void;
        let bytes_read = read(fd, buffer_ptr, remaining_capacity);
        if bytes_read < 0 {
            // Error occurred
            close(fd);
            panic!("Map read error");
        } else if bytes_read == 0 {
            // End of file
            break;
        } else {
            content
                .as_mut_vec()
                .set_len(current_len + bytes_read as usize);
        }
    }

    close(fd);
    content
}

#[cfg(test)]
mod tests {
    use super::*;

    #[cfg(target_pointer_width = "64")]
    #[test]
    fn parse_memory_map_entry_valid_line() {
        let line = "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file";
        let result = parse_memory_map_entry(line).unwrap();
        assert_eq!(result.start_address, 0x7f9c89991000);
        assert_eq!(result.end_address, 0x7f9c89993000);
    }

    #[cfg(target_pointer_width = "32")]
    #[test]
    fn parse_memory_map_entry_valid_line_32() {
        let line = "9c89900-9c89c00 r--p 00000000 08:01 3932177                    /path/to/file";
        let result = parse_memory_map_entry(line).unwrap();
        assert_eq!(result.start_address, 0x9c89900);
        assert_eq!(result.end_address, 0x9c89c00);
    }

    #[test]
    #[should_panic]
    fn parse_memory_map_entry_invalid_line() {
        let line = "Invalid line";
        let _ = parse_memory_map_entry(line).unwrap();
    }

    #[cfg(target_pointer_width = "64")]
    #[test]
    fn parse_memory_map_valid_lines() {
        let lines = vec![
            "7f9c89991000-7f9c89993000 r--p 00000000 08:01 3932177                    /path/to/file",
            "7f9c89994000-7f9c89995000 r--p 00000000 08:01 3932178                    /path/to/file"
        ];
        let result = parse_memory_map_from_lines(lines);
        assert_eq!(result.len(), 2);
        assert_eq!(result[0].start_address, 0x7f9c89991000);
        assert_eq!(result[0].end_address, 0x7f9c89993000);
        assert_eq!(result[1].start_address, 0x7f9c89994000);
        assert_eq!(result[1].end_address, 0x7f9c89995000);
    }

    #[cfg(target_pointer_width = "32")]
    #[test]
    fn parse_memory_map_valid_lines_32() {
        let lines = vec![
            "9c89900-9c89C00 r--p 00000000 08:01 3932177                    /path/to/file",
            "9c89C00-9c89E00 r--p 00000000 08:01 3932178                    /path/to/file",
        ];
        let result = parse_memory_map_from_lines(lines);
        assert_eq!(result.len(), 2);
        assert_eq!(result[0].start_address, 0x9c89900);
        assert_eq!(result[0].end_address, 0x9c89C00);
        assert_eq!(result[1].start_address, 0x9c89C00);
        assert_eq!(result[1].end_address, 0x9c89E00);
    }
}
