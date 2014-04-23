using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PNetS
{
    partial class Room
    {
        /// <summary>
        /// Start a coroutine
        /// Not thread safe.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            _shouldRunNextFrame.Add(routine);
            return new Coroutine(routine);
        }

        List<IEnumerator> _unblockedCoroutines = new List<IEnumerator>();
        List<IEnumerator> _shouldRunNextFrame = new List<IEnumerator>();

        internal void RunCoroutines()
        {
            GameMachine.State.Coroutine.Run(ref _unblockedCoroutines, ref _shouldRunNextFrame);
        }
    }
}