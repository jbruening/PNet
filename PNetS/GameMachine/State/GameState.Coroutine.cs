using System.Collections;
using System.Collections.Generic;

namespace PNetS
{
    static partial class GameState
    {
        /// <summary>
        /// Start a coroutine
        /// Not thread safe.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            routine.MoveNext();
            _shouldRunNextFrame.Add(routine);
            return new Coroutine(routine);
        }

        static List<IEnumerator> _unblockedCoroutines = new List<IEnumerator>();
        static List<IEnumerator> _shouldRunNextFrame = new List<IEnumerator>();

        static void RunCoroutines()
        {
            GameMachine.State.Coroutine.Run(ref _unblockedCoroutines, ref _shouldRunNextFrame);
        }
    }
}
