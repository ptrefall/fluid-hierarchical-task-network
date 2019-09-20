
using System.Collections.Generic;

namespace FluidHTN.Compounds
{
    public interface ICompoundTask<TWorldStateEntry> : ITask<TWorldStateEntry>
    {
        List<ITask<TWorldStateEntry>> Subtasks { get; }
        ICompoundTask<TWorldStateEntry> AddSubtask(ITask<TWorldStateEntry> subtask);

        /// <summary>
        ///     Decompose the task onto the tasks to process queue, mind it's depth first
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        DecompositionStatus Decompose(IContext<TWorldStateEntry> ctx, int startIndex, out Queue<ITask<TWorldStateEntry>> result);
    }
}
