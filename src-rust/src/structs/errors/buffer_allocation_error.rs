use crate::structs::params::BufferAllocatorSettings;

#[derive(Debug, Clone)]
pub struct BufferAllocationError {
    pub settings: BufferAllocatorSettings,
    pub text: &'static str,
}

impl std::fmt::Display for BufferAllocationError {
    #[cfg_attr(feature = "size_opt", optimize(size))]
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "Buffer allocation error: {}. Settings: {:?}",
            self.text, self.settings
        )
    }
}

impl std::error::Error for BufferAllocationError {}

impl BufferAllocationError {
    pub fn new(settings: BufferAllocatorSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
