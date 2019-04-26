using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Reloaded.Memory.Buffers.Internal.Utilities
{
    internal static class MutexObtainer
    {
        /// <summary>
        /// Opens an existing mutex with a specified name if exists. Otherwise creates a new Mutex.
        /// </summary>
        /// <param name="mutexName">The name of the mutex.</param>
        /// <returns>An instance of the Mutex111</returns>
        internal static System.Threading.Mutex MakeMutex(String mutexName)
        {
            try
            {
                return System.Threading.Mutex.OpenExisting(mutexName);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return new System.Threading.Mutex(false, mutexName);
            }
        }
    }
}
