using System.Collections.Generic;
using FluidHTN.Operators;

namespace FluidHTN.PrimitiveTasks
{
	public interface IPrimitiveTask : ITask
	{
		IOperator Operator { get; }
		void SetOperator( IOperator action );

		List<IEffect> Effects { get; }
		ITask AddEffect( IEffect effect );
		void ApplyEffects( IContext ctx );
	}
}