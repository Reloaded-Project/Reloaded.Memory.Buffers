use crate::utilities::cached::CACHED;
use crate::utilities::mathematics;
use std::cmp;

/// Settings to pass to the buffer allocator.
#[derive(Debug, Clone, Copy)]
#[repr(C)]
pub struct BufferAllocatorSettings {
    /// Minimum address of the allocation.
    pub min_address: usize,

    /// Maximum address of the allocation.
    pub max_address: usize,

    /// Required size of the data.
    pub size: u32,

    /// Process to allocate memory in.
    /// Stored as process id.
    pub target_process_id: u32,

    /// Amount of times library should retry after failing to allocate a region.
    ///
    /// # Remarks
    /// This is useful when there's high memory pressure, meaning pages become unavailable between the time
    /// they are found and the time we try to allocate them.
    pub retry_count: i32,

    /// Whether to use brute force to find a suitable address.
    ///
    /// # Remarks
    ///
    /// In the original library, this was for some reason only ever needed in FFXIV under Wine; and was contributed
    /// (prior to rewrite) by the Dalamud folks. In Wine and on FFXIV *only*; this was ever the case.
    /// Inclusion of a brute force approach is a last ditch workaround for that.
    ///
    /// This setting is only used on Windows targets today.
    pub brute_force: bool,
}

impl BufferAllocatorSettings {
    /// Initializes the buffer allocator with default settings.
    pub fn new() -> Self {
        Self {
            min_address: 0,
            max_address: CACHED.max_address,
            size: 4096,
            target_process_id: CACHED.this_process_id,
            retry_count: 8,
            brute_force: true,
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
    /// * `BufferAllocatorSettings` - Settings that would satisfy this search.
    pub fn from_proximity(proximity: usize, target: usize, size: usize) -> Self {
        Self {
            max_address: mathematics::add_with_overflow_cap(target, proximity),
            min_address: mathematics::subtract_with_underflow_cap(target, proximity),
            size: size as u32,
            ..Self::new()
        }
    }

    /// Sanitizes the input values.
    pub fn sanitize(&mut self) {
        // On Windows, VirtualAlloc treats 0 as 'any address', we might aswell avoid this out the gate.
        if cfg!(windows) && (self.min_address < CACHED.get_allocation_granularity() as usize) {
            self.min_address = CACHED.get_allocation_granularity() as usize;
        }

        self.size = cmp::max(self.size, 1);
        self.size = mathematics::round_up(
            self.size as usize,
            CACHED.get_allocation_granularity() as usize,
        ) as u32;
    }
}

impl Default for BufferAllocatorSettings {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {

    use super::*;

    #[test]
    fn test_default_settings() {
        let settings = BufferAllocatorSettings::new();
        assert_eq!(settings.min_address, 0);
        assert_eq!(settings.max_address, CACHED.max_address);
        assert_eq!(settings.size, 4096);
        assert_eq!(settings.target_process_id, CACHED.this_process_id);
        assert_eq!(settings.retry_count, 8);
        assert!(settings.brute_force);
    }

    #[test]
    fn test_from_proximity() {
        let proximity: usize = 1000;
        let target: usize = 2000;
        let size: usize = 3000;
        let settings = BufferAllocatorSettings::from_proximity(proximity, target, size);

        assert_eq!(
            settings.max_address,
            mathematics::add_with_overflow_cap(target, proximity)
        );
        assert_eq!(
            settings.min_address,
            mathematics::subtract_with_underflow_cap(target, proximity)
        );
        assert_eq!(settings.size, size as u32);
        assert_eq!(settings.target_process_id, CACHED.this_process_id);
        assert_eq!(settings.retry_count, 8);
        assert!(settings.brute_force);
    }

    #[test]
    fn test_sanitize() {
        let mut settings = BufferAllocatorSettings::new();
        settings.min_address = 0;
        settings.size = 1;

        settings.sanitize();

        if cfg!(windows) {
            assert_eq!(
                settings.min_address,
                CACHED.get_allocation_granularity() as usize
            );
        } else {
            assert_eq!(settings.min_address, 0);
        }

        assert_eq!(
            settings.size,
            mathematics::round_up(
                cmp::max(1, 1) as usize,
                CACHED.get_allocation_granularity() as usize
            ) as u32
        );
    }
}
