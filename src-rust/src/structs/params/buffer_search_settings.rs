use crate::utilities::{cached::get_sys_info, mathematics};

/// Settings to pass to buffer search mechanisms.
#[derive(Debug, Clone, Copy)]
#[repr(C)]
pub struct BufferSearchSettings {
    /// Minimum address of the allocation.
    pub min_address: usize,

    /// Maximum address of the allocation.
    pub max_address: usize,

    /// Required size of the data.
    pub size: u32,
}

impl BufferSearchSettings {
    /// Initializes the buffer allocator with default settings.
    pub fn new() -> Self {
        Self {
            min_address: 0,
            max_address: get_sys_info().max_address,
            size: 4096,
        }
    }

    /// Creates settings such that the returned buffer will always be within `proximity` bytes of `target`.
    ///
    /// # Arguments
    ///
    /// * `proximity` - Max proximity (number of bytes) to target.
    /// * `target` - Target address.
    /// * `size` - Size required in the settings.
    ///
    /// # Returns
    ///
    /// * `BufferSearchSettings` - Settings that would satisfy this search.
    pub fn from_proximity(proximity: usize, target: usize, size: usize) -> Self {
        Self {
            max_address: mathematics::add_with_overflow_cap(target, proximity),
            min_address: mathematics::subtract_with_underflow_cap(target, proximity),
            size: size as u32,
        }
    }
}

impl Default for BufferSearchSettings {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_default_settings() {
        let settings = BufferSearchSettings::new();
        assert_eq!(settings.min_address, 0);
        assert_eq!(settings.max_address, get_sys_info().max_address);
        assert_eq!(settings.size, 4096);
    }

    #[test]
    fn test_from_proximity() {
        let proximity: usize = 1000;
        let target: usize = 2000;
        let size: usize = 3000;
        let settings = BufferSearchSettings::from_proximity(proximity, target, size);

        assert_eq!(
            settings.max_address,
            mathematics::add_with_overflow_cap(target, proximity)
        );
        assert_eq!(
            settings.min_address,
            mathematics::subtract_with_underflow_cap(target, proximity)
        );
        assert_eq!(settings.size, size as u32);
    }
}
