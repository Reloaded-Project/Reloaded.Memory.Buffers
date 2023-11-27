use crate::structs::params::BufferAllocatorSettings;

#[derive(Debug, Clone)]
pub struct BufferAllocationError {
    pub settings: BufferAllocatorSettings,
    pub text: &'static str,
}

impl core::fmt::Display for BufferAllocationError {
    #[cfg_attr(feature = "size_opt", optimize(size))]
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        write!(
            f,
            "Buffer allocation error: {}. Settings: {:?}",
            self.text, self.settings
        )
    }
}

impl BufferAllocationError {
    pub fn new(settings: BufferAllocatorSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
