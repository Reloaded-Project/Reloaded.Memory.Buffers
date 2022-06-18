using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reloaded.Memory.Buffers.Tests.Helpers;
using Reloaded.Memory.Sources;
using Xunit;
using static Reloaded.Memory.Buffers.Internal.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers.Tests
{
    public class MemoryBufferTests : IDisposable
    {
        private MemoryBufferHelper _bufferHelper;
        private MemoryBufferHelper _externalBufferHelper;
        public MemoryBufferTests()
        {
            _bufferHelper = new MemoryBufferHelper(Process.GetCurrentProcess());
            _externalBufferHelper = new MemoryBufferHelper(Process.Start("HelloWorld.exe"));
        }

        public void Dispose()
        {
            _externalBufferHelper.Process.Kill();
            _externalBufferHelper.Process.Dispose();
        }

        /// <summary>
        /// Tests if a <see cref="MemoryBuffer"/> can be successfully created.
        /// </summary>
        [Fact]
        public void CreateBufferInternal() => CreateBufferBase(_bufferHelper);

        /// <summary>
        /// Tests if a <see cref="MemoryBuffer"/> can be successfully created.
        /// </summary>
        [Fact]
        public void CreateBufferExternal() => CreateBufferBase(_externalBufferHelper);

        /// <summary>
        /// Tests if a <see cref="PrivateMemoryBuffer"/> can be successfully created.
        /// </summary>
        [Fact]
        public void CreatePrivateBufferInternal() => CreatePrivateBufferBase(_bufferHelper);

        /// <summary>
        /// Tests if a <see cref="PrivateMemoryBuffer"/> can be successfully created.
        /// </summary>
        [Fact]
        public void CreaterivateBufferExternal() => CreatePrivateBufferBase(_externalBufferHelper);

        /// <summary>
        /// Creates a <see cref="MemoryBuffer"/> and attempts to retrieve it by searching for it in memory.
        /// </summary>
        [Fact]
        private void GetBuffersInternal() => GetBuffers(_bufferHelper);

        /// <summary>
        /// Creates a <see cref="MemoryBuffer"/> and attempts to retrieve it by searching for it in memory.
        /// </summary>
        [Fact]
        private void GetBuffersExternal() => GetBuffers(_externalBufferHelper);

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        [Fact]
        public unsafe void GetBuffersInRangeInternal() => GetBuffersInRange(_bufferHelper, GetMaxAddress(_bufferHelper));

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        [Fact]
        public unsafe void GetBuffersInRangeExternal() => GetBuffersInRange(_externalBufferHelper, GetMaxAddress(_externalBufferHelper));

#if X86
        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        [Fact]
        public unsafe void GetBuffersInRangeInternal_LargeAddressAware()
        {
            AssertLargeAddressAware();
            GetBuffersInRange(_bufferHelper, int.MaxValue, uint.MaxValue);
        }

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        [Fact]
        public unsafe void GetBuffersInRangeExternal_LargeAddressAware()
        {
            AssertLargeAddressAware();
            GetBuffersInRange(_externalBufferHelper, int.MaxValue, uint.MaxValue);
        }

        void AssertLargeAddressAware()
        {
            var maxAddress = GetMaxAddress(_externalBufferHelper, true);
            if ((long)maxAddress <= int.MaxValue)
                Assert.False(true, "Test host is not large address aware!!");
        }
#endif

        /* Same as above, except without cache. */

        [Fact]
        public unsafe void GetBuffersInRangeInternalNoCache() => GetBuffersInRangeNoCache(_bufferHelper, GetMaxAddress(_bufferHelper));
        
        [Fact]
        public unsafe void GetBuffersInRangeExternalNoCache() => GetBuffersInRangeNoCache(_externalBufferHelper, GetMaxAddress(_externalBufferHelper));

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void MemoryBufferAddGenericInternal() => MemoryBufferAddGeneric(CreateMemoryBuffer(_bufferHelper), _bufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void MemoryBufferAddGenericExternal() => MemoryBufferAddGeneric(CreateMemoryBuffer(_externalBufferHelper), _externalBufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="PrivateMemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void PrivateMemoryBufferAddGenericInternal() => MemoryBufferAddGeneric(CreatePrivateMemoryBuffer(_bufferHelper), _bufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="PrivateMemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void PrivateMemoryBufferAddGenericExternal() => MemoryBufferAddGeneric(CreatePrivateMemoryBuffer(_externalBufferHelper), _externalBufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>, with raw data;
        /// including the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private unsafe void MemoryBufferAddByteArrayInternal() => MemoryBufferAddByteArray(CreateMemoryBuffer(_bufferHelper), _bufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>, with raw data;
        /// including the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private unsafe void MemoryBufferAddByteArrayExternal() => MemoryBufferAddByteArray(CreateMemoryBuffer(_externalBufferHelper), _externalBufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="PrivateMemoryBuffer"/>, with raw data;
        /// including the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private unsafe void PrivateMemoryBufferAddByteArrayInternal() => MemoryBufferAddByteArray(CreatePrivateMemoryBuffer(_bufferHelper), _bufferHelper.Process);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="PrivateMemoryBuffer"/>, with raw data;
        /// including the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private unsafe void PrivateMemoryBufferAddByteArrayExternal() => MemoryBufferAddByteArray(CreatePrivateMemoryBuffer(_externalBufferHelper), _externalBufferHelper.Process);


        /*
         * ----------
         * Core Tests
         * ----------
        */

        [Fact]
        private void AllocateFree()
        {
            for (int x = 0; x < 20; x++)
            {
                // Commit
                var buf = _bufferHelper.Allocate(4096);
                var extBuf = _externalBufferHelper.Allocate(4096);

                // Write something to start of buffers to test allocation.
                var bufMem = new Sources.Memory();
                var extBufMem = new ExternalMemory(_externalBufferHelper.Process);

                bufMem.Write(buf.MemoryAddress, 5);
                extBufMem.Write(extBuf.MemoryAddress, 5);

                // Release
                _bufferHelper.Free(buf.MemoryAddress);
                _externalBufferHelper.Free(extBuf.MemoryAddress);
            }
        }

        [Fact]
        private void AllocateConcurrent()
        {
            int numThreads = 100;
            var threads = new Thread[numThreads];

            for (int x = 0; x < numThreads; x++)
            {
                threads[x] = new Thread(AllocateFree); 
                threads[x].Start();
            }

            foreach (var thread in threads)
                thread.Join();
        }

        /// <summary>
        /// [Testing Purposes]
        /// Creates a buffer, then frees the memory belonging to the buffer.
        /// </summary>
        private void CreateBufferBase(MemoryBufferHelper bufferHelper)
        {
            var buffer = bufferHelper.CreateMemoryBuffer(4096);

            // Cleanup
            Internal.Testing.Buffers.FreeBuffer(buffer);
        }

        /// <summary>
        /// [Testing Purposes]
        /// Creates a buffer, then frees the memory belonging to the buffer.
        /// </summary>
        private void CreatePrivateBufferBase(MemoryBufferHelper bufferHelper)
        {
            var buffer = bufferHelper.CreatePrivateMemoryBuffer(4096);

            // Cleanup
            buffer.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="MemoryBuffer"/> and attempts to retrieve it by searching for it in memory.
        /// </summary>
        private void GetBuffers(MemoryBufferHelper bufferHelper)
        {
            // Options
            int size        = 4096;
            int repetitions = 128;
            int increment   = 2048;

            // Setup
            MemoryBuffer[] memoryBuffers = new MemoryBuffer[repetitions];

            for (int x = 0; x < repetitions; x++)
            {
                int newSize = size + (x * increment);
                memoryBuffers[x] = bufferHelper.CreateMemoryBuffer(newSize);
            }

            // Search for our buffers with exact originally given buffer sizes and try find the exact buffer.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = size + (x * increment);
                var buffers = bufferHelper.FindBuffers(newSize);

                if (!buffers.Contains(memoryBuffers[x]))
                    Assert.True(false, $"Failed to find existing buffer in memory of minimum size {newSize} bytes.");
            }

            // Cleanup.
            for (int x = 0; x < repetitions; x++)
                Internal.Testing.Buffers.FreeBuffer(memoryBuffers[x]);
        }

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        private unsafe void GetBuffersInRange(MemoryBufferHelper bufferHelper, nuint minAddress, nuint maxAddress)
        {
            /* The reason that testing the upper half is sufficient is because the buffer allocation
               functions work in such a manner that they allocate from the lowest address.
               As such, normally the only allocated addresses would be in the lower half... until enough memory is allocated to cross the upper half.
            */

            // Options
            int sizeStart = 0;    // Default page size for x86 and x64.
            int repetitions = 128;
            int increment = 4096; // Equal to allocation granularity.

            MemoryBuffer[] buffers = new MemoryBuffer[repetitions];

            // Allocate <repetitions> buffers, and try to find them all.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                buffers[x] = bufferHelper.CreateMemoryBuffer(newSize, minAddress, maxAddress);
            }

            // Validate whether each buffer is present and in range.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                var foundBuffers = bufferHelper.FindBuffers(newSize, minAddress, maxAddress);

                if (!foundBuffers.Contains(buffers[x]))
                    Assert.True(false, $"Failed to find existing buffer in memory of minimum size {newSize} bytes.");

                foreach (var buffer in foundBuffers)
                    AssertBufferInRange(buffer, minAddress, maxAddress);
            }

            // Cleanup
            for (int x = 0; x < buffers.Length; x++)
                Internal.Testing.Buffers.FreeBuffer(buffers[x]);
        }

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        private unsafe void GetBuffersInRange(MemoryBufferHelper bufferHelper, UIntPtr maxApplicationAddress)
        {
            /* The reason that testing the upper half is sufficient is because the buffer allocation
               functions work in such a manner that they allocate from the lowest address.
               As such, normally the only allocated addresses would be in the lower half... until enough memory is allocated to cross the upper half.
            */

            // Minimum address is start of upper half of 32/64 bit address range.
            // Maximum is the maximum address in 32/64 bit address range.
            long minAddress = (long)maxApplicationAddress - ((long)maxApplicationAddress / 2);
            long maxAddress = (long)maxApplicationAddress;
            GetBuffersInRange(bufferHelper, (nuint)minAddress, (nuint)maxAddress);
        }

        /// <summary>
        /// Same as <see cref="GetBuffersInRange"/>, except disables the caching when acquiring <see cref="MemoryBuffer"/>s.
        /// </summary>
        private unsafe void GetBuffersInRangeNoCache(MemoryBufferHelper bufferHelper, UIntPtr maxApplicationAddress)
        {
            /* The reason that testing the upper half is sufficient is because the buffer allocation
               functions work in such a manner that they allocate from the lowest address.
               As such, normally the only allocated addresses would be in the lower half... until enough memory is allocated to cross the upper half.
            */

            // Options
            int sizeStart = 0;    // Default page size for x86 and x64.
            int repetitions = 128;
            int increment = 4096; // Equal to allocation granularity.

            // Minimum address is start of upper half of 32/64 bit address range.
            // Maximum is the maximum address in 32/64 bit address range.
            nuint minAddress = (nuint)maxApplicationAddress - ((nuint)maxApplicationAddress / 2);
            nuint maxAddress = (nuint)maxApplicationAddress;

            MemoryBuffer[] buffers = new MemoryBuffer[repetitions];

            // Allocate <repetitions> buffers, and try to find them all.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                buffers[x] = bufferHelper.CreateMemoryBuffer(newSize, minAddress, maxAddress);
            }

            // Validate whether each buffer is present and in range.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                var foundBuffers = bufferHelper.FindBuffers(newSize, minAddress, maxAddress, false);

                if (!foundBuffers.Contains(buffers[x]))
                    Assert.True(false, $"Failed to find existing buffer in memory of minimum size {newSize} bytes.");

                foreach (var buffer in foundBuffers)
                    AssertBufferInRange(buffer, minAddress, maxAddress);
            }

            // Cleanup
            for (int x = 0; x < buffers.Length; x++)
                Internal.Testing.Buffers.FreeBuffer(buffers[x]);
        }

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        private unsafe void MemoryBufferAddGeneric(MemoryBuffer buffer, Process process)
        {
            // Setup test.
            ExternalMemory externalMemory = new ExternalMemory(process);

            // Disable item alignment.
            var bufferHeader = buffer.Properties;
            buffer.Properties = bufferHeader;

            // Get remaining space, items to place.
            int remainingBufferSpace    = bufferHeader.Remaining;
            int structSize              = Struct.GetSize<RandomIntStruct>();
            int itemsToFit              = remainingBufferSpace / structSize;

            // Generate array of random int structs.
            RandomIntStruct[] randomIntStructs = new RandomIntStruct[itemsToFit];

            for (int x = 0; x < itemsToFit; x++)
                randomIntStructs[x] = RandomIntStruct.BuildRandomStruct();

            // Fill the buffer and verify each item as it's added.
            for (int x = 0; x < itemsToFit; x++)
            {
                nuint writeAddress = buffer.Add(ref randomIntStructs[x], false, 1);

                // Read back and compare.
                externalMemory.Read(writeAddress, out RandomIntStruct actual);
                Assert.Equal(randomIntStructs[x], actual);
            }

            // Compare again, running the entire array this time.
            nuint bufferStartPtr = bufferHeader.DataPointer;
            for (int x = 0; x < itemsToFit; x++)
            {
                nuint readAddress = (UIntPtr)bufferStartPtr + (x * structSize);

                // Read back and compare.
                externalMemory.Read(readAddress, out RandomIntStruct actual);
                Assert.Equal(randomIntStructs[x], actual);
            }

            // The array is full, calling CanItemFit should return false.
            Assert.False(buffer.CanItemFit(ref randomIntStructs[0]));

            // Likewise, calling Add should return IntPtr.Zero.
            var randIntStr = RandomIntStruct.BuildRandomStruct();
            Assert.Equal((nuint)0, buffer.Add(ref randIntStr, false, 1));
        }

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>, with raw data;
        /// including the return of the correct pointer and CanItemFit.
        /// </summary>
        private unsafe void MemoryBufferAddByteArray(MemoryBuffer buffer, Process process)
        {
            // Setup test.
            ExternalMemory externalMemory = new ExternalMemory(process);

            // Disable item alignment.
            var bufferHeader = buffer.Properties;
            buffer.Properties = bufferHeader;

            // Get remaining space, items to place.
            int remainingBufferSpace = bufferHeader.Remaining;
            var randomByteArray      = RandomByteArray.GenerateRandomByteArray(remainingBufferSpace);
            byte[] rawArray          = randomByteArray.Array;

            // Fill the buffer with the whole array.
            buffer.Add(rawArray, 1);

            // Compare against the array written.
            nuint bufferStartPtr = bufferHeader.DataPointer;
            for (int x = 0; x < remainingBufferSpace; x++)
            {
                nuint readAddress = (UIntPtr)bufferStartPtr + x;

                // Read back and compare.
                externalMemory.Read(readAddress, out byte actual);
                Assert.Equal(rawArray[x], actual);
            }

            // The array is full, calling CanItemFit should return false.
            Assert.False(buffer.CanItemFit(sizeof(byte)));

            // Likewise, calling Add should return IntPtr.Zero.
            byte testByte = 55;
            Assert.Equal((nuint)0, buffer.Add(ref testByte, false, 1));
        }

        /*
         * ---------------
         * Utility Methods
         * ---------------
        */

        private MemoryBuffer CreateMemoryBuffer(MemoryBufferHelper helper)
        {
            return helper.CreateMemoryBuffer(4096);
        }

        private MemoryBuffer CreatePrivateMemoryBuffer(MemoryBufferHelper helper)
        {
            return helper.CreatePrivateMemoryBuffer(4096);
        }

        /// <summary>
        /// Asserts whether the contents of a given <see cref="MemoryBuffer"/> lie in the <see cref="minAddress"/> to <see cref="maxAddress"/> address range.
        /// </summary>
        private unsafe void AssertBufferInRange(MemoryBuffer buffer, nuint minAddress, nuint maxAddress)
        {
            nuint bufferDataPtr = buffer.Properties.DataPointer;
            if ((void*)bufferDataPtr < (void*)minAddress ||
                (void*)bufferDataPtr > (void*)maxAddress)
            {
                Assert.True(false, $"The newly allocated MemoryBuffer should lie in the {minAddress.ToString("X")} to {maxAddress.ToString("X")} range.");
            }
        }

        /// <summary>
        /// Returns the max addressable address of the process sitting behind the <see cref="MemoryBufferHelper"/>.
        /// </summary>
        private UIntPtr GetMaxAddress(MemoryBufferHelper helper, bool largeAddressAware = false)
        {
            // Is this Windows on Windows 64? (x86 app running on x64 Windows)
            IsWow64Process(helper.Process.Handle, out bool isWow64);
            GetSystemInfo(out SYSTEM_INFO systemInfo);
            long maxAddress = 0x7FFFFFFF;

            // Check for large address aware
            if (largeAddressAware && IntPtr.Size == 4 && (uint)systemInfo.lpMaximumApplicationAddress > maxAddress)
                maxAddress = (uint)systemInfo.lpMaximumApplicationAddress;

            // Check if 64bit.
            if (systemInfo.wProcessorArchitecture == ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 && !isWow64)
                maxAddress = (long)systemInfo.lpMaximumApplicationAddress;

            return (UIntPtr)maxAddress;
        }
    }
}
