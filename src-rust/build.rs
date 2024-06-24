fn main() {
    // This defines a fallback to mmap-rs if one of the explicit memory mapped file implementations
    // is not available.
    if cfg!(any(
        target_os = "macos",
        target_os = "windows",
        target_os = "linux"
    )) {
        println!("cargo:rustc-cfg=feature=\"direct-mmap\"");
    }
}
