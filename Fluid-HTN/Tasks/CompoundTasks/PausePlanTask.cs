using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;

namespace FluidHTN
{
    public class PausePlanTask : ITask
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = null;
        public List<IEffect> Effects { get; } = null;
        public TaskStatus LastStatus { get; }

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            throw new Exception("Pause Plan tasks does not support conditions.");
        }

        public ITask AddEffect(IEffect effect)
        {
            throw new Exception("Pause Plan tasks does not support effects.");
        }

        // ========================================================= FUNCTIONALITY

        public void ApplyEffects(IContext ctx)
        {
        }

        // ========================================================= VALIDITY

        public bool IsValid(IContext ctx)
        {
            return true;
        }
    }
}