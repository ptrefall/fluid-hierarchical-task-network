using System;
using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN
{
    public class Domain<T> where T : IContext
    {
        // ========================================================= CONSTRUCTION

        public Domain(string name)
        {
            Root = new TaskRoot {Name = name, Parent = null};
        }
        // ========================================================= PROPERTIES

        public TaskRoot Root { get; }

        // ========================================================= HIERARCHY HANDLING

        public void Add(ICompoundTask parent, ITask child)
        {
            parent.AddChild(child);
            child.Parent = parent;
        }

        // ========================================================= PLANNING

        public Queue<ITask> FindPlan(T ctx)
        {
            if (ctx.MethodTraversalRecord == null)
                throw new Exception("We require the Method Traversal Record to have a valid instance.");

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
                if (plan == null || plan.Count == 0)
                {
                    ctx.MethodTraversalRecord.Clear();
                    ctx.MTRDebug.Clear();

                    Root.Decompose(ctx, 0);

                    // If we found a new plan, let's make sure we remove any partial plan tracking, unless
                    // the new plan replaced our partial plan tracking with a new partial plan.
                    if (root != null && plan != null && plan.Count > 0 && ctx.PlanStartTaskParent == root)
                    {
                        ctx.PlanStartTaskParent = null;
                        ctx.PlanStartTaskChildIndex = 0;
                    }
                }
            }
            else
            {
                var lastPlanStartTaskParent = ctx.PlanStartTaskParent;

                // We only erase the MTR if we start from the root task of the domain.
                ctx.MethodTraversalRecord.Clear();
                ctx.MTRDebug.Clear();

                plan = Root.Decompose(ctx, 0);

                // If we found a new plan, let's make sure we remove any partial plan tracking, unless
                // the new plan replaced our partial plan tracking with a new partial plan.
                if (lastPlanStartTaskParent != null && plan != null && plan.Count > 0 &&
                    ctx.PlanStartTaskParent == lastPlanStartTaskParent)
                {
                    ctx.PlanStartTaskParent = null;
                    ctx.PlanStartTaskChildIndex = 0;
                }
            }

            // If this MTR equals the last MTR, then we need to double check whether we ended up
            // just finding the exact same plan. During decomposition each compound task can't check
            // for equality, only for less than, so this case needs to be treated after the fact.
            var isMTRsEqual = ctx.MethodTraversalRecord.Count == ctx.LastMTR.Count;
            if (isMTRsEqual)
            {
                for (var i = 0; i < ctx.MethodTraversalRecord.Count; i++)
                    if (ctx.MethodTraversalRecord[i] < ctx.LastMTR[i])
                    {
                        isMTRsEqual = false;
                        break;
                    }

                if (isMTRsEqual) plan = null;
            }

            if (plan != null)
            {
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
                        stack.Clear();
                    }
                }
            }
            else
            {
                // Clear away any changes that might have been applied to the stack
                // No changes should be made or tracked further when the plan failed.
                for (var i = 0; i < ctx.WorldStateChangeStack.Length; i++)
                {
                    var stack = ctx.WorldStateChangeStack[i];
                    if (stack != null && stack.Count > 0) stack.Clear();
                }
            }

            ctx.ContextState = ContextState.Executing;
            return plan;
        }
    }
}