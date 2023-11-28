<div align="center">
	<h1>The Reloaded Buffers Library</h1>
	<img src="https://camo.githubusercontent.com/8f75011947bdd0d14630a789dd4edc11edcbcd9f51bc94511c0f45ec71a0d06b/68747470733a2f2f692e696d6775722e636f6d2f426a506e3772552e706e67" width="150" align="center" />
	<br/> <br/>
	<strong><i>Allocate Memory, & Knuckles</i></strong>
	<br/> <br/>
	<!-- Coverage -->
	<a href="https://codecov.io/gh/Reloaded-Project/Reloaded.Memory.Buffers">
		<img src="https://codecov.io/gh/Reloaded-Project/Reloaded.Memory.Buffers/branch/master/graph/badge.svg" alt="Coverage" />
	</a>
	<!-- Crates -->
	<a href="https://crates.io/crates/reloaded_memory_buffers">
		<img src="https://img.shields.io/crates/dv/reloaded_memory_buffers" alt="NuGet" />
	</a>
	<!-- Build Status -->
	<a href="https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/actions/workflows/rust.yml">
		<img src="https://img.shields.io/github/actions/workflow/status/Reloaded-Project/Reloaded.Memory.Buffers/rust.yml" alt="Build Status" />
	</a>
	<br/>
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

`Reloaded.Memory.Buffers` is a library for allocating memory between a given minimum and maximum memory address, for C# and Rust.

With the following properties:

- ***Memory Efficient***: No wasted memory.  
- ***Shared***: Can be found and read/written to by multiple users.  
- ***Static***: Allocated data never moves, or is overwritten.  
- ***Permanent***: Allocated data lasts the lifetime of the process.  
- ***Concurrent***: Multiple users can access at the same time.  
- ***Large Address Aware:*** On Windows, the library can correctly leverage all 4GB in 32-bit processes.  
- ***Cross Platform***: Supports Windows, OSX and Linux.  

Note: Rust/C port also work with FreeBSD (untested), and has partial [(limited) Android support](https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/issues/3).

## Wiki & Documentation

[For full documentation, please see the Wiki](https://reloaded-project.github.io/Reloaded.Memory.Buffers/).  

## Example Use Cases

These are just examples:  

- ***Hooks***: Hooking libraries like [Reloaded.Hooks](https://github.com/Reloaded-Project/Reloaded.Hooks) can reduce amount of bytes stolen from functions.
- ***Libraries***: Libraries like [Reloaded.Assembler](https://github.com/Reloaded-Project/Reloaded.Assembler) require memory be allocated in first 2GB for x64 FASM.

## Usage

!!! info "The library provides a simple high level API to use."

!!! info "See [Wiki](https://reloaded-project.github.io/Reloaded.Memory.Buffers/) for Rust usage"

### Get A Buffer

Gets a buffer where you can allocate 4096 bytes in first 2GiB of address space.

```csharp
var settings = new BufferSearchSettings()
{
    MinAddress = 0,
    MaxAddress = int.MaxValue,
    Size = 4096
};

// Make sure to dispose, so lock gets released.
using var item = Buffers.GetBuffer(settings);

// Write some data, get pointer back.
var ptr = item->Append(data); 
```

### Get A Buffer (With Proximity)

Gets a buffer where 4096 bytes written will be within 2GiB of 0x140000000.

```csharp
var settings = BufferSearchSettings.FromProximity(int.MaxValue, (nuint)0x140000000, 4096);

// Make sure to dispose, so lock gets released.
using var item = Buffers.GetBuffer(settings);

// Write some data, get pointer back.
var ptr = item->Append(data); 
```

### Allocate Memory

Allows you to temporarily allocate memory within a specific address range and size constraints.

```csharp
// Arrange
var settings = new BufferAllocatorSettings()
{
    MinAddress = 0,
    MaxAddress = int.MaxValue,
    Size = 4096
};

using var item = Buffers.AllocatePrivateMemory(settings);

// You have allocated memory in first 2GiB of address space.
// Disposing this memory (via `using` statement) will free it.
item.BaseAddress.Should().NotBeNull();
item.Size.Should().BeGreaterOrEqualTo(settings.Size);
```

You can specify another process with `TargetProcess = someProcess` in `BufferAllocatorSettings`, but this is only supported on Windows.

## Crate Features (Rust)

- *no_format*: Disables formatting code in errors, saving ~8kB of space.  
- *size_opt*: Makes cold paths optimized for size instead of optimized for speed. [Requires 'nightly' Rust]  

## Community Feedback

If you have questions/bug reports/etc. feel free to [Open an Issue](https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/issues/new).

Contributions are welcome and encouraged. Feel free to implement new features, make bug fixes or suggestions so long as
they meet the quality standards set by the existing code in the repository.

For an idea as to how things are set up, [see Reloaded Project Configurations.](https://github.com/Reloaded-Project/Reloaded.Project.Configurations)

Happy Hacking 💜