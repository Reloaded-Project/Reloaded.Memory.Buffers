using System;
using System.Runtime.InteropServices;

namespace Reloaded.Memory.Buffers.Internal.Structs
{
    /// <summary>
    /// Sits at the top of every Reloaded buffer and identifies the buffer as Reloaded managed.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct MemoryBufferMagic
    {
        /// <summary>
        /// The initial value from which the pseudo-random sequence is generated.
        /// </summary>
        public const int InitialSeed = 1;  // Increment by one every time breaking changes are made.
        
        /// <summary>
        /// Contains the amount of integers used to store the header.
        /// </summary>
        public const int MagicCount = 16;  // 512bit header.

        /// <summary>
        /// Standard pseudo-random generated signature to mark the start of a Reloaded buffer.
        /// </summary>
        public fixed int ReloadedIdentifier[MagicCount];

        /// <summary>
        /// Generates a new buffer "magic" header; of size 256 bytes.
        /// </summary>
        /// <param name="initialize">Set this to true to create and calculate magic bytes.</param>
        public MemoryBufferMagic(bool initialize)
        {
            // We need the "magic" header to ensure that when we are looking through arbitrary page regions in memory;
            // we do not accidentally assume a non-Reloaded allocated region of memory as an Reloaded allocated region.

            // As these regions will be reused by different programs and mods; we need to ensure that they will be fairly unique.
            // This simple pseudo-random number generation algorithm will ensure at least that each buffer 
            // should have a consistent signature in the header by which it can be identified that will likely not cause
            // collisions with non-Reloaded data.

            if (initialize)
                PseudoGenerate();
        }

        /// <summary>
        /// Returns true if two of the magic sequences inside the structure are equivalent.
        /// </summary>
        public bool MagicEquals(ref MemoryBufferMagic other)
        {
            fixed (int* thisIdentifier = this.ReloadedIdentifier)
            fixed (int* otherIdentifier = other.ReloadedIdentifier)
            {
                Span<int> aBytes = new Span<int>(thisIdentifier, MagicCount);
                Span<int> bBytes = new Span<int>(otherIdentifier, MagicCount);

                for (int x = 0; x < MagicCount; x++)
                    if (aBytes[x] != bBytes[x])
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Generates a pseudo random set of bytes in the place of "Magic".
        /// </summary>
        private void PseudoGenerate()
        {
            int randomNumber = InitialSeed;
            for (int x = 0; x < MagicCount; x++)
            {
                ReloadedIdentifier[x] = randomNumber;
                randomNumber = (randomNumber + 13) * 31;
            }
        }
    }
}
