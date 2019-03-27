using System.Collections.Generic;
using FluidHTN.Compounds;
using Packages.Tasks.CompoundTasks;

namespace FluidHTN
{
	public enum ContextState { Planning, Executing }

	public interface IContext
	{
		ContextState ContextState { get; set; }

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
		List<string> MTRDebug { get; set; }

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
		List<string> LastMTRDebug { get; set; }

		/// <summary>
		/// Reset the context state to default values.
		/// </summary>
		void Reset();

		void TrimForExecution();
		void TrimToStackDepth(int[] toDepth);

		bool HasState(int state, byte value);
		byte GetState(int state);
		void SetState(int state, byte value, bool setAsDirty = true, EffectType e = EffectType.Permanent);

		/// <summary>
		/// 
		/// </summary>
		Stack<string> DecompositionLog { get; set; }

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

		byte[] WorldState { get; }

		/// <summary>
		/// A stack of changes applied to each world state entry during planning.
		/// This is necessary if one wants to support planner-only and plan&execute effects.
		/// </summary>
		Stack<KeyValuePair<EffectType, byte>>[] WorldStateChangeStack { get; }

		int[] GetWorldStateChangeDepth();
	}
}