#[repr(C, packed)]
#[derive(Copy, Clone)]
pub struct Unaligned<T> {
    pub value: T,
}

impl<T> Unaligned<T> {
    pub fn new(value: T) -> Self {
        Self { value }
    }
}
