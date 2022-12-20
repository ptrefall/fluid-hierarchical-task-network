using FluidHTN.Operators;
using FluidHTN;

namespace Fluid_HTN.UnitTests
{
    internal class MyOperator : IOperator
    {
        public TaskStatus Update(IContext ctx)
        {
            return TaskStatus.Continue;
        }

        public void Stop(IContext ctx)
        {
        }
    }
}
