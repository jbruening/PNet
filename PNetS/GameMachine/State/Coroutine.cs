using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PNetS.GameMachine.State
{
    static class Coroutine
    {
        /// <summary>
        /// standardized behaviour to run through coroutines
        /// </summary>
        /// <param name="unblockedCoroutines"></param>
        /// <param name="shouldRunNextFrame"></param>
        public static void Run(ref List<IEnumerator> unblockedCoroutines, ref List<IEnumerator> shouldRunNextFrame)
        {
            foreach (IEnumerator coroutine in unblockedCoroutines)
            {
                var yRoute = coroutine.Current as YieldInstruction;
                if (yRoute != null)
                {
                    //yielding on a yieldinstruction
                    //running IsDone is equivilent to MoveNext for yieldinstructions
                    if (!yRoute.IsDone())
                    {
                        shouldRunNextFrame.Add(coroutine);
                        continue;
                    }
                }

                //everything else...
                if (!coroutine.MoveNext())
                    // This coroutine has finished
                    continue;

                yRoute = coroutine.Current as YieldInstruction;
                if (yRoute is PNetS.Coroutine)
                {
                    //remove the routine we just made from the top of the stack...
                    var croute = yRoute as PNetS.Coroutine;
                    lock (shouldRunNextFrame)
                    {
                        var last = shouldRunNextFrame.Last();
                        if (!ReferenceEquals(croute.Routine, last))
                        {
                            Debug.LogError(
                                "Something went wrong when yielding on a coroutine. Are you using coroutines in other threads?");
                            continue;
                        }
                        shouldRunNextFrame.RemoveAt(shouldRunNextFrame.Count - 1);

                        //add the outer so that it can call the inner.
                        shouldRunNextFrame.Add(coroutine);
                    }
                    continue;
                }

                if (yRoute == null)
                {
                    // This coroutine yielded null, or some other value we don't understand; run it next frame.
                    shouldRunNextFrame.Add(coroutine);
                    continue;
                }

                if (!yRoute.IsDone())
                {
                    shouldRunNextFrame.Add(coroutine);
                }
            }

            unblockedCoroutines = shouldRunNextFrame;
            shouldRunNextFrame = new List<IEnumerator>();
        }
    }
}
