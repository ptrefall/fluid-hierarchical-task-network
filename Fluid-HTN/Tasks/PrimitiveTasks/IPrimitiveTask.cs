using System.Collections.Generic;
using FluidHTN.Conditions;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
    public interface IPrimitiveTask : ITask
    {
        /// <summary>
        ///     Executing conditions are validated before every call to Operator.Update(...)
        /// </summary>
        List<ICondition> ExecutingConditions { get; }

        /// <summary>
        ///     Add a new executing condition to the primitive task. This will be checked before
        ///		every call to Operator.Update(...)
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        ITask AddExecutingCondition(ICondition condition);

        IOperator Operator { get; }
        void SetOperator(IOperator action);

        List<IEffect> Effects { get; }
        ITask AddEffect(IEffect effect);
        void ApplyEffects(IContext ctx);

        void Stop(IContext ctx);
    }
}