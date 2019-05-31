using System.Collections.Generic;
using FluidHTN.Conditions;

namespace FluidHTN.Compounds
{
    public abstract class CompoundTask : ICompoundTask
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = new List<ICondition>();
        public TaskStatus LastStatus { get; private set; }
        public List<ITask> Subtasks { get; } = new List<ITask>();

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public ICompoundTask AddSubtask(ITask subtask)
        {
            Subtasks.Add(subtask);
            return this;
        }

        // ========================================================= DECOMPOSITION

        public DecompositionStatus Decompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            return OnDecompose(ctx, startIndex, out result);
        }

        protected abstract DecompositionStatus OnDecompose(IContext ctx, int startIndex, out Queue<ITask> result);

        protected abstract DecompositionStatus OnDecomposeTask(IContext ctx, ITask task, int taskIndex, int[] oldStackDepth, out Queue<ITask> result);

        protected abstract DecompositionStatus OnDecomposeCompoundTask(IContext ctx, ICompoundTask task, int taskIndex, int[] oldStackDepth, out Queue<ITask> result);

        // ========================================================= VALIDITY

        public virtual bool IsValid(IContext ctx)
        {
            foreach (var condition in Conditions)
                if (condition.IsValid(ctx) == false)
                    return false;

            return true;
        }
    }
}
