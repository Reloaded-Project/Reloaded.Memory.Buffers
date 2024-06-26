use crate::structs::params::BufferAllocatorSettings;

#[cfg(not(feature = "std"))]
use alloc::string::String;

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
            use nanokit::string_concat::concat_2;
            const BASE_MSG: &str = "Buffer Allocation Error: ";
            concat_2(BASE_MSG, self.text)
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
