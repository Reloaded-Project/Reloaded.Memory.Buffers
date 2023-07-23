#[derive(PartialEq)]
pub enum ItemAllocationError {
    NoSpaceInHeader,
    CannotAllocateMemory,
}

impl ItemAllocationError {
    pub fn as_string(&self) -> &'static str {
        match self {
            ItemAllocationError::NoSpaceInHeader => "No more space in locator header",
            ItemAllocationError::CannotAllocateMemory => "Could not allocate memory",
        }
    }
}
