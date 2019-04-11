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
        public List<ITask> Children { get; } = new List<ITask>();

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public ICompoundTask AddChild(ITask child)
        {
            Children.Add(child);
            return this;
        }

        // ========================================================= DECOMPOSITION

        public Queue<ITask> Decompose(IContext ctx, int startIndex)
        {
            return OnDecompose(ctx, startIndex);
        }

        protected abstract Queue<ITask> OnDecompose(IContext ctx, int startIndex);

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