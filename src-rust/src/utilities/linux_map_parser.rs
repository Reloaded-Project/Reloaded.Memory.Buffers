use std::error::Error;
use crate::utilities;

#[derive(Debug)]
pub struct MemoryMapEntry {
    pub(crate) start_address: usize,
    pub(crate) end_address: usize,
}

/// This struct represents an entry in the memory map, 
/// which is a region in the process's virtual memory space.
impl MemoryMapEntry {
    fn new(start_address: usize, end_address: usize) -> MemoryMapEntry {
        MemoryMapEntry {
            start_address,
            end_address,
        }
    }
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
fn parse_memory_map_from_process_id(process_id: i32) -> Result<Vec<MemoryMapEntry>, Box<dyn std::error::Error>> {
    // Construct the path to the maps file for the given process ID.
    let maps_path = format!("/proc/{}/maps", process_id);

    // Read all the lines from the file into a single string.
    let all_lines = std::fs::read_to_string(&maps_path)?;

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
fn parse_memory_map_from_lines(lines: Vec<&str>) -> Result<Vec<MemoryMapEntry>, Box<dyn std::error::Error>> {
    let mut entries = Vec::new();

    for line in lines {
        let result = parse_memory_map_entry(&line);
        match result {
            Ok(entry) => entries.push(entry),
            Err(_) => {}
        }
    }

    Ok(entries)
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
fn parse_memory_map_entry(line: &str) -> Result<MemoryMapEntry, Box<dyn std::error::Error>> {
    let parts: Vec<&str> = line.split_ascii_whitespace().collect();

    if let Some(address_range) = parts.get(0) {
        let addresses: Vec<&str> = address_range.split('-').collect();
        if addresses.len() == 2 {
            let start_address = usize::from_str_radix(addresses[0], 16)?;
            let end_address = usize::from_str_radix(addresses[1], 16)?;
            return Ok(MemoryMapEntry::new(start_address, end_address));
        }
    }

    Err("Invalid Memory Map Entry".into())
}

/// Returns all free regions based on the found regions.
///
/// # Arguments
///
/// * `regions` - A slice of MemoryMapEntry that contains the regions.
///
pub fn get_free_regions(regions: &[MemoryMapEntry]) -> Vec<MemoryMapEntry> {
    let mut last_end_address: usize = 0;
    let mut free_regions = Vec::with_capacity(regions.len() + 2); // +2 for start and finish

    for entry in regions.iter() {
        if entry.start_address > last_end_address {
            free_regions.push(MemoryMapEntry {
                start_address: last_end_address,
                end_address: entry.start_address - 1,
            });
        }

        last_end_address = entry.end_address;
    }

    // After the last region, up to the end of memory
    if last_end_address < utilities::cached::CACHED.max_address {
        free_regions.push(MemoryMapEntry {
            start_address: last_end_address,
            end_address: utilities::cached::CACHED.max_address,
        });
    }

    free_regions
}

/// Returns all free regions based on the found regions.
///
/// # Arguments
///
/// * `process_id` - ID of the process to get regions for.
pub fn get_free_regions_from_process_id(process_id: i32) -> Vec<MemoryMapEntry> {
    
    let regions = parse_memory_map_from_process_id(process_id).unwrap();
    return get_free_regions(&regions);
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn get_free_regions_with_no_gap() {
        let regions = vec![
            MemoryMapEntry::new(0, 10),
            MemoryMapEntry::new(10, 20),
            MemoryMapEntry::new(20, usize::MAX),
        ];
        let free_regions = get_free_regions(&regions);
        assert_eq!(free_regions.len(), 0);
    }

    #[test]
    fn get_free_regions_single_gap() {
        let regions = vec![
            MemoryMapEntry::new(0, 10),
            MemoryMapEntry::new(10, 20),
            MemoryMapEntry::new(30, usize::MAX),
        ];
        let free_regions = get_free_regions(&regions);
        assert_eq!(free_regions.len(), 1);
        assert_eq!(free_regions[0].start_address, 20);
        assert_eq!(free_regions[0].end_address, 29);
    }

    #[test]
    fn get_free_regions_multiple_gaps() {
        let regions = vec![
            MemoryMapEntry::new(0, 10),
            MemoryMapEntry::new(20, 30),
            MemoryMapEntry::new(40, usize::MAX),
        ];
        let free_regions = get_free_regions(&regions);
        assert_eq!(free_regions.len(), 2);
        assert_eq!(free_regions[0].start_address, 10);
        assert_eq!(free_regions[0].end_address, 19);
        assert_eq!(free_regions[1].start_address, 30);
        assert_eq!(free_regions[1].end_address, 39);
    }

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
    #[should_panic(expected = "Invalid Memory Map Entry")]
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
        let result = parse_memory_map_from_lines(lines).unwrap();
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
            "9c89C00-9c89E00 r--p 00000000 08:01 3932178                    /path/to/file"
        ];
        let result = parse_memory_map_from_lines(lines).unwrap();
        assert_eq!(result.len(), 2);
        assert_eq!(result[0].start_address, 0x9c89900);
        assert_eq!(result[0].end_address, 0x9c89C00);
        assert_eq!(result[1].start_address, 0x9c89C00);
        assert_eq!(result[1].end_address, 0x9c89E00);
    }
}
