/*!
# The Reloaded Buffers Library

**Allocate Memory, & Knuckles**

- ![Coverage](https://codecov.io/gh/Reloaded-Project/Reloaded.Memory.Buffers/branch/master/graph/badge.svg)
- ![Crate](https://img.shields.io/crates/dv/reloaded_memory_buffers)
- ![Build Status](https://img.shields.io/github/actions/workflow/status/Reloaded-Project/Reloaded.Memory.Buffers/rust-cargo-test.yml)

## About

`Reloaded.Memory.Buffers` is a library for allocating memory between a given minimum and maximum memory address, for C# and Rust.

With the following properties:

- ***Memory Efficient***: No wasted memory.
- ***Shared***: Can be found and read/written to by multiple users.
- ***Static***: Allocated data never moves, or is overwritten.
- ***Permanent***: Allocated data lasts the lifetime of the process.
- ***Concurrent***: Multiple users can access at the same time.
- ***Large Address Aware:*** On Windows, the library can correctly leverage all 4GB in 32-bit processes.
- ***Cross Platform***: Supports Windows, OSX and Linux.

## Wiki & Documentation

This is a cross-platform library, with shared documentation.

[For basic usage, see wiki](https://reloaded-project.github.io/Reloaded.Memory.Buffers/)

## Example Use Cases

These are just examples:

- ***Hooks***: Hooking libraries like [Reloaded.Hooks](https://github.com/Reloaded-Project/Reloaded.Hooks) can reduce amount of bytes stolen from functions.
- ***Libraries***: Libraries like [Reloaded.Assembler](https://github.com/Reloaded-Project/Reloaded.Assembler) require memory be allocated in first 2GB for x64 FASM.

## Usage

The library provides a simple high level API to use.

See [Wiki](https://reloaded-project.github.io/Reloaded.Memory.Buffers/) for Rust usage

### Community Feedback

If you have questions/bug reports/etc. feel free to [Open an Issue](https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/issues/new).

Contributions are welcome and encouraged. Feel free to implement new features, make bug fixes or suggestions so long as
they meet the quality standards set by the existing code in the repository.

For an idea as to how things are set up, [see Reloaded Project Configurations.](https://github.com/Reloaded-Project/Reloaded.Project.Configurations)

Happy Hacking 💜
*/

pub mod structs {

    pub mod params {
        pub mod buffer_allocator_settings;
        pub use buffer_allocator_settings::BufferAllocatorSettings;

        pub mod buffer_search_settings;
        pub use buffer_search_settings::BufferSearchSettings;
    }

    pub mod errors {
        pub mod buffer_allocation_error;
        pub use buffer_allocation_error::BufferAllocationError;

        pub mod buffer_search_error;
        pub use buffer_search_error::BufferSearchError;

        pub mod item_allocation_error;
        pub use item_allocation_error::ItemAllocationError;
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

    pub mod address_range;
    pub mod cached;
    pub mod mathematics;
    pub mod wrappers;

    #[cfg(target_os = "linux")]
    pub mod linux_map_parser;
}

pub mod c {
    mod buffers_c_buffers;
    mod buffers_c_locatoritem;
    mod buffers_fnptr;
}

pub mod buffers;
