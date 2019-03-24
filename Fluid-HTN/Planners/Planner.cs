
using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluidHTN.Compounds;
using Packages.Tasks.CompoundTasks;

namespace FluidHTN
{
	public static class Planner
	{
		public static Queue<ITask> FindPlan<T>( this Domain< T > domain, T ctx ) where T : IContext
		{
			if (ctx.MethodTraversalRecord == null)
			{
				throw new Exception("We require the Method Traversal Record to have a valid instance.");
			}

			ctx.ContextState = ContextState.Planning;

			Queue<ITask> plan = null;

			// We first check whether we have a stored start task. This is true
			// if we had a partial plan split somewhere in our plan, and we now
			// want to continue where we left off.
			// If this is the case, we don't erase the MTR, but continue building it.
			// However, if we have a partial plan, but LastMTR is not 0, that means
			// that the partial plan is still running, but something triggered a replan.
			// When this happens, we have to plan from the domain root (we're not
			// continuing the current plan), so that we're open for other plans to replace
			// the running partial plan.
			if (ctx.PlanStartTaskParent != null && ctx.LastMTR.Count == 0)
			{
				var root = ctx.PlanStartTaskParent;
				ctx.PlanStartTaskParent = null;

				var startIndex = ctx.PlanStartTaskChildIndex;
				ctx.PlanStartTaskChildIndex = 0;

				plan = root.Decompose(ctx, startIndex);

				// If we fail to decompose from where we left off, then we failed to continue the partial
				// plan, and need a replan from the domain root.
				if (plan == null || plan.Count == 0)
				{
					ctx.LastConditionFail.Push($"Failed to continue the partial plan when decomposing {root.Name}.");
					ctx.MethodTraversalRecord.Clear();

					root = domain.Root;
					plan = root.Decompose(ctx, 0);

					// If we fail to find a plan from the root, well, then we didn't find a plan at all!
					if (plan == null || plan.Count == 0)
					{
						ctx.ContextState = ContextState.Executing;
						return null;
					}
				}

				/*while (plan == null || plan.Count == 0)
				{
					plan = root.Decompose(ctx, startIndex);

					// If decomposing the current root fails, we backtrack the decomposition up the hierarchy,
					// continuing to look for valid decomposition.
					if ( (plan == null || plan.Count == 0) )
					{
						root = BacktrackDecomposition(domain, root, ctx, out startIndex);
						ctx.LastConditionFail.Push($"Backtrack decomposition to new root {root?.Name ?? "none"}.");
						if (root == domain.Root)
						{
							ctx.ContextState = ContextState.Executing;
							return null;
						}
					}
				}*/
			}
			else
			{
				var lastPlanStartTaskParent = ctx.PlanStartTaskParent;

				// We only erase the MTR if we start from the root task of the domain.
				ctx.MethodTraversalRecord.Clear();

				plan = domain.Root.Decompose(ctx, 0);

				// If we found a new plan, let's make sure we remove any partial plan tracking, unless
				// the new plan replaced our partial plan tracking with a new partial plan.
				if (lastPlanStartTaskParent != null && plan != null && plan.Count > 0 && ctx.PlanStartTaskParent == lastPlanStartTaskParent)
				{
					ctx.PlanStartTaskParent = null;
					ctx.PlanStartTaskChildIndex = 0;
				}
			}

			// Trim away any plan-only or plan&execute effects from the world state change stack, that only
			// permanent effects on the world state remains now that the planning is done.
			ctx.TrimForExecution();

			// Apply permanent world state changes to the actual world state used during plan execution.
			for (var i = 0; i < ctx.WorldStateChangeStack.Length; i++)
			{
				var stack = ctx.WorldStateChangeStack[i];
				if (stack != null && stack.Count > 0)
				{
					ctx.WorldState[i] = stack.Peek().Value;
				}
			}

			ctx.ContextState = ContextState.Executing;
			return plan;
		}

		private static ICompoundTask BacktrackDecomposition<T>(Domain<T> domain, ICompoundTask root, IContext ctx, out int startIndex) where T : IContext
		{
			startIndex = 0;

			// If we decomposed back down to the root task of the domain, but failed, we failed to find a valid plan!
			if (root == domain.Root)
			{
				return domain.Root;
			}

			// For each root that fails to find a plan, we need to go back up the hierarchy,
			// but we require roots to be selectors now! Track the descendants, to ensure we
			// continue decomposition after the descendant.
			var descendant = root; 
			while (root is IDecomposeAll)
			{
				descendant = root;
				root = root.Parent;
			}

			// We now need to backtrack our MTR, so that it's correct with the decomposition backtracking.
			if (ctx.MethodTraversalRecord.Count > 0)
			{
				ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count - 1);
			}

			// Find the start index
			for (var i = 0; i < root.Children.Count; i++)
			{
				if (root.Children[i] == descendant)
				{
					startIndex = i+1;
					break;
				}
			}

			// If the descendant was the last child of root, we need to backtrack up the hierarchy further.
			if (startIndex >= root.Children.Count)
			{
				return BacktrackDecomposition<T>(domain, root, ctx, out startIndex);
			}

			// If we get this far, we should have found a new root that we're confident we can decompose,
			// and the index we want to start the decomposition from.
			return root;
		}
	}
}