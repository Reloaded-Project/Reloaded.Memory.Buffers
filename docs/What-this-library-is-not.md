
## What this library is not

### The fastest solution to the problem

This library is fast, but it is not the fastest, What I chose myself is a solution balanced to give acceptable performance in both internal and external operations to match the goals and use case of the library.

While many optimizations have been done under the hood, mainly involving caching to keep this library fast,  If we apply certain constraints to our problem such restricting the library to only work with buffers in the current process (thus limiting the feature set of the library), faster solutions could still be engineered.

#### Example A
If the buffer was guaranteed to reside in the current process, we could use pointer arithmetic directly to access the buffer properties, rather than copying the buffer properties to a local variable, writing a to the buffer, updating the properties and writing the new struct properties back to memory.

*(Would notably improve performance of repeatedly adding many small elements to the buffer; for larger structs to effect is too minor to be noticeable)*

#### Example B
We could store only one master buffer with the "magic header" in a page as low as possible (minimize use of `VirtualQuery` in finding the buffers) and instead use a linked list of buffer properties (`BufferProperties`). However, traversing the linked list for buffers in an external process would be comparably rather slow due to a considerably increased number of system calls, requiring a single call every iteration to next element.

*(Caching of already found MemoryBuffers is implemented to mitigate the problem of slower finding of buffers/new buffers. New buffers will only be searched if no already known buffer satisfies the users' required parameters)*

### Storage of disposable memory

The whole point of this library is to provide storage of data that persists until the end of the application's lifetime, efficiently (wasting as little allocated memory as possible).

Because this library's `MemoryBuffers` allow themselves to be shared between multiple threads and applications; deallocating any buffer ever would be a too dangerous operation as the use case of any item in a given buffer is unknown. 

That said, if you want disposable memory in a in a set region of memory, you can use the `MemoryBufferHelper` to give you a location of where you could allocate X bytes of memory and allocate that memory yourself with `VirtualAlloc`  for your own private use.

### Dynamically resizable memory

With the reasons same as above, with the use of items in buffers unknown; the buffers do not have the option to relocate themselves or to grow.

While in theory it is possible to safely grow the buffer onto the next/following pages of memory provided that the buffer location is not relocated, in practice, I believe that the feature is not useful enough to warrant implementing at the current time.

