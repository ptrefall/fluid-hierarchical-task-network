using System.Collections.Generic;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
    public interface IPrimitiveTask : ITask
    {
        IOperator Operator { get; }

        List<IEffect> Effects { get; }
        void SetOperator(IOperator action);
        ITask AddEffect(IEffect effect);
        void ApplyEffects(IContext ctx);

        void Stop(IContext ctx);
    }
}