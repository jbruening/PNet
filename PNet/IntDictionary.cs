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
    public class IntDictionary<T> : IEnumerable, IEnumerable<T>
    {
        readonly List<T> _collection;
        readonly List<bool> _hasValueCollection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collectionSize">initial size of the internal collection</param>
        public IntDictionary(int collectionSize = 32)
        {
            // TODO: Complete member initialization
            _collection = new List<T>(collectionSize);
            _hasValueCollection = new List<bool>(collectionSize);
        }

        /// <summary>
        /// returns the values stored in the dictionary. keys where there is no value will be return as default(T)
        /// </summary>
        /// <returns></returns>
        public T[] Values
        {
            get
            {
                return _collection.ToArray();
            }
        }
        /// <summary>
        /// get an array for the size of the internal array, that says if a value is contained for that key
        /// </summary>
        public bool[] HasValues
        {
            get
            {
                return _hasValueCollection.ToArray();
            }
        }

        //not using stack because of need to remove from somewhere in the middle
        readonly List<int> _keyReuse = new List<int>();

        /// <summary>
        /// adds the object to the collection, and returns the key that was assigned to the object
        /// </summary>
        /// <param name="add"></param>
        /// <returns></returns>
        public int Add(T add)
        {
            var index = -1;

            if (_keyReuse.Count > 0)
            {
                //pop
                index = _keyReuse[_keyReuse.Count - 1];
                _keyReuse.RemoveAt(_keyReuse.Count - 1);
            }

            if (index != -1)
            {
                _collection[index] = add;
                _hasValueCollection[index] = true;
                return index;
            }
            _collection.Add(add);
            _hasValueCollection.Add(true);
            return _collection.Count - 1;
        }

        /// <summary>
        /// remove the item at the specified key
        /// </summary>
        /// <param name="key"></param>
        public void Remove(int key)
        {
            if (key < _collection.Count)
            {
                _collection[key] = default(T);

                var hadValue = _hasValueCollection[key];
                if (hadValue)
                {
                    //push
                    _keyReuse.Add(key);
                }

                _hasValueCollection[key] = false;
            }
        }

        /// <summary>
        /// clear out all keys/values
        /// </summary>
        public void Clear()
        {
            _collection.Clear();
            _hasValueCollection.Clear();
            _keyReuse.Clear();
        }

        /// <summary>
        /// NOT RECOMMENDED.
        /// This will overwrite the previous value if the key already exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="add"></param>
        public void Add(int key, T add)
        {
            if (_collection.Count <= key)
            {
                //collections not big enough. need to resize.
                var range = key - _collection.Count + 1;
                _collection.AddRange(Enumerable.Repeat<T>(default(T), range));
                _hasValueCollection.AddRange(Enumerable.Repeat<bool>(false, range));

                _collection[key] = add;
                _hasValueCollection[key] = true;
            }
            else
            {
                if (_keyReuse.Contains(key))
                    _keyReuse.Remove(key);
                _collection[key] = add;
                _hasValueCollection[key] = true;
            }
        }

        /// <summary>
        /// does a value exist for the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasValue(int key)
        {
            if (key < _collection.Count)
            {
                return _hasValueCollection[key];
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
                if (index < _collection.Count)
                    return _collection[index];
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
            if (key < _collection.Count)
            {
                value = _collection[key];
                return _hasValueCollection[key];
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// size of the collection array. can be used as upper bound for enumeration
        /// </summary>
        public int Capacity { get { return _collection.Count; } }

        System.Collections.IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T>  GetEnumerator()
        {
            for(int i = 0; i < _collection.Count; i++)
            {
                if (_hasValueCollection[i])
                    yield return _collection[i];
            }
        }
    }
}
