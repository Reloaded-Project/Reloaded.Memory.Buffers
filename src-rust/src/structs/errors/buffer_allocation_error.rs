use crate::structs::params::BufferAllocatorSettings;

#[derive(Debug, Clone)]
pub struct BufferAllocationError {
    pub settings: BufferAllocatorSettings,
    pub text: &'static str,
}

#[allow(clippy::inherent_to_string_shadow_display)]
impl BufferAllocationError {
    pub fn to_string(&self) -> String {
        // We save some space here for C binding use.
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
                "Buffer Allocation Error: {}. Settings: {:?}",
                self.text, self.settings
            )
        }
    }
}

impl core::fmt::Display for BufferAllocationError {
    #[cfg_attr(feature = "size_opt", optimize(size))]
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        #[cfg(feature = "no_format")]
        {
            f.write_str("Buffer Allocation Error: ")?;
            f.write_str(self.text)
        }

        #[cfg(not(feature = "no_format"))]
        {
            write!(
                f,
                "Buffer Allocation Error: {}. Settings: {:?}",
                self.text, self.settings
            )
        }
    }
}

impl BufferAllocationError {
    pub fn new(settings: BufferAllocatorSettings, text: &'static str) -> Self {
        Self { settings, text }
    }
}
