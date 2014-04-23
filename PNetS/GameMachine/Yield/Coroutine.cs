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
        internal IEnumerator Routine;
        private bool innerFinished = false;

        internal Coroutine(IEnumerator routine)
        {
            Routine = routine;
        }

        /// <summary>
        /// method that says if the YieldInstruction is done
        /// </summary>
        public override bool IsDone()
        {
            if (innerFinished) return true;

            if (Routine.Current != null)
            {
                if (Routine.Current is YieldInstruction)
                {
                    if ((Routine.Current as YieldInstruction).IsDone())
                    {
                        innerFinished = true;
                        return false;
                    }
                }
            }
                
            //if the routine finishes, then we need to say we're finished
            innerFinished = !Routine.MoveNext();
            return false;
        }
    }
}
