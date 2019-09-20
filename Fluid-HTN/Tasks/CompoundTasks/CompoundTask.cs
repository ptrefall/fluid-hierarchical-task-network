using System;
using System.Collections.Generic;
using FluidHTN.Conditions;

namespace FluidHTN.Compounds
{
    public abstract class CompoundTask<TWorldStateEntry> : ICompoundTask<TWorldStateEntry>
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask<TWorldStateEntry> Parent { get; set; }
        public List<ICondition<TWorldStateEntry>> Conditions { get; } = new List<ICondition<TWorldStateEntry>>();
        public TaskStatus LastStatus { get; private set; }
        public List<ITask<TWorldStateEntry>> Subtasks { get; } = new List<ITask<TWorldStateEntry>>();

        // ========================================================= VALIDITY

        public virtual DecompositionStatus OnIsValidFailed(IContext<TWorldStateEntry> ctx)
        {
            return DecompositionStatus.Failed;
        }

        // ========================================================= ADDERS

        public ITask<TWorldStateEntry> AddCondition(ICondition<TWorldStateEntry> condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public ICompoundTask<TWorldStateEntry> AddSubtask(ITask<TWorldStateEntry> subtask)
        {
            Subtasks.Add(subtask);
            return this;
        }

        // ========================================================= DECOMPOSITION

        public DecompositionStatus Decompose(IContext<TWorldStateEntry> ctx, int startIndex, out Queue<ITask<TWorldStateEntry>> result)
        {
            if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth++;
            var status = OnDecompose(ctx, startIndex, out result);
            if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth--;
            return status;
        }

        protected abstract DecompositionStatus OnDecompose(IContext<TWorldStateEntry> ctx, int startIndex, out Queue<ITask<TWorldStateEntry>> result);

        protected abstract DecompositionStatus OnDecomposeTask(IContext<TWorldStateEntry> ctx, ITask<TWorldStateEntry> task, int taskIndex, int[] oldStackDepth, out Queue<ITask<TWorldStateEntry>> result);

        protected abstract DecompositionStatus OnDecomposeCompoundTask(IContext<TWorldStateEntry> ctx, ICompoundTask<TWorldStateEntry> task, int taskIndex, int[] oldStackDepth, out Queue<ITask<TWorldStateEntry>> result);

        protected abstract DecompositionStatus OnDecomposeSlot(IContext<TWorldStateEntry> ctx, Slot<TWorldStateEntry> task, int taskIndex, int[] oldStackDepth, out Queue<ITask<TWorldStateEntry>> result);

        // ========================================================= VALIDITY

        public virtual bool IsValid(IContext<TWorldStateEntry> ctx)
        {
            foreach (var condition in Conditions)
            {
                var result = condition.IsValid(ctx);
                if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.IsValid:{(result ? "Success" : "Failed")}:{condition.Name} is{(result ? "" : " not")} valid!", result ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
                if (result == false)
                {
                    return false;
                }
            }

            return true;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext<TWorldStateEntry> ctx, string description, ConsoleColor color = ConsoleColor.White)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, color);
        }
    }
}
