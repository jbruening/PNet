using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// Waiting threads, basically.
    /// </summary>
    public abstract class YieldInstruction
    {
        /// <summary>
        /// If the coroutine has finished
        /// </summary>
        public abstract bool IsDone { get; }
    }
}
