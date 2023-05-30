# Buffer Locator

!!! info "The buffer locator is a shared region of memory dedicated to storing allocated memory regions within a target process."

## Structure

!!! info "Size: `4096 bytes`, to match OS page size."  

- [Header](#header) (14/22 bytes)  
- [Items[]](#item) (fill until end of buffer)  

### Header

!!! note "Size of this header is version dependent. Implementation should not use versions it doesn't recognise."

Size: `14/22 bytes` (Version 0).

- `u32/u64` [This Header Address](#this-header-address)  
- `u32/u64` [Next Locator Ptr](#next-locator-ptr)  
- `u32` [IsLocked](#is-locked)  
- `u3` [Version](#version)  
- `u5` Reserved  
- `u8` [NumItems](#item)  

!!! note "Locks are `u32` because older .NET versions don't support `u8` atomic operations (emitting `cmpxchg` for 1 byte)."

#### This Header Address

!!! note "[This header is allocated using non-persistent Memory Mapped Files](#finding-the-locator-structure)."

Actual address of this header in memory.  
This is the address assigned by the first ever memory mapping of the file.  

For more details, see [Finding the Locator Structure](#finding-the-locator-structure).

#### Next Locator Ptr

!!! info "This is the address of the next locator structure in memory."

i.e. If this is the first locator structure, this will be the address of the second locator structure.  
If this value is non-null, it is assumed that the next locator structure is valid; and the current locator is full.  

#### Is Locked

!!! info "This is a lock, manipulated with `Interlocked.CompareExchange` (x86 `cmpxchg`)."

This lock is taken when the locator is being modified.  
If the lock is taken, the code should re-assert if modifying is still necessary after taking the lock.  

#### Version

!!! info "This stores the version of the buffer in memory."  

Size: `3 bits`, (`0-7`).  

#### Implicit Property: Max Item Count

Max item count is calculated as `(4096 - sizeof(Header)) / sizeof(Item)`.  
Rounded down, of course.  

#### Implicit Property: Is Full

!!! info "In practice for most use cases, this buffer will never be full, but just in case."

This is true when NumItems == [Max Item Count](#implicit-property-max-item-count).

### Item

!!! info "This stores region information about a single buffer."

Size: `16/20 bytes`  

- `u32/u64` Base Address (`u64` if 64-bit process)  
- `u32` IsTaken  
- `u32` Size  
- `u32` Position  

!!! tip "Remaining bytes are calculated by `Size - Position`."  

!!! info "IsTaken is a lock, manipulated with `Interlocked.CompareExchange` (x86 `cmpxchg`). If `IsTaken` is true, skip the current buffer and make another if necessary."

## Finding the Locator Structure

!!! info "The locator structure is always located at the end of the buffer."

Locators are allocated using `Memory Mapped Files`, with predefined name.  
This name is: `/Reloaded.Memory.Buffers.MemoryBuffer, PID {processId}`.  

!!! note "The name starts with a backslash because this is required by some OSes based off of POSIX."

Code below shows basic use of `Memory Mapped Files`:  

=== "C# (Cross Platform)"
    
    ```csharp
    // Create or open the memory-mapped file
    var name = $"/Reloaded.Memory.Buffers.MemoryBuffer, PID {System.Environment.ProcessId}";
    
    MemoryMappedFile mmf;
    bool previouslyExisted = true;
    try { mmf = MemoryMappedFile.OpenExisting(name); }
    catch (FileNotFoundException)
    {
        mmf = MemoryMappedFile.CreateNew(name, allocationGranularity);
        previouslyExisted = false;
    }

    // Access the memory-mapped file
    var view = mmf!.CreateViewAccessor(start, length, MemoryMappedFileAccess.ReadWrite);
    var data = (byte*)view.SafeMemoryMappedViewHandle.DangerousGetHandle();

    // Pointer in `data`.
    ```

=== "C++ (Windows)"

    ```cpp
    #include <windows.h>
    #include <iostream>

    // Note: Untested AI generated code, for reference only.
    // Note: Missing error handling.
    // Construct the name
    bool previouslyExisted = true;
    int pid = GetCurrentProcessId(); // Get current process ID
    char bufferName[256];
    sprintf_s(bufferName, sizeof(bufferName), "/Reloaded.Memory.Buffers.MemoryBuffer, PID %d", pid);

    // Open Memory Mapped File
    HANDLE hMapFile = OpenFileMappingA(
        FILE_MAP_ALL_ACCESS,   // read/write access
        FALSE,                 // do not inherit the name
        bufferName);           // name of mapping object

    if (hMapFile == NULL) {
        // If the file mapping object doesn't exist, create it
        hMapFile = CreateFileMappingA(
            INVALID_HANDLE_VALUE,    // use paging file
            NULL,                    // default security
            PAGE_READWRITE,          // read/write access
            0,                       // max. object size
            allocationGranularity,   // buffer size
            bufferName);             // name of mapping object

        previouslyExisted = false;
    }

    // Map the view
    void* data = MapViewOfFile(
        hMapFile,   // handle to map object
        FILE_MAP_ALL_ACCESS, // read/write permission
        0,
        0,
        allocationGranularity);
    ```

=== "C++ (Linux & OSX)"

    ```cpp
    #include <sys/mman.h>
    #include <sys/stat.h>
    #include <fcntl.h>
    #include <unistd.h>
    #include <iostream>

    // Note: Untested AI generated code (with manual correction), for reference only.
    // Note: Missing error handling.
    // Construct the name
    bool previouslyExisted = true;
    int pid = getpid(); // Get current process ID
    char bufferName[256];
    sprintf_s(bufferName, sizeof(bufferName), "/Reloaded.Memory.Buffers.MemoryBuffer, PID %d", pid); 

    // Open Memory Mapped File
    int fd = shm_open(bufferName, O_RDWR, S_IRUSR | S_IWUSR);
    if (fd == -1) {
        // If the file mapping object doesn't exist, create it
        fd = shm_open(bufferName, O_CREAT | O_RDWR, S_IRUSR | S_IWUSR);
        ftruncate(fd, 4096);
        previouslyExisted = false;
    }

    // Map the view
    void* data = mmap(NULL, 4096, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
    ```

!!! tip "Memory Mapped Files on Windows respect `allocationGranularity` (usually 64KiB) which you can get from `GetSystemInfo`."

!!! tip "Register the remainder of that (usually 64K) buffer after header as first [Items](#item) in the locator."

For the users implementing from other languages, here are the raw OS APIs for reference:  

| Platform    | APIs                                                                       |
|-------------|----------------------------------------------------------------------------|
| Windows     | `CreateFileMapping`, `OpenFileMapping`, `MapViewOfFile`, `UnmapViewOfFile` |
| Linux & OSX | `shm_open`, `shm_unlink`, `mmap`, `munmap`                                 |

Notice the presence of `previouslyExisted` bool.  
This is used to determine if the locator structure needs to be kept alive.

Here's what to do depending on situation:  

=== "Memory File Not Previously Existed"

    - Initialize [This Header Address](#this-header-address), with current address.  
    - Do not unmap file, keep it alive, forever.  

=== "Memory Mapped File Previously Existed"

    - Fetch [This Header Address](#this-header-address), and cache into static field.  
    - Unmap the memory mapped file.  
    - Use address from static field (address of first memory map) in this and further accesses.  

### Cleaning Up

!!! warning "On Linux & OSX, Shared Memory Objects are *NOT* automatically destroyed when all processes close."

!!! warning "Given expected use is in hooking frameworks where crashes are expected to be common on dev machines."

In these scenarios, we cannot waste memory. For Linux, we can look through `/dev/shm` for any unused mapping, and unlink them.  
In the reference library, the following code is ran upon successful opening of existing memory mapped file (i.e. only ever once per library instance).  

=== "C#"

    ```csharp
    private static void Cleanup()
    {
        // Keep the view around forever for other mods/programs/etc. to use.

        // Note: At runtime this is only ever executed once per library instance, so this should be okay.
        // On Linux we need to execute a runtime check to ensure that after a crash, no MMF was left over.
        // because the OS does not auto dispose them.
        if (Polyfills.IsLinux())
        {
            const string shmDirectoryPath = "/dev/shm";
            const string memoryMappedFilePrefix = "Reloaded.Memory.Buffers.MemoryBuffer, PID ";

            // Read all files in /dev/shm
            var files = Directory.EnumerateFiles(shmDirectoryPath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!fileName.StartsWith(memoryMappedFilePrefix))
                    continue;

                // Extract PID from the file name
                var pidStr = fileName.Substring(memoryMappedFilePrefix.Length);
                if (!int.TryParse(pidStr, out var pid))
                    continue;

                // Check if the process is still running
                if (!IsProcessRunning(pid))
                    Posix.shm_unlink(fileName);
            }
        }
    }

    private static bool IsProcessRunning(int pid)
    {
        try
        {
            Process.GetProcessById(pid);
            return true;
        }
        catch (ArgumentException)
        {
            // Process is not running
            return false;
        }
    }
    ```

=== "C++"

    ```cpp
    // AI Generated.
    // Note: This is untested code, for reference only.
    // Note: This will only build on a Linux/OSX box due to included headers. You'll need to compile guard this.
    #include <sys/stat.h>
    #include <dirent.h>
    #include <fcntl.h>
    #include <signal.h>
    #include <unistd.h>
    #include <cstdlib>
    #include <cstring>
    #include <iostream>
    #include <sys/mman.h>

    bool isProcessRunning(int pid) {
        return kill(pid, 0) == 0;
    }    

    void cleanup() {
        const char* shmDirectoryPath = "/dev/shm";
        const char* memoryMappedFilePrefix = "Reloaded.Memory.Buffers.MemoryBuffer, PID ";
        size_t prefixLength = strlen(memoryMappedFilePrefix);
    
        DIR* dirp = opendir(shmDirectoryPath);
        if (dirp == nullptr) {
            perror("Could not open /dev/shm directory");
            return;
        }
    
        struct dirent* dp;
        while ((dp = readdir(dirp)) != nullptr) {
            if (strncmp(dp->d_name, memoryMappedFilePrefix, prefixLength) == 0) {
                char* pidStr = dp->d_name + prefixLength;
                int pid = std::atoi(pidStr);
    
                if (!isProcessRunning(pid)) {
                    std::string filePath = std::string(shmDirectoryPath) + "/" + dp->d_name;
                    shm_unlink(filePath.c_str());
                }
            }
        }
    
        closedir(dirp);
    }
    ```

## Supporting Concurrency

!!! info "Only one user may use a buffer at any time."

Access to all buffers should look something like:  

```csharp
// Ensure safe disposal of buffer 
// - `using` in C#
// - RAII in C++
// - Drop trait in Rust
// etc.
using var buffer = BufferHelper.GetOrAllocateBuffer(minAddress, maxAddress, size);
```

When the buffer is acquired, a the [IsTaken](#item) field is set to `1` using `cmpxchg`.  

```csharp
// C#
item->IsTaken = Interlocked.CompareExchange(ref item->IsTaken, 1, 0);
```

When the buffer is released, the `IsTaken` field is set to `0` using `cmpxchg`.  

```csharp
item->IsTaken = 0;
```

## Allocating Buffers

!!! tip "[Allocation algorithm is documented here](./allocation-algorithm.md)"

If an additional buffer requires to be allocated, the following steps are taken:

- If an existing buffer is not taken, and has sufficient space, [lock it](#supporting-concurrency) and use it.  

or...

- If no unlocked buffer exists, and `header.NumItems` < [MaxItemCount](#implicit-property-max-item-count), allocate a new buffer.  
    - Lock the [allocator structure](#is-locked).  
    - Allocate memory (multiple of 4096 sufficient to fit content).  
    - Write buffer to end of header and increment `NumItems`.

or...

- If no unlocked buffer exists, and `header.NumItems` >= [MaxItemCount](#implicit-property-max-item-count)...

=== "Next Locator Ptr is 0"
 
    - Allocate new locator (malloc 4096 bytes) and initialize [header](#structure).  
    - Initialize new locator. 
    - Assign [Next Locator Ptr](#next-locator-ptr).  
    - Operate on [Next Locator Ptr]

=== "Next Locator Ptr is not 0"

    - Visit [Next Locator Ptr](#next-locator-ptr) and try finding an unlocked buffer.

!!! note "In practice, it's expected another locator will probably never be allocated."