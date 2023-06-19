#[repr(packed(1))]
#[derive(Copy, Clone)]
pub struct Unaligned<T>(pub T);
