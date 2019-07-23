using System;
using System.Collections.Generic;
using FluidHTN.Conditions;

namespace FluidHTN.Compounds
{
    public class Slot : ITask
    {
        // ========================================================= PROPERTIES

        public int SlotId { get; set; }
        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = null;
        public TaskStatus LastStatus { get; private set; }
        public ICompoundTask Subtask { get; private set; } = null;

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            throw new Exception("Slot tasks does not support conditions.");
        }

        // ========================================================= SET / REMOVE

        public bool Set(ICompoundTask subtask)
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

        public DecompositionStatus Decompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            if(Subtask != null)
            {
                return Subtask.Decompose(ctx, startIndex, out result);
            }

            result = null;
            return DecompositionStatus.Failed;
        }

        // ========================================================= VALIDITY

        public virtual bool IsValid(IContext ctx)
        {
            var result = Subtask != null;
            if (ctx.LogDecomposition) Log(ctx, $"Slot.IsValid:{(result ? "Success" : "Failed")}!", result ? ConsoleColor.Green : ConsoleColor.Red);
            return result;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext ctx, string description, ConsoleColor color = ConsoleColor.White)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, color);
        }
    }
}
