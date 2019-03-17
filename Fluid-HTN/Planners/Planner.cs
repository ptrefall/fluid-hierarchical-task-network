
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

			Queue<ITask> plan = null;

			// We first check whether we have a stored start task. This is true
			// if we had a partial plan split somewhere in our plan, and we now
			// want to continue where we left off.
			// If this is the case, we don't erase the MTR, but continue building it.
			if (ctx.PlanStartTaskParent != null)
			{
				// When we continue a partial plan, we are already committed, and stop
				// comparing to the LastMTR (we erase it).
				ctx.LastMTR.Clear();

				var root = ctx.PlanStartTaskParent;
				ctx.PlanStartTaskParent = null;
				var startIndex = ctx.PlanStartTaskChildIndex;
				ctx.PlanStartTaskChildIndex = 0;
				while (plan == null || plan.Count == 0)
				{
					plan = root.Decompose(ctx, startIndex);
					if (plan == null || plan.Count == 0)
					{
						root = BacktrackDecomposition(domain, root, ctx, out startIndex);
						ctx.LastConditionFail.Push($"Backtrack decomposition to new root {root?.Name ?? "none"}.");
						if (root == domain.Root)
						{
							return null;
						}
					}
				}
			}
			else
			{
				// We only erase the MTR if we start from the root task of the domain.
				ctx.MethodTraversalRecord.Clear();

				plan = domain.Root.Decompose(ctx, 0);
			}

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