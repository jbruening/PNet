using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Yaml.Serialization;

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
        [YamlSerialize(YamlSerializeMethod.Never)]
        public GameObject gameObject { get; internal set; }

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

        #region coroutine

        /// <summary>
        /// Start a coroutine
        /// Not thread safe.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            routine.MoveNext();
            _shouldRunNextFrame.Add(routine);
            return new Coroutine(routine);
        }

        List<IEnumerator> _unblockedCoroutines = new List<IEnumerator>();
        List<IEnumerator> _shouldRunNextFrame = new List<IEnumerator>();

        internal void RunCoroutines()
        {
            GameMachine.State.Coroutine.Run(ref _unblockedCoroutines, ref _shouldRunNextFrame);
        }

        #endregion

        internal void Dispose()
        {
            try
            {Disposing();}
            catch(Exception e)
            {
                Debug.LogError("[Disposing {0}] {1}", gameObject.Name, e);
            }
            //help prevent bad use of the library from keeping the other components around.
            gameObject = null;
        }
        /// <summary>
        /// The object is being deleted
        /// </summary>
        protected virtual void Disposing() { }
    }
}
