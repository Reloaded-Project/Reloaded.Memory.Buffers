
<div align="center">
	<h1>Reloaded.Memory.Buffers: Getting Started</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong><i>Everything not saved will be lost.</i></strong>
</div>

## Page Information

🕒 Reading Time: 05 Minutes

## Introduction

The following is a small, quick, non-exhaustive resource to help you get started with the *Reloaded.Memory.Buffers* library - providing an introduction to writing code using the library. This serves as a guide to help you get going, covering the basics and essentials.

## Adding Reloaded.Memory to your project.
1.  Open/Create project in Visual Studio.
2.  Right-click your project within the `Solution Explorer` and select `Manage NuGet Packages`.
3.  Search for `Reloaded.Hooks`.
4.  Install the package.

## Prologue
Reloaded.Memory.Buffers, part of Project-Reloaded (also known as Reloaded 3.X) exposes a class called `MemoryBufferHelper` to help you get started with buffers.

The class' sole purpose is to be a one stop shop for creating and finding already existing `MemoryBuffer`s within a process. 

For brevity, the public API can be simplified to the following;
```csharp
/* The process MemoryBufferHelper was instantiated with. */
Process Process { get; }

/* Creates a new MemoryBuffer which can store at least `size` bytes inside a 
   given minimum and maximum memory address. */
MemoryBuffer CreateMemoryBuffer(int size, long minimumAddress = 0, long maximumAddress = long.MaxValue, int retryCount = 3);

/* Finds existing MemoryBuffers in a process between minimumAddress and 
   maximumAddress */
MemoryBuffer[] FindBuffers(int size, IntPtr minimumAddress, IntPtr maximumAddress, bool useCache = true);
```

Which should hopefully be easy and straightforward to use once you know the purpose of this library.

The `MemoryBufferHelper` class also provides you with the following utility methods:

```csharp
/* Utility method that retrieves the memory address and size of allocation.
   if you requested to create a MemoryBuffer with the given parameters. */
BufferAllocationProperties FindBufferLocation(int size, IntPtr minimumAddress, IntPtr maximumAddress);

/* Utility method that retrieves the number of bytes that would be allocated 
   if you were to create a MemoryBuffer that needs to fit at least `size` bytes.*/
int GetBufferSize(int size);
```

These internal methods have been exposed to you as public in the case that you would want to allocate memory yourself in a given address range with VirtualAlloc and not share it with others. 

They return the address and/or size of allocation that you would need to supply in the case that you would want to make the memory allocation yourself.

## Using MemoryBuffer

Once you have obtained an instance of `MemoryBuffer`, after either creating a new instance using `MemoryBufferHelper` or finding existing instances; you obtain access to the following API (once again, simplified for brevity):

```csharp
/* Part of Reloaded.Memory. 
   Provides access to process' memory in which the buffer resides. */
IMemory MemorySource { get; }

/* Allows you to peek into the current properties of the buffer 
   such as size, offset, pointer, alignment of last item. */
MemoryBufferProperties Properties { get; set; }

/* Appends an array to the end of the buffer.
   Returns pointer to appended array. */
IntPtr Add(byte[] bytesToWrite, int alignment = 4);

/* Appends generic type to the end of the buffer.
   Returns pointer to appended generic.*/
IntPtr Add<TStructure>(ref TStructure bytesToWrite, bool marshalElement = false, int alignment = 4);

/* Checks if an item objectSize in bytes can fit in the buffer. */
bool CanItemFit(int objectSize);

/* Returns false if a given item cannot fit into buffer, else true. */
bool CanItemFit<TGeneric>(ref TGeneric item, bool marshalElement = false);
```

From here, so long that you know the library's true purpose, what to do next should be clear.
