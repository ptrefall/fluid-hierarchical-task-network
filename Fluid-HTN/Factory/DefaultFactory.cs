
using System.Collections.Generic;

namespace FluidHTN.Factory
{
    public sealed class DefaultFactory : IFactory
    {
        public T[] CreateArray<T>(int length)
        {
            return new T[length];
        }

        public List<T> CreateList<T>()
        {
            return new List<T>();
        }

        public Queue<T> CreateQueue<T>()
        {
            return new Queue<T>();
        }

        public bool FreeArray<T>(ref T[] array)
        {
            array = null;
            return array == null;
        }

        public bool FreeList<T>(ref List<T> list)
        {
            list = null;
            return list == null;
        }

        public bool FreeQueue<T>(ref Queue<T> queue)
        {
            queue = null;
            return queue == null;
        }

        public T Create<T>() where T : new()
        {
            return new T();
        }

        public bool Free<T>(ref T obj)
        {
            obj = default(T);
            return obj == null;
        }
    }
}
