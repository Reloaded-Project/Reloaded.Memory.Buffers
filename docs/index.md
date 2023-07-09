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

!!! info "`Reloaded.Memory.Buffers` is a library for allocating memory between a given minimum and maximum memory address, for C# and Rust"

With the following properties:

- ***Memory Efficient***: No wasted memory.  
- ***Shared***: Can be found and read/written to by multiple users.  
- ***Static***: Allocated data never moves, or is overwritten.  
- ***Permanent***: Allocated data lasts the lifetime of the process.  
- ***Concurrent***: Multiple users can access at the same time.  
- ***Large Address Aware:*** On Windows, the library can correctly leverage all 4GB in 32-bit processes.  
- ***Cross Platform***: Supports Windows, OSX and Linux.  

## Example Use Cases

!!! tip "These are just examples."

- ***Hooks***: Hooking libraries like [Reloaded.Hooks](https://github.com/Reloaded-Project/Reloaded.Hooks) can reduce amount of bytes stolen from functions.  
- ***Libraries***: Libraries like [Reloaded.Assembler](https://github.com/Reloaded-Project/Reloaded.Assembler) require memory be allocated in first 2GB for x64 FASM.  

## Usage

!!! info "The library provides a simple high level API to use."

!!! note "Both C# and Rust ports expose the same APIs."

### Get A Buffer

!!! info "Gets a buffer where you can allocate 4096 bytes in first 2GiB of address space."

=== "C#"

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

=== "Rust"

	```rust
	let settings = BufferSearchSettings {
		min_address: 0 as usize,
		max_address: i32::MAX as usize,
		size: 4096,
	};

	// Automatically dropped.
	let item = Buffers::get_buffer(&settings)?;

	// Append some data.
	unsafe {
		item.append_bytes(data);
	}
	```

### Get A Buffer (With Proximity)

!!! info "Gets a buffer where 4096 bytes written will be within 2GiB of 0x140000000."

=== "C#"

	```csharp
	var settings = BufferSearchSettings.FromProximity(int.MaxValue, (nuint)0x140000000, 4096);

	// Make sure to dispose, so lock gets released.
	using var item = Buffers.GetBuffer(settings);

	// Write some data, get pointer back.
	var ptr = item->Append(data); 
	```

=== "Rust"

	```rust
	let settings = BufferAllocatorSettings::from_proximity(i32::MAX, 0x140000000 as usize, 4096);
	
	// Automatically dropped.
	let item = Buffers::get_buffer(settings)?;

	// Append some data.
	unsafe {
		item?.append_bytes(data);
	}
	```

### Allocate Memory

!!! info "Allows you to temporarily allocate memory within a specific address range and size constraints."


=== "C#"

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

=== "Rust"

	```rust
	let mut settings = BufferAllocatorSettings::new();
	settings.min_address = 0;
	settings.max_address = i32::MAX as usize;
	settings.size = 4096;

	let item = Buffers::allocate_private_memory(&mut settings).unwrap();

	// You have allocated memory in first 2GiB of address space.
	// Disposing this memory (via `using` statement) will free it.
	assert!(item.base_address.as_ptr() != std::ptr::null_mut());
	assert!(item.size >= settings.size as usize);
	```

!!! note "You can specify another process with `TargetProcess = someProcess` in `BufferAllocatorSettings`, but this is only supported on Windows."

## Community Feedback

If you have questions/bug reports/etc. feel free to [Open an Issue](https://github.com/Reloaded-Project/Reloaded.Memory.Buffers/issues/new).

Contributions are welcome and encouraged. Feel free to implement new features, make bug fixes or suggestions so long as
they meet the quality standards set by the existing code in the repository.

For an idea as to how things are set up, [see Reloaded Project Configurations.](https://github.com/Reloaded-Project/Reloaded.Project.Configurations)

Happy Hacking 💜