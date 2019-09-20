using System;
using System.Collections.Generic;
using FluidHTN.Conditions;

namespace FluidHTN.Compounds
{
    public class Slot<TWorldStateEntry> : ITask<TWorldStateEntry>
    {
        // ========================================================= PROPERTIES

        public int SlotId { get; set; }
        public string Name { get; set; }
        public ICompoundTask<TWorldStateEntry> Parent { get; set; }
        public List<ICondition<TWorldStateEntry>> Conditions { get; } = null;
        public TaskStatus LastStatus { get; private set; }
        public ICompoundTask<TWorldStateEntry> Subtask { get; private set; } = null;

        // ========================================================= VALIDITY

        public DecompositionStatus OnIsValidFailed(IContext<TWorldStateEntry> ctx)
        {
            return DecompositionStatus.Failed;
        }

        // ========================================================= ADDERS

        public ITask<TWorldStateEntry> AddCondition(ICondition<TWorldStateEntry> condition)
        {
            throw new Exception("Slot tasks does not support conditions.");
        }

        // ========================================================= SET / REMOVE

        public bool Set(ICompoundTask<TWorldStateEntry> subtask)
        {
            if(Subtask != null)
            {
                return false;
            }

            Subtask = subtask;
            return true;
        }

        public void Clear()
        {
            Subtask = null;
        }

        // ========================================================= DECOMPOSITION

        public DecompositionStatus Decompose(IContext<TWorldStateEntry> ctx, int startIndex, out Queue<ITask<TWorldStateEntry>> result)
        {
            if(Subtask != null)
            {
                return Subtask.Decompose(ctx, startIndex, out result);
            }

            result = null;
            return DecompositionStatus.Failed;
        }

        // ========================================================= VALIDITY

        public virtual bool IsValid(IContext<TWorldStateEntry> ctx)
        {
            var result = Subtask != null;
            if (ctx.LogDecomposition) Log(ctx, $"Slot.IsValid:{(result ? "Success" : "Failed")}!", result ? ConsoleColor.Green : ConsoleColor.Red);
            return result;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext<TWorldStateEntry> ctx, string description, ConsoleColor color = ConsoleColor.White)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, color);
        }
    }
}
