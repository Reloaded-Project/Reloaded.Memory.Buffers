﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Reloaded.Memory.Kernel32.Kernel32;

namespace Reloaded.Memory.Buffers.Internal
{
    /// <summary>
    /// Utility class which searches for existing <see cref="MemoryBuffer"/>s
    /// in a process with support for caching already found buffers.
    /// </summary>
    internal class MemoryBufferSearcher
    {
        /// <summary> Maintains address to buffer mappings. </summary>
        private ConcurrentDictionary<nuint, MemoryBuffer> _bufferCache = new();

        /// <summary> The process in which the buffers are being searched for. </summary>
        private Process _process;

        /// <summary>
        /// Creates a <see cref="MemoryBufferSearcher"/> that can search for
        /// existing Reloaded <see cref="MemoryBuffer"/>s within a given <see cref="Process"/>.
        /// </summary>
        /// <param name="targetProcess">The process in which to search for buffers.</param>
        internal MemoryBufferSearcher(Process targetProcess)
        {
            _process = targetProcess;
        }

        /// <summary>
        /// Adds a new <see cref="MemoryBuffer"/> to the internal buffer cache.
        /// </summary>
        /// <param name="buffer">The buffer which to add to cache.</param>
        internal void AddBuffer(MemoryBuffer buffer)
        {
            _bufferCache[buffer.AllocationAddress] = buffer;
        }

        /// <summary>
        /// Scans the whole process for buffers and returns a list of found buffers.
        /// </summary>
        /// <remarks>Running this function updates the internal module cache.</remarks>
        /// <returns>A list of available <see cref="MemoryBuffer"/>s to be used.</returns>
        internal MemoryBuffer[] FindBuffers()
        {
            // Get a list of all pages.
            var memoryBasicInformation = MemoryPages.GetPages(_process);
            List<MemoryBuffer> buffers = new List<MemoryBuffer>();

            // Check if each page is the start of a buffer, and add it conditionally.
            for (int x = 0; x < memoryBasicInformation.Count; x++)
            {
                if (memoryBasicInformation[x].State == (uint)(MEM_ALLOCATION_TYPE.MEM_COMMIT) &&
                    memoryBasicInformation[x].Type == (uint)MEM_ALLOCATION_TYPE.MEM_PRIVATE &&
                    memoryBasicInformation[x].Protect == (uint)MEM_PROTECTION.PAGE_EXECUTE_READWRITE &&
                    MemoryBufferFactory.IsBuffer(_process, memoryBasicInformation[x].BaseAddress))
                {
                    var address = memoryBasicInformation[x].BaseAddress;
                    MemoryBuffer buffer = MemoryBufferFactory.FromAddress(_process, address);
                    AddBuffer(buffer);
                    buffers.Add(buffer);
                }
            }

            return buffers.ToArray();
        }


        /// <summary>
        /// Returns a list of buffers that satisfy the passed in size requirements.
        /// </summary>
        /// <param name="size">The amount of bytes a buffer must have minimum.</param>
        /// <param name="useCache">
        ///     If this flag is set to true, the searcher will try to return a buffer in its cached list of buffers.
        ///     If one or more buffer meeting the size requirements is found in the cache, an array of found buffers will be returned.
        ///     If no buffer has been found in the cache, the function will scan the whole process for buffers and return the found
        ///     set of buffers which satisfy the size parameter.
        ///     However this may not find the buffers that have been added since the last time this function has called.</param>
        /// <param name="dontReturnLockedBuffers"></param>
        /// <returns></returns>
        internal MemoryBuffer[] GetBuffers(int size, bool useCache = true, bool dontReturnLockedBuffers = true)
        {
            // Return the already known buffers if there is a buffer that can fit the size.
            if (useCache)
            {
                var cachedBuffers = _bufferCache.Values.Where(x => x.CanItemFit(size) && !x.IsLocked()).ToArray();
                if (cachedBuffers.Length > 0)
                    return cachedBuffers;
            }

            // No cached buffers meet the criteria; find a set of new buffers.
            var newBuffers = FindBuffers();

            // Retry finding buffers that meet requirements.
            if (useCache)
            {
                var memoryBuffers = _bufferCache.Values.Where(x => x.CanItemFit(size)).ToArray();
                return memoryBuffers;
            }
            else
            {
                var memoryBuffers = newBuffers.Where(x => x.CanItemFit(size)).ToArray();
                return memoryBuffers;
            }
        }
    }
}