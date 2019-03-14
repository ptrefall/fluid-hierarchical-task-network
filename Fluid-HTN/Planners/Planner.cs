
using System.Collections.Generic;
using System.Diagnostics;
using FluidHTN.Compounds;

namespace FluidHTN
{
	public static class Planner
	{
		public static Queue<ITask> FindPlan<T>( this Domain< T > domain, T ctx ) where T : IContext
		{
			if (ctx.MethodTraversalRecord == null)
			{
				return null;
			}

			ctx.MethodTraversalRecord.Clear();

			var plan = domain.Root.Decompose( ctx );
			return plan;
		}
	}
}