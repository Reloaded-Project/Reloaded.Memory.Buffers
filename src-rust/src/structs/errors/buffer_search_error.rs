use core::fmt::{Display, Formatter};

use crate::structs::params::BufferSearchSettings;

#[derive(Debug, Clone)]
pub struct BufferSearchError {
    pub settings: BufferSearchSettings,
    pub text: &'static str,
}

impl Display for BufferSearchError {
    #[cfg_attr(feature = "size_opt", optimize(size))]
    fn fmt(&self, f: &mut Formatter<'_>) -> core::fmt::Result {
        write!(
            f,
            "Buffer search error: {}. Settings: {:?}",
            self.text, self.settings
        )
    }
}

impl BufferSearchError {
    pub fn new(settings: BufferSearchSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
