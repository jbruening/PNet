using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;

namespace PNetS
{
    /// <summary>
    /// Base class for components that can attach to GameObjects
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        /// The gameobject this is attached to. cached result.
        /// </summary>
        public GameObject gameObject { get; internal set; }

        /// <summary>
        /// in order to actually start a coroutine chain, you need to set IsRootRoutine to true on the first call in a coroutine call chain.
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="IsRootRoutine"></param>
        /// <returns></returns>
        public Coroutine StartCoroutine(IEnumerator<YieldInstruction> routine, bool IsRootRoutine = false)
        {
            if (IsRootRoutine)
            {
                GameState.AddRoutine(routine);
                rootRoutines.Add(routine);
            }
            return new Coroutine(routine);
        }

        internal List<IEnumerator<YieldInstruction>> rootRoutines = new List<IEnumerator<YieldInstruction>>();

        /// <summary>
        /// Get the first component on the gameObject of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComponent<T>() 
            where T : Component
        {
            return gameObject.GetComponent<T>();
        }

        /// <summary>
        /// Get all components of the type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetComponents<T>()
            where T : Component
        {
            return gameObject.GetComponents<T>();
        }

        internal void Dispose()
        {
            Disposing();
            foreach (var routine in rootRoutines)
            {
                GameState.RemoveRoutine(routine);
            }

            rootRoutines = null;
        }
        /// <summary>
        /// The object is being deleted
        /// </summary>
        protected virtual void Disposing() { }
    }
}
