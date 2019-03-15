using System.Collections.Generic;

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
		/// Duplicate only the world state that will have the potential to change through effects during planning.
		/// </summary>
		/// <returns></returns>
		IContext Duplicate();

		/// <summary>
		/// Copies the values of ctx that has the potential to change through effects during planning.
		/// </summary>
		/// <param name="ctx"></param>
		void Copy( IContext ctx );

		Stack<string> LastConditionFail { get; set; }
	}
}