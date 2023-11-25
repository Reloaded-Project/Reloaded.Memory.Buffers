use super::cached::CACHED;

// Generic structure to use for custom parsers.
#[derive(Debug)]
pub struct MemoryMapEntry {
    pub start_address: usize,
    pub end_address: usize,
}

/// This struct represents an entry in the memory map,
/// which is a region in the process's virtual memory space.
impl MemoryMapEntry {
    pub fn new(start_address: usize, end_address: usize) -> MemoryMapEntry {
        MemoryMapEntry {
            start_address,
            end_address,
        }
    }
}

// Trait to use for external types.
pub trait MemoryMapEntryTrait {
    fn start_address(&self) -> usize;
    fn end_address(&self) -> usize;
}

impl MemoryMapEntryTrait for MemoryMapEntry {
    fn start_address(&self) -> usize {
        self.start_address
    }

    fn end_address(&self) -> usize {
        self.end_address
    }
}

/// Returns all free regions based on the found regions.
///
/// # Arguments
///
/// * `regions` - A slice of MemoryMapEntry that contains the regions.
pub fn get_free_regions<T: MemoryMapEntryTrait>(regions: &[T]) -> Vec<MemoryMapEntry> {
    let mut last_end_address: usize = 0;
    let mut free_regions = Vec::with_capacity(regions.len() + 2); // +2 for start and finish

    for entry in regions.iter() {
        if entry.start_address() > last_end_address {
            free_regions.push(MemoryMapEntry {
                start_address: last_end_address,
                end_address: entry.start_address() - 1,
            });
        }

        last_end_address = entry.end_address();
    }

    // After the last region, up to the end of memory
    if last_end_address < CACHED.max_address {
        free_regions.push(MemoryMapEntry {
            start_address: last_end_address,
            end_address: CACHED.max_address,
        });
    }

    free_regions
}

#[cfg(test)]
mod tests {
    use crate::utilities::map_parser_utilities::{get_free_regions, MemoryMapEntry};

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
}
