using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
    public class PrimitiveTask : IPrimitiveTask
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = new List<ICondition>();
        public List<ICondition> ExecutingConditions { get; } = new List<ICondition>();
        public TaskStatus LastStatus { get; }
        public IOperator Operator { get; private set; }
        public List<IEffect> Effects { get; } = new List<IEffect>();

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public ITask AddExecutingCondition(ICondition condition)
        {
            ExecutingConditions.Add(condition);
            return this;
        }

        public ITask AddEffect(IEffect effect)
        {
            Effects.Add(effect);
            return this;
        }

        // ========================================================= SETTERS

        public void SetOperator(IOperator action)
        {
            if (Operator != null) throw new Exception("A Primitive Task can only contain a single Operator!");

            Operator = action;
        }

        // ========================================================= FUNCTIONALITY

        public void ApplyEffects(IContext ctx)
        {
            foreach (var effect in Effects)
            {
                if (ctx.ContextState == ContextState.Planning)
                {
                    ctx.TryLogDecomposition(Name, "ApplyEffects", effect);
                }

                effect.Apply(ctx);
            }
        }

        public void Stop(IContext ctx)
        {
            Operator?.Stop(ctx);
        }

        // ========================================================= VALIDITY

        public bool IsValid(IContext ctx)
        {
            foreach (var condition in Conditions)
            {
                var result = condition.IsValid(ctx);

                if (ctx.ContextState == ContextState.Planning)
                {
                    ctx.TryLogDecomposition(Name, "IsValid", condition, result);
                }

                if (result == false)
                    return false;
            }

            return true;
        }
    }
}
