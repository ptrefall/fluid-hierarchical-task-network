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
            if (ctx.ContextState == ContextState.Planning)
            {
                if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.ApplyEffects", ConsoleColor.Yellow);
            }

            if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth++;
            foreach (var effect in Effects)
            {
                effect.Apply(ctx);
            }
            if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth--;
        }

        public void Stop(IContext ctx)
        {
            Operator?.Stop(ctx);
        }

        // ========================================================= VALIDITY

        public bool IsValid(IContext ctx)
        {
            if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.IsValid check");
            foreach (var condition in Conditions)
            {
                if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth++;
                var result = condition.IsValid(ctx);
                if (ctx.LogDecomposition) ctx.CurrentDecompositionDepth--;
                if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.IsValid:{(result ? "Success" : "Failed")}:{condition.Name} is{(result ? "" : " not")} valid!", result ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
                if (result == false)
                {
                    if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.IsValid:Failed:Preconditions not met!", ConsoleColor.Red);
                    return false;
                }
            }

            if (ctx.LogDecomposition) Log(ctx, $"PrimitiveTask.IsValid:Success!", ConsoleColor.Green);
            return true;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext ctx, string description, ConsoleColor color = ConsoleColor.White)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth+1, this, color);
        }
    }
}
