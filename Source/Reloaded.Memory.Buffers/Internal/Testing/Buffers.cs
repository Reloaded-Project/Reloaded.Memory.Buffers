namespace Reloaded.Memory.Buffers.Internal.Testing
{
    /// <summary>
    /// FOR TESTING USE ONLY, PLEASE DO NOT USE.
    /// </summary>
    public static class Buffers
    {
        /// <summary>
        /// [FOR TESTING USE ONLY]
        /// Frees the region of pages that backs an individual <see cref="MemoryBuffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to free.</param>
        /// <returns></returns>
        public static bool FreeBuffer(MemoryBuffer buffer)
        {
            return buffer.MemorySource.Free(buffer.AllocationAddress);
        }
    }
}
