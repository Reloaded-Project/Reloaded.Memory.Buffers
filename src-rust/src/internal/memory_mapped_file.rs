pub trait MemoryMappedFile {
    fn already_existed(&self) -> bool;
    unsafe fn data(&self) -> *mut u8;
    fn length(&self) -> usize;
}