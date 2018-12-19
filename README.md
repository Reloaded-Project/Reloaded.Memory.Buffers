


<div align="center">
	<h1>Project Reloaded: Buffers Library</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong><i>Allocate Memory... & Knuckles</i></strong>
	<br/> <br/>
	<!-- TODO: Update This -->
	<!-- Coverage -->
	<a href="">
		<img src="" alt="Coverage" />
	</a>
	<!-- Build Status -->
	<a href="">
		<img src="" alt="Build Status" />
	</a>
</div>

# Introduction
Reloaded.Memory.Buffers is a library designed with a rather simple purpose:
*Allocate and provide access to memory between a given minimum and maximum memory address.*

The library provides an implementation of efficient, shared, concurrent and permanent storage of many small objects in memory in a static, non-changing locations that last the lifetime of the process.

The goal is to allow multiple applications to access and write to shared contiguous memory regions *(in user defined min - max memory regions)* without wasting memory or time because due to memory micro allocations.

## Features
Below is a list of ideas as to what you can do/should expect from this library:

+ General purpose memory storage shared between different threads, processes and modules in same process.
+ Support for creating and using buffers (`MemoryBuffers`) in both current and external processes.
+ Reasonable performance in both the internal (current process) and external implementations.
+ Easy to use method for finding existing `MemoryBuffers` in both current and external processes.
+ The ability to allocate `MemoryBuffers` in user specified memory address range.

## Non-Features
Below is a list of ideas as to what you should NOT expect from this library:
+ The straight up fastest, most performant solution. (Not possible without limiting functionality)
+ Storage of disposable memory. (Everything written is stored for the lifetime of the program)
+ Relocatable & resizable memory. (Usage of written bytes is unpredictable. Cannot fulfill.)

For more details please see what this library is not..

## Documentation

The following below are links aimed to help you get started with the library, they cover the basics of use:

+ [Getting Started](https://github.com/Reloaded-Project/Reloaded.Memory/blob/master/Docs/Getting-Started.md)
+ [Why this library was made]()
+ [What this library is not]()

For extra ideas of how to use the library, you may always take a look at `Reloaded.Memory.Buffers.Tests`, the test suite for the library.

## Contributions
As with the standard for all of the `Reloaded-Project`, repositories; contributions are very welcome and encouraged.

Feel free to implement new features, make bug fixes or suggestions so long as they are accompanied by an issue with a clear description of the pull request.

If you are implementing new features, please do provide the appropriate unit tests to cover the new features you have implemented; try to keep the coverage near 100%.
