using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;

namespace FluidHTN
{
    public class PausePlanTask<TWorldStateEntry> : ITask<TWorldStateEntry>
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask<TWorldStateEntry> Parent { get; set; }
        public List<ICondition<TWorldStateEntry>> Conditions { get; } = null;
        public List<IEffect<TWorldStateEntry>> Effects { get; } = null;
        public TaskStatus LastStatus { get; }

        // ========================================================= VALIDITY

        public DecompositionStatus OnIsValidFailed(IContext<TWorldStateEntry> ctx)
        {
            return DecompositionStatus.Failed;
        }

        // ========================================================= ADDERS

        public ITask<TWorldStateEntry> AddCondition(ICondition<TWorldStateEntry> condition)
        {
            throw new Exception("Pause Plan tasks does not support conditions.");
        }

        public ITask<TWorldStateEntry> AddEffect(IEffect<TWorldStateEntry> effect)
        {
            throw new Exception("Pause Plan tasks does not support effects.");
        }

        // ========================================================= FUNCTIONALITY

        public void ApplyEffects(IContext<TWorldStateEntry> ctx)
        {
        }

        // ========================================================= VALIDITY

        public bool IsValid(IContext<TWorldStateEntry> ctx)
        {
            if (ctx.LogDecomposition) Log(ctx, $"PausePlanTask.IsValid:Success!");
            return true;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext<TWorldStateEntry> ctx, string description)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, ConsoleColor.Green);
        }
    }
}
