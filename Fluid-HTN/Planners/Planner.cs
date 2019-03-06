
using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN
{
	public static class Planner
	{
		public static Queue<ITask> FindPlan<T>( this Domain< T > domain, T ctx ) where T : IContext
		{
			var plan = domain.Root.Decompose( ctx );
			return plan;
		}
	}
}