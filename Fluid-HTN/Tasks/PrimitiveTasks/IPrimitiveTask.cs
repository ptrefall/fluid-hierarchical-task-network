using System.Collections.Generic;
using FluidHTN.Conditions;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
    public interface IPrimitiveTask<TWorldStateEntry> : ITask<TWorldStateEntry>
    {
        /// <summary>
        ///     Executing conditions are validated before every call to Operator.Update(...)
        /// </summary>
        List<ICondition<TWorldStateEntry>> ExecutingConditions { get; }

        /// <summary>
        ///     Add a new executing condition to the primitive task. This will be checked before
        ///		every call to Operator.Update(...)
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITask<TWorldStateEntry> AddExecutingCondition(ICondition<TWorldStateEntry> condition);

        IOperator<TWorldStateEntry> Operator { get; }
        void SetOperator(IOperator<TWorldStateEntry> action);

        List<IEffect<TWorldStateEntry>> Effects { get; }
        ITask<TWorldStateEntry> AddEffect(IEffect<TWorldStateEntry> effect);
        void ApplyEffects(IContext<TWorldStateEntry> ctx);

        void Stop(IContext<TWorldStateEntry> ctx);
    }
}
