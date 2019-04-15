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
            parent.AddSubtask(child);
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
            // if we had a partial plan pause somewhere in our plan, and we now
            // want to continue where we left off.
            // If this is the case, we don't erase the MTR, but continue building it.
            // However, if we have a partial plan, but LastMTR is not 0, that means
            // that the partial plan is still running, but something triggered a replan.
            // When this happens, we have to plan from the domain root (we're not
            // continuing the current plan), so that we're open for other plans to replace
            // the running partial plan.
            if (ctx.HasPausedPartialPlan && ctx.LastMTR.Count == 0)
            {
                ctx.HasPausedPartialPlan = false;
                while (ctx.PartialPlanQueue.Count > 0)
                {
                    var kvp = ctx.PartialPlanQueue.Dequeue();
                    if (plan == null)
                    {
                        plan = kvp.Task.Decompose(ctx, kvp.TaskIndex);
                    }
                    else
                    {
                        var p = kvp.Task.Decompose(ctx, kvp.TaskIndex);
                        while (p.Count > 0)
                        {
                            plan.Enqueue(p.Dequeue());
                        }
                    }

                    // While continuing a partial plan, we might encounter
                    // a new pause.
                    if (ctx.HasPausedPartialPlan)
                        break;
                }

                // If we failed to continue the paused partial plan,
                // then we have to start planning from the root.
                if (plan == null || plan.Count == 0)
                {
                    ctx.MethodTraversalRecord.Clear();
                    if (ctx.DebugMTR) ctx.MTRDebug.Clear();

                    Root.Decompose(ctx, 0);
                }
            }
            else
            {
                Queue<PartialPlanEntry> lastPartialPlanQueue = null;
                if (ctx.HasPausedPartialPlan)
                {
                    ctx.HasPausedPartialPlan = false;
                    lastPartialPlanQueue = new Queue<PartialPlanEntry>(); // TODO: Use a pool
                    while (ctx.PartialPlanQueue.Count > 0)
                    {
                        lastPartialPlanQueue.Enqueue(ctx.PartialPlanQueue.Dequeue());
                    }
                }

                // We only erase the MTR if we start from the root task of the domain.
                ctx.MethodTraversalRecord.Clear();
                if (ctx.DebugMTR) ctx.MTRDebug.Clear();

                plan = Root.Decompose(ctx, 0);

                // If we failed to find a new plan, we have to restore the old plan,
                // if it was a partial plan.
                if (lastPartialPlanQueue != null)
                {
                    if(plan == null || plan.Count == 0)
                    {
                        ctx.HasPausedPartialPlan = true;
                        ctx.PartialPlanQueue.Clear();
                        while (lastPartialPlanQueue.Count > 0)
                        {
                            ctx.PartialPlanQueue.Enqueue(lastPartialPlanQueue.Dequeue());
                        }
                    }
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