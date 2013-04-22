using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PNet
{
    /// <summary>
    /// provides a dictionary where the key is an integer, but provides array-time lookups
    /// TODO: implement a trimming function
    /// TODO: implemnt ICollections interface
    /// </summary>
    /// <remarks>
    /// Benefits: access speed is identical to an array
    /// Downsides: 
    /// collection takes up more memory than a dictionary of the same size of objects
    /// Of note: as this takes up as much memory as the last index + bool array the same size, this should probably be used for small collections.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class IntDictionary<T> : IEnumerable
    {

        List<T> m_Collection;
        List<bool> hasValueCollection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collectionSize">initial size of the internal collection</param>
        public IntDictionary(int collectionSize = 32)
        {
            // TODO: Complete member initialization
            m_Collection = new List<T>(collectionSize);
            hasValueCollection = new List<bool>(collectionSize);
        }

        /// <summary>
        /// returns the values stored in the dictionary. keys where there is no value will be return as default(T)
        /// </summary>
        /// <returns></returns>
        public T[] Values
        {
            get
            {
                return m_Collection.ToArray();
            }
        }
        /// <summary>
        /// get an array for the size of the internal array, that says if a value is contained for that key
        /// </summary>
        public bool[] HasValues
        {
            get
            {
                return hasValueCollection.ToArray();
            }
        }


        /// <summary>
        /// adds the object to the collection, and returns the key that was assigned to the object
        /// </summary>
        /// <param name="add"></param>
        /// <returns></returns>
        public int Add(T add)
        {
            //get the first null index
            var index = hasValueCollection.FindIndex(c => c == false);
            if (index != -1)
            {
                m_Collection[index] = add;
                hasValueCollection[index] = true;
                return index;
            }
            m_Collection.Add(add);
            hasValueCollection.Add(true);
            return m_Collection.Count - 1;
        }

        /// <summary>
        /// NOT RECOMMENDED.
        /// This will overwrite the previous value if the key already exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="add"></param>
        public void Add(int key, T add)
        {
            if (m_Collection.Count <= key)
            {
                //collections not big enough. need to resize.
                var range = key - m_Collection.Count + 1;
                m_Collection.AddRange(Enumerable.Repeat<T>(default(T), range));
                hasValueCollection.AddRange(Enumerable.Repeat<bool>(false, range));

                m_Collection[key] = add;
                hasValueCollection[key] = true;
            }
            else
            {
                m_Collection[key] = add;
                hasValueCollection[key] = true;
            }
        }

        /// <summary>
        /// remove the item at the specified key
        /// </summary>
        /// <param name="key"></param>
        public void Remove(int key)
        {
            if (key < m_Collection.Count)
            {
                m_Collection[key] = default(T);
                hasValueCollection[key] = false;
            }
        }

        /// <summary>
        /// does a value exist for the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasValue(int key)
        {
            if (key < m_Collection.Count)
            {
                return hasValueCollection[key];
            }
            return false;
        }
        /// <summary>
        /// value for a specified key. If there has no value there, it will return default(T)
        /// You should probably use TryGetValue.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (index < m_Collection.Count)
                    return m_Collection[index];
                return default(T);
            }
            set
            {
                Add(index, value);
            }
        }

        /// <summary>
        /// Try to get the value associated with the key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(int key, out T value)
        {
            if (key < m_Collection.Count)
            {
                value = m_Collection[key];
                return hasValueCollection[key];
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// size of the collection array. can be used as upper bound for enumeration
        /// </summary>
        public int Capacity { get { return m_Collection.Count; } }

        

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }
        /// <summary>
        /// get the enumerator to enumerate the collection
        /// </summary>
        /// <returns></returns>
        public IntDictionaryEnumerator<T> GetEnumerator()
        {
            return new IntDictionaryEnumerator<T>(m_Collection);
        }

        // Defines the enumerator for the Boxes collection.
        // (Some prefer this class nested in the collection class.)
        /// <summary>
        /// enumerator helper
        /// </summary>
        /// <typeparam name="U"></typeparam>
        public class IntDictionaryEnumerator<U> : IEnumerator<U>
        {
            private U currentT;
            private int curIndex;

            private   List<U> m_Collection;

            /// <summary>
            /// create a new enumerator helper for the intdictionary
            /// </summary>
            /// <param name="m_Collection"></param>
            public    IntDictionaryEnumerator(List<U> m_Collection)
            {
                // TODO: Complete member initialization
                this.m_Collection = m_Collection;
                curIndex = -1;
                currentT = default(U);
            }

            /// <summary>
            /// go to the next iem in the collection
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                //Avoids going beyond the end of the collection.
                if (++curIndex >= m_Collection.Count)
                {
                    return false;
                }
                else
                {
                    // Set current box to next item in collection.
                    currentT = m_Collection[curIndex];
                }
                return true;
            }

            /// <summary>
            /// reset the position of the enumerator
            /// </summary>
            public void Reset() { curIndex = -1; }

            void IDisposable.Dispose() { }

            /// <summary>
            /// current item in the enumeration
            /// </summary>
            public U Current
            {
                get { return currentT; }
            }


            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
