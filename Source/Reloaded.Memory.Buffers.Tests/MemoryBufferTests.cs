using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Reloaded.Memory.Buffers.Tests.Helpers;
using Reloaded.Memory.Sources;
using Vanara.PInvoke;
using Xunit;

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

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void MemoryBufferAddInternal() => MemoryBufferAdd(_bufferHelper);

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        [Fact]
        private void MemoryBufferAddExternal() => MemoryBufferAdd(_externalBufferHelper);

        /*
         * ----------
         * Core Tests
         * ----------
        */


        /// <summary>
        /// [Testing Purposes]
        /// Creates a buffer, then frees the memory belonging to the buffer.
        /// </summary>
        private void CreateBufferBase(MemoryBufferHelper bufferHelper)
        {
            var buffer = bufferHelper.CreateMemoryBuffer(4096);

            // Cleanup
            Testing.Buffers.FreeBuffer(buffer);
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
                var buffers = bufferHelper.GetBuffers(newSize);

                if (!buffers.Contains(memoryBuffers[x]))
                    Assert.True(false, $"Failed to find existing buffer in memory of minimum size {newSize} bytes.");
            }

            // Cleanup.
            for (int x = 0; x < repetitions; x++)
                Testing.Buffers.FreeBuffer(memoryBuffers[x]);
        }

        /// <summary>
        /// Attempts to create a set of <see cref="MemoryBuffer"/>s at the beginning and end of the
        /// address space, and then find the given buffers.
        /// </summary>
        private unsafe void GetBuffersInRange(MemoryBufferHelper bufferHelper, IntPtr maxApplicationAddress)
        {
            /* The reason that testing the upper half is sufficient is because the buffer allocation
               functions work in such a manner that they allocate from the lowest address.
               As such, normally the only allocated addresses would be in the lower half... until enough memory is allocated to cross the upper half.
            */

            // Options
            int sizeStart   = 0;    // Default page size for x86 and x64.
            int repetitions = 128;
            int increment   = 4096; // Equal to allocation granularity.

            // Minimum address is start of upper half of 32/64 bit address range.
            // Maximum is the maximum address in 32/64 bit address range.
            long minAddress = (long) maxApplicationAddress - ((long)maxApplicationAddress / 2);
            long maxAddress = (long) maxApplicationAddress;

            MemoryBuffer[] buffers = new MemoryBuffer[repetitions];

            // Allocate <repetitions> buffers, and try to find them all.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                buffers[x] = bufferHelper.CreateMemoryBuffer(newSize, (long) minAddress, (long) maxAddress);
            }

            // Validate whether each buffer is present and in range.
            for (int x = 0; x < repetitions; x++)
            {
                int newSize = sizeStart + (x * increment);
                var foundBuffers = bufferHelper.GetBuffers(newSize, (IntPtr) minAddress, (IntPtr) maxAddress);

                if (!foundBuffers.Contains(buffers[x]))
                    Assert.True(false, $"Failed to find existing buffer in memory of minimum size {newSize} bytes.");

                foreach (var buffer in foundBuffers)
                    AssertBufferInRange(buffer, (IntPtr) minAddress, (IntPtr) maxAddress);
            }

            // Cleanup
            for (int x = 0; x < buffers.Length; x++)
                Testing.Buffers.FreeBuffer(buffers[x]);
        }

        /// <summary>
        /// Tests the "Add" functionality of the <see cref="MemoryBuffer"/>; including
        /// the return of the correct pointer and CanItemFit.
        /// </summary>
        private void MemoryBufferAdd(MemoryBufferHelper bufferHelper)
        {
            // Setup test.
            ExternalMemory externalMemory = new ExternalMemory(bufferHelper.Process);
            var buffer = bufferHelper.CreateMemoryBuffer(1000);

            // Disable item alignment.
            var bufferHeader = buffer.BufferHeader;
            bufferHeader.SetAlignment(1);
            buffer.BufferHeader = bufferHeader;

            // Get remaining space, generate random byte array.
            int remainingBufferSpace = buffer.BufferHeader.Remaining;
            var randomByteArray = RandomByteArray.GenerateRandomByteArray(remainingBufferSpace);

            // Fill the buffer with the random byte array and verify each item as it's added.
            for (int x = 0; x < remainingBufferSpace; x++)
            {
                IntPtr writeAddress = buffer.Add(ref randomByteArray.Array[x]);

                // Read back and compare.
                externalMemory.Read(writeAddress, out byte actual);
                Assert.Equal(randomByteArray.Array[x], actual);
            }

            // Compare again, running the entire array this time.
            IntPtr bufferStartPtr = buffer.BufferHeader.DataPointer;
            for (int x = 0; x < remainingBufferSpace; x++)
            {
                IntPtr readAddress = bufferStartPtr + x;

                // Read back and compare.
                externalMemory.Read(readAddress, out byte actual);
                Assert.Equal(randomByteArray.Array[x], actual);
            }

            // The array is full, calling CanItemFit should return false.
            Assert.False(buffer.CanItemFit(1));

            // Likewise, calling Add should return IntPtr.Zero.
            byte miscByte = 55;
            Assert.Equal(IntPtr.Zero, buffer.Add(ref miscByte));
        }

        /*
         * ---------------
         * Utility Methods
         * ---------------
        */

        /// <summary>
        /// Asserts whether the contents of a given <see cref="MemoryBuffer"/> lie in the <see cref="minAddress"/> to <see cref="maxAddress"/> address range.
        /// </summary>
        private unsafe void AssertBufferInRange(MemoryBuffer buffer, IntPtr minAddress, IntPtr maxAddress)
        {
            IntPtr bufferDataPtr = buffer.BufferHeader.DataPointer;
            if ((void*)bufferDataPtr < (void*)minAddress ||
                (void*)bufferDataPtr > (void*)maxAddress)
            {
                Assert.True(false, $"The newly allocated MemoryBuffer should lie in the {minAddress.ToString("X")} to {maxAddress.ToString("X")} range.");
            }
        }

        /// <summary>
        /// Returns the max addressable address of the process sitting behind the <see cref="MemoryBufferHelper"/>.
        /// </summary>
        private IntPtr GetMaxAddress(MemoryBufferHelper helper)
        {
            // Is this Windows on Windows 64? (x86 app running on x64 Windows)
            Kernel32.IsWow64Process(helper.Process.Handle, out bool isWow64);
            Kernel32.GetSystemInfo(out Kernel32.SYSTEM_INFO systemInfo);
            long maxAddress = 0xFFFFFFFF;

            // Check if 64bit.
            if (systemInfo.wProcessorArchitecture == ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 && !isWow64)
                maxAddress = (long)systemInfo.lpMaximumApplicationAddress;

            return (IntPtr) maxAddress;
        }
    }
}
