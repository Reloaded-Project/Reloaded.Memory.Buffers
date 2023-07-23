use crate::structs::params::BufferSearchSettings;

#[derive(Debug, Clone)]
pub struct BufferSearchError {
    pub settings: BufferSearchSettings,
    pub text: &'static str,
}

impl std::fmt::Display for BufferSearchError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "Buffer search error: {}. Settings: {:?}",
            self.text, self.settings
        )
    }
}

impl std::error::Error for BufferSearchError {}

impl BufferSearchError {
    pub fn new(settings: BufferSearchSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
