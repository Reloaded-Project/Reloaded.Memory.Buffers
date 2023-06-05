# Buffers Specification

!!! note "Version 1.0.0 (Library Version 3.0.0+)"

!!! info "`Reloaded.Memory.Buffers` is a library for allocating memory between a given minimum and maximum memory address"

With the following properties:  

- ***Memory Efficient***: No wasted memory.  
- ***Shared***: Can be found and read/written to by multiple processes.    
- ***Static***: Allocated data never moves, or is overwritten.  
- ***Permanent***: Allocated data lasts the lifetime of the process.  
- ***Concurrent***: Multiple users can access at the same time.  
- **Large Address Aware:** On Windows, the library can correctly leverage all 4GB in 32-bit processes.  

## Use Cases

!!! tip "These are just examples."

- ***Hooks***: Hooking libraries like [Reloaded.Hooks](https://github.com/Reloaded-Project/Reloaded.Hooks) can reduce amount of bytes stolen from functions.  
- ***Libraries***: Libraries like [Reloaded.Assembler](https://github.com/Reloaded-Project/Reloaded.Assembler) require memory be allocated in first 2GB.  

And some other useful functionality.

## Field Sizes

Field sizes used used in this spec are based on Rust notation; with some custom types e.g.

- `u8`: Unsigned 8 bits.
- `i8`: Signed 8 bits.
- `u4`: 4 bits.
- `u32/u64`: 4 Bytes or 8 Bytes (depending on variant).

Assume any bit packed values are sequential, i.e. if `u4` then `u4` is specified, first `u4` is the upper 4 bits.

All packed fields are `little-endian`; and written out when total number of bits aligns with a power of 2.

- `u6` + `u12` is 2 bytes `little-endian`
- `u15` + `u17` is 4 bytes `little-endian`
- `u26` + `u22` + `u16` is 8 bytes `little-endian`
- `u6` + `u11` + `u17` ***is 4 bytes*** `little-endian`, ***not 2+2***

## General Access Pattern

!!! note "Names below are not final API, only for illustration purposes."

```mermaid
flowchart TB
  User["User"] --> GetOrAllocateBuffer(["Buffers.GetBuffer"])
  GetOrAllocateBuffer --> BufferLocatorFind(["LocatorHeaderFinder.Find"])
  BufferLocatorFind -- "(Via Memory Mapped Files)" --> GetAvailableItem(["LocatorHeader.GetFirstAvailableItem(locator)"])
  GetAvailableItem --> BufferMatch{Buffer Match Found?}
  BufferMatch -- Yes --> ReturnBufferToUser["Lock & Return Buffer to User"]
  BufferMatch -- "No (Make New Buffer)" --> CanRegisterBuffer{Locator Has Space for New Entry?}
  CanRegisterBuffer -- "No (Alloc New Locator, Link via Pointer & Try Again)" --> GetAvailableItem
  CanRegisterBuffer -- "Yes (Allocate Memory and Register)" --> BufferAllocator(["BufferAllocator.Allocate"])
  BufferAllocator -- "Lock & Register Returned Buffer" --> BufferLocatorRegister(["LocatorHeader.Register"])
  BufferLocatorRegister --> ReturnNewBufferToUser["Return New Buffer to User"]
```

In the flowchart above, the user calls `GetBuffer`, which in turn calls `LocatorHeaderFinder.Find` to get address of the
[locator structure](buffer-locator.md#structure).

If a match is found (Yes path), the buffer is locked and then returned to the user. 

If no match is found (No path), a new buffer is allocated, and locked. If this buffer can fit into the current
locator, it is appended. Otherwise a new locator is allocated (linked via pointer), and the buffer is registered into
the new locator. Buffer is then returned to the user.