using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN
{
    public interface IDomain
    {
        TaskRoot Root { get; }
        void Add(ICompoundTask parent, ITask subtask);
        void Add(ICompoundTask parent, Slot slot);
    }
}
