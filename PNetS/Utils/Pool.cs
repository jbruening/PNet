using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS.Utils
{
    class Pool<T> where T:IPoolItem
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _ctor;

        public Pool(Func<T> ctorFunc)
        {
            _ctor = ctorFunc;
            _pool = new Stack<T>();
        }

        public int MaxPoolSize;

        public T GetItem()
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                    return _pool.Pop();
            }
            return _ctor();
        }

        public void Recycle(T item)
        {
            item.Reset();
            lock (_pool)
            {
                if (_pool.Count < MaxPoolSize)
                {
                    _pool.Push(item);
                }
            }
        }
    }
}
