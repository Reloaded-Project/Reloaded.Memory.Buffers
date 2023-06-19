pub mod structs {
    
    pub mod params {
        pub mod buffer_allocator_settings;
        pub use buffer_allocator_settings::BufferAllocatorSettings;
        
        pub mod buffer_search_settings;
        pub use buffer_search_settings::BufferSearchSettings;
    }
    
    pub mod safe_locator_item;
    pub use safe_locator_item::SafeLocatorItem;
    
    pub mod private_allocation;
    pub use private_allocation::PrivateAllocation;

    pub(crate) mod internal {
        pub mod locator_item;
        pub use locator_item::LocatorItem;

        pub mod locator_header;
        pub use locator_header::LocatorHeader;
    }
}

pub(crate) mod internal {
    pub mod buffer_allocator;
    pub mod locator_header_finder;
    
    #[cfg(target_os = "linux")]
    pub mod buffer_allocator_linux;

    #[cfg(target_os = "macos")]
    pub mod buffer_allocator_osx;
    
    #[cfg(target_os = "windows")]
    pub mod buffer_allocator_windows;
    
    pub mod memory_mapped_file;
    
    #[cfg(any(target_os = "linux", target_os = "macos"))]
    pub mod memory_mapped_file_unix;
    
    #[cfg(target_os = "windows")]
    pub mod memory_mapped_file_windows;
}

pub(crate) mod utilities {

    pub mod mathematics;
    pub mod wrappers;
    pub mod address_range;
    pub mod cached;

    #[cfg(target_os = "linux")]
    pub mod linux_map_parser;
}

pub mod buffers;