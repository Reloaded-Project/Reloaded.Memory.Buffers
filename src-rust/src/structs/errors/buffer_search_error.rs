use core::fmt::{Display, Formatter};

use crate::structs::params::BufferSearchSettings;

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
            let mut error_message = String::from("Buffer Search Error: ");
            error_message.push_str(self.text);
            error_message
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
            f.write_str("Buffer Search Error: ")?;
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
