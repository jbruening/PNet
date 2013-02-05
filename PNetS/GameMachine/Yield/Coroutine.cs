using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PNetS
{
    /// <summary>
    /// Used for yielding yields via StartCoroutine. should support infinite depth.
    /// </summary>
    public sealed class Coroutine : YieldInstruction
    {
        internal IEnumerator<YieldInstruction> routine;

        internal Coroutine(IEnumerator<YieldInstruction> routine)
        {
            this.routine = routine;
        }

        /// <summary>
        /// method that says if the YieldInstruction is done
        /// </summary>
        public override bool IsDone
        {
            get
            {
                if (routine.Current != null && routine.Current.IsDone)
                    return true;
                
                //if the routine finishes, then we need to say we're finished
                return !routine.MoveNext();
            }
        }
    }
}
