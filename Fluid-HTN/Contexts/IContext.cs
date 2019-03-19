using System.Collections.Generic;
using FluidHTN.Compounds;
using Packages.Tasks.CompoundTasks;

namespace FluidHTN
{
	public interface IContext
	{
		/// <summary>
		/// The Method Traversal Record is used while decomposing a domain and
		/// records the valid decomposition indices as we go through our
		/// decomposition process.
		/// 
		/// It "should" be enough to only record decomposition traversal in Selectors.
		/// 
		/// This can be used to compare LastMTR with the MTR, and reject
		/// a new plan early if it is of lower priority than the last plan.
		///
		/// It is the user's responsibility to set the instance of the MTR, so that
		/// the user is free to use pooled instances, or whatever optimization they
		/// see fit.
		/// </summary>
		List<int> MethodTraversalRecord { get; set; }

		/// <summary>
		/// The Method Traversal Record that was recorded for the currently
		/// running plan.
		/// 
		/// If a plan completes successfully, this should be cleared.
		///
		/// It is the user's responsibility to set the instance of the MTR, so that
		/// the user is free to use pooled instances, or whatever optimization they
		/// see fit.
		/// </summary>
		List<int> LastMTR { get; }

		/// <summary>
		/// Reset the context state to default values.
		/// </summary>
		void Reset();

		/// <summary>
		/// Duplicate only the world state that will have the potential to change through effects during planning.
		/// </summary>
		/// <returns></returns>
		IContext Duplicate();

		/// <summary>
		/// Copies the values of ctx that has the potential to change through effects during planning.
		/// </summary>
		/// <param name="ctx"></param>
		void Copy( IContext ctx );

		/// <summary>
		/// 
		/// </summary>
		Stack<string> LastConditionFail { get; set; }

		/// <summary>
		/// The parent task of the partial split. The next child to be decomposed is marked by the
		/// PlanStartTaskChildIndex marker.
		/// </summary>
		ICompoundTask PlanStartTaskParent { get; set; }

		/// <summary>
		/// The marker for where to continue in a decompose-all compound task, like a Sequence task,
		/// after a partial split.
		/// </summary>
		int PlanStartTaskChildIndex { get; set; }
	}
}