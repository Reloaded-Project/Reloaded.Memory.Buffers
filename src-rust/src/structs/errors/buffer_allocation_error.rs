use crate::structs::params::BufferAllocatorSettings;

#[derive(Debug, Clone)]
pub struct BufferAllocationError {
    
    pub settings: BufferAllocatorSettings,
    pub text: &'static str
}

impl std::fmt::Display for BufferAllocationError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "Buffer allocation error: {}. Settings: {:?}", self.text, self.settings)
    }
}

impl std::error::Error for BufferAllocationError {

}

impl BufferAllocationError {
    pub fn new(settings: BufferAllocatorSettings, text: &'static str) -> Self {
        Self {
            settings,
            text
        }
    }
}