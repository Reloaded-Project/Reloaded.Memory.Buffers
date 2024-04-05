use crate::structs::params::BufferSearchSettings;
use core::fmt::{Display, Formatter};

#[cfg(not(feature = "std"))]
use alloc::string::String;

#[derive(Debug, Clone)]
pub struct BufferSearchError {
    pub settings: BufferSearchSettings,
    pub text: &'static str,
}

#[allow(clippy::inherent_to_string_shadow_display)]
impl BufferSearchError {
    pub fn to_string(&self) -> String {
        #[cfg(feature = "no_format")]
        {
            use nanokit::string_concat::concat_2;
            const BASE_MSG: &str = "Buffer Search Error: ";
            concat_2(BASE_MSG, self.text)
        }

        #[cfg(not(feature = "no_format"))]
        {
            format!(
                "Buffer Search Error: {}. Settings: {:?}",
                self.text, self.settings
            )
        }
    }
}

impl Display for BufferSearchError {
    #[cfg_attr(feature = "size_opt", optimize(size))]
    fn fmt(&self, f: &mut Formatter<'_>) -> core::fmt::Result {
        #[cfg(feature = "no_format")]
        {
            const BASE_MSG: &str = "Buffer Search Error: ";
            f.write_str(BASE_MSG)?;
            f.write_str(self.text)
        }

        #[cfg(not(feature = "no_format"))]
        {
            write!(
                f,
                "Buffer Search Error: {}. Settings: {:?}",
                self.text, self.settings
            )
        }
    }
}

impl BufferSearchError {
    pub fn new(settings: BufferSearchSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
