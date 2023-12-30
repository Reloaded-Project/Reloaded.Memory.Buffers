use crate::structs::params::BufferSearchSettings;
use core::fmt::{Display, Formatter};

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
            const BASE_MSG: &str = "Buffer Search Error: ";
            let total_length = BASE_MSG.len() + self.text.len();
            let mut error_message = String::with_capacity(total_length);

            unsafe {
                let vec = error_message.as_mut_vec();

                // SAFETY: Ensure that the vector has enough capacity
                vec.set_len(BASE_MSG.len() + self.text.len());

                // SAFETY: Manually copy elements
                core::ptr::copy_nonoverlapping(BASE_MSG.as_ptr(), vec.as_mut_ptr(), BASE_MSG.len());
                core::ptr::copy_nonoverlapping(
                    self.text.as_ptr(),
                    vec.as_mut_ptr().add(BASE_MSG.len()),
                    self.text.len(),
                );
            }

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
