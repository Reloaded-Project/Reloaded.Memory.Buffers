## Why was this created? (A Story)

This library was built as part of the process of building my own library for hooking custom pure unmanaged functions in .NET. One of the things I needed to do is simply out of the need to minimize the amount of bytes necessary to perform an absolute jump in x64 assembler to a given address, without modifying any registers.

### The Prologue
Trying to tackle this problem, naturally the first thing that you would try to do is the following:
```x86asm
push 0x123456789
ret             
```

Until you notice that unfortunately for you, in x64 `push` and many other common instructions do not support 64bit immediates and you get an assembler error.

Although that is not enough to stop you. You are never gonna give it up, you are never gonna let it down. 

The cheeky little rodent you are, you try some "black magic" with the stack and one of the lesser known instructions:
```x86asm
push rax
mov rax, 0x123456789
xchg rax, [rsp]
ret   
```
Aaaaand it works...
*Until you notice that your assembled solution is now 16 bytes long; yikes!*

So you start bargaining and think to yourself, there has got to be a better way. What if I could make... sacrifices... live... the dangerous way... figure which register is unused... and... maybe... just maybe... steal it.
```x86asm
mov rax, 0x123456789
jmp rax
```
12 bytes.

You know which registers the hooked function uses... but you also probably know that you can't always know everything and *that this is probably not a good idea*. 

### Acceptance, or lack of.

Even with the possibility of reducing the hook length to 12 bytes, I was still not entirely happy with the outcome, I wanted to do better. I just wondered, really *what if I could just cheat the system*? 

Well, after some research, which is well... rather hard for anything x86 related due to the complexity of the architecture. I learned... or rather finally remembered that addresses at given memory operands are referenced by the architecture's default word size:

```x86asm
jmp qword [0xDEDBEEF] // FASM syntax requires explicit operand size
```

7 bytes. Glorious.

Now the only problem left is how we place a pointer in 32bit address space, specifically at a place like `0xDEDBEEF`.

The code I was working with already had a JIT/Live assembler based on FASM, so anything like manual patching of pointers was not necessary, the question was rather how I would . After all `GlobalAlloc`, `LocalAloc`, `malloc` and other means of allocating memory are unpredictable.

Well, there was only one possible solution left. Find some free space and ask the kernel, nicely, to allocate it ourselves. Using the Windows API I cooked up a quick solution that would first map out the memory inside the process by looping calls to `VirtualQuery` to enumerate all virtual memory pages. From there, find a free page, allocate it with `VirtualAlloc` and done.

Of course this skips some steps inbetween such as aligning and only considering allocation addresses that line up with the allocation granularity or rounding the allocation size to page size. 

And well, that solution was improved and expanded as part of Reloaded-Mod-Loader to become "MemoryBuffers", and now as part of Reloaded3 it's being split and further modularized into its own library with multiple enhancements along the way like proper concurrency support, performance improvements via caching and unit tests to verify functionality.
