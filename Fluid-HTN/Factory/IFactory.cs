
using System.Collections.Generic;

namespace FluidHTN.Factory
{
    public interface IFactory
    {
        T[] CreateArray<T>(int length);
        bool FreeArray<T>(ref T[] array);

        Queue<T> CreateQueue<T>();
        bool FreeQueue<T>(ref Queue<T> queue);

        List<T> CreateList<T>();
        bool FreeList<T>(ref List<T> list);

        T Create<T>() where T : new();
        bool Free<T>(ref T obj);
    }
}
