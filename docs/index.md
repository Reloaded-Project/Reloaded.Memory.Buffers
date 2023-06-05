---
hide:
  - toc
---

<div align="center">
	<h1>The Reloaded Buffers Library</h1>
	<img src="Reloaded/Images/Reloaded-Icon.png" width="150" align="center" />
	<br/> <br/>
	<strong><i>Allocate Memory, & Knuckles</i></strong>
	<br/> <br/>
	<!-- Coverage -->
	<a href="https://codecov.io/gh/Reloaded-Project/Reloaded.Memory.Buffers">
		<img src="https://codecov.io/gh/Reloaded-Project/Reloaded.Memory.Buffers/branch/master/graph/badge.svg" alt="Coverage" />
	</a>
	<!-- NuGet -->
	<a href="https://www.nuget.org/packages/Reloaded.Memory.Buffers">
		<img src="https://img.shields.io/nuget/v/Reloaded.Memory.Buffers.svg" alt="NuGet" />
	</a>
	<!-- Build Status -->
	<a href="https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/actions/workflows/build-and-publish.yml">
		<img src="https://img.shields.io/github/actions/workflow/status/Reloaded-Project/Reloaded.Memory.Buffers/build-and-publish.yml" alt="Build Status" />
	</a>
</div>

## About

!!! info "`Reloaded.Memory.Buffers` is a library for allocating memory between a given minimum and maximum memory address"

With the following properties:

- ***Memory Efficient***: No wasted memory.  
- ***Shared***: Can be found and read/written to by multiple processes.  
- ***Static***: Allocated data never moves, or is overwritten.  
- ***Permanent***: Allocated data lasts the lifetime of the process.  
- ***Concurrent***: Multiple users can access at the same time.  
- **Large Address Aware:** On Windows, the library can correctly leverage all 4GB in 32-bit processes.  

## Example Use Cases

!!! tip "These are just examples."

- ***Hooks***: Hooking libraries like [Reloaded.Hooks](https://github.com/Reloaded-Project/Reloaded.Hooks) can reduce amount of bytes stolen from functions.  
- ***Libraries***: Libraries like [Reloaded.Assembler](https://github.com/Reloaded-Project/Reloaded.Assembler) require memory be allocated in first 2GB for x64 FASM.  

## Usage

!!! info "The library is available as a NuGet package."


## Common Utilities

!!! info "Common Classes within this Package Include"

**Memory Manipulation:  **

| Action                              | Description                                                                         |
|-------------------------------------|-------------------------------------------------------------------------------------|
| [Memory](./About-Memory.md)         | Allows you to Read, Write, Allocate & Change Memory Protection for Current Process. |
| [ExternalMemory](./About-Memory.md) | Read, Write, Allocate & Change Memory Protection but for Another Process.           |

**Streams Management:  **

| Action                                                                                                                        | Description                                     |
|-------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| BigEndian([Reader](./Streams/EndianReaders/BigEndianReader.md)/[Writer](./Streams/EndianReaders/BigEndianWriter.md))          | Read/write raw data in memory as Big Endian.    |
| LittleEndian([Reader](./Streams/EndianReaders/LittleEndianReader.md)/[Writer](./Streams/EndianReaders/LittleEndianWriter.md)) | Read/write raw data in memory as Little Endian. |
| [BufferedStreamReader](./Streams/BufferedStreamReader.md)                                                                     | High performance alternative to `BinaryReader`. |

**Extensions:  **

| Action                                                                                      | Description                                                                       |
|---------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| ([Array](./Extensions/ArrayExtensions.md)/[Span](./Extensions/SpanExtensions.md))Extensions | Unsafe slicing, references without bounds checks and SIMD accelerated extensions. |
| [StreamExtensions](./Extensions/StreamExtensions.md)                                        | Extensions for reading and writing from/to generics.                              |
| [StringExtensions](./Extensions/StringExtensions.md)                                        | Custom Hash Function(s) and unsafe character references.                          |

**Utilities:  **

| Action                                                       | Description                                                                            |
|--------------------------------------------------------------|----------------------------------------------------------------------------------------|
| [ArrayRental & ArrayRentalSlice](./Utilities/ArrayRental.md) | Safe wrapper around `ArrayPool<T>` rentals.                                            |
| [Box<T>](./Utilities/Box.md)                                 | Represents a boxed value type, providing build-time validation and automatic unboxing. |
| [CircularBuffer](./Utilities/CircularBuffer.md)              | Basic high-performance circular buffer.                                                |
| [Pinnable<T>](./Utilities/Pinnable.md)                       | Utility for pinning C# objects for access from native code.                            |

**Base building blocks:  **

| Action                                                                                   | Description                                     |
|------------------------------------------------------------------------------------------|-------------------------------------------------|
| [Ptr&lt;T&gt; / MarshalledPtr&lt;T&gt;](./Pointers/Ptr.md)                               | Abstraction over a pointer to arbitrary source. |
| [FixedArrayPtr&lt;T&gt; & MarshalledFixedArrayPtr&lt;T&gt;](./Pointers/FixedArrayPtr.md) | Abstraction over a pointer with known length.   |

(This list is not exhaustive, please see the API Documentation for complete API)

## Community Feedback

If you have questions/bug reports/etc. feel free to [Open an Issue](https://github.com/Reloaded-Project/Reloaded.Memory/issues/new).

Contributions are welcome and encouraged. Feel free to implement new features, make bug fixes or suggestions so long as
they meet the quality standards set by the existing code in the repository.

For an idea as to how things are set up, [see Reloaded Project Configurations.](https://github.com/Reloaded-Project/Reloaded.Project.Configurations)

Happy Hacking 💜