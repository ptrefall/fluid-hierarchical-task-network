using System;
using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN
{
    public class Domain<T> where T : IContext
    {
        // ========================================================= FIELDS

        private Dictionary<int, Slot> _slots = null;

        // ========================================================= CONSTRUCTION

        public Domain(string name)
        {
            Root = new TaskRoot { Name = name, Parent = null };
        }
        // ========================================================= PROPERTIES

        public TaskRoot Root { get; }

        // ========================================================= HIERARCHY HANDLING

        public void Add(ICompoundTask parent, ITask subtask)
        {
            if (parent == subtask)
                throw new Exception("Parent-task and Sub-task can't be the same instance!");

            parent.AddSubtask(subtask);
            subtask.Parent = parent;
        }

        public void Add(ICompoundTask parent, Slot slot)
        {
            if (parent == slot)
                throw new Exception("Parent-task and Sub-task can't be the same instance!");

            if (_slots != null)
            {
                if (_slots.ContainsKey(slot.SlotId))
                {
                    throw new Exception("This slot id already exist in the domain definition!");
                }
            }

            parent.AddSubtask(slot);
            slot.Parent = parent;

            if(_slots == null)
            {
                _slots = new Dictionary<int, Slot>();
            }

            _slots.Add(slot.SlotId, slot);
        }

        // ========================================================= PLANNING

        public DecompositionStatus FindPlan(T ctx, out Queue<ITask> plan)
        {
            if (ctx.MethodTraversalRecord == null)
                throw new Exception("We require the Method Traversal Record to have a valid instance.");

            ctx.ContextState = ContextState.Planning;

            plan = null;
            var status = DecompositionStatus.Rejected;

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
                        status = kvp.Task.Decompose(ctx, kvp.TaskIndex, out plan);
                    }
                    else
                    {
                        status = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var p);
                        if (status == DecompositionStatus.Succeeded || status == DecompositionStatus.Partial)
                        {
                            while (p.Count > 0)
                            {
                                plan.Enqueue(p.Dequeue());
                            }
                        }
                    }

                    // While continuing a partial plan, we might encounter
                    // a new pause.
                    if (ctx.HasPausedPartialPlan)
                        break;
                }

                // If we failed to continue the paused partial plan,
                // then we have to start planning from the root.
                if (status == DecompositionStatus.Rejected || status == DecompositionStatus.Failed)
                {
                    ctx.MethodTraversalRecord.Clear();
                    if (ctx.DebugMTR) ctx.MTRDebug.Clear();

                    status = Root.Decompose(ctx, 0, out plan);
                }
            }
            else
            {
                Queue<PartialPlanEntry> lastPartialPlanQueue = null;
                if (ctx.HasPausedPartialPlan)
                {
                    ctx.HasPausedPartialPlan = false;
                    lastPartialPlanQueue = ctx.Factory.CreateQueue<PartialPlanEntry>();
                    while (ctx.PartialPlanQueue.Count > 0)
                    {
                        lastPartialPlanQueue.Enqueue(ctx.PartialPlanQueue.Dequeue());
                    }
                }

                // We only erase the MTR if we start from the root task of the domain.
                ctx.MethodTraversalRecord.Clear();
                if (ctx.DebugMTR) ctx.MTRDebug.Clear();

                status = Root.Decompose(ctx, 0, out plan);

                // If we failed to find a new plan, we have to restore the old plan,
                // if it was a partial plan.
                if (lastPartialPlanQueue != null)
                {
                    if (status == DecompositionStatus.Rejected || status == DecompositionStatus.Failed)
                    {
                        ctx.HasPausedPartialPlan = true;
                        ctx.PartialPlanQueue.Clear();
                        while (lastPartialPlanQueue.Count > 0)
                        {
                            ctx.PartialPlanQueue.Enqueue(lastPartialPlanQueue.Dequeue());
                        }
                        ctx.Factory.FreeQueue(ref lastPartialPlanQueue);
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

                if (isMTRsEqual)
                {
                    plan = null;
                    status = DecompositionStatus.Rejected;
                }
            }

            if (status == DecompositionStatus.Succeeded || status == DecompositionStatus.Partial)
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
            return status;
        }

        // ========================================================= SLOTS

        /// <summary>
        ///     At runtime, set a sub-domain to the slot with the given id.
        ///     This can be used with Smart Objects, to extend the behavior
        ///     of an agent at runtime.
        /// </summary>
        public bool TrySetSlotDomain(int slotId, Domain<T> subDomain)
        {
            if(_slots != null && _slots.TryGetValue(slotId, out var slot))
            {
                return slot.Set(subDomain.Root);
            }

            return false;
        }

        /// <summary>
        ///     At runtime, clear the sub-domain from the slot with the given id.
        ///     This can be used with Smart Objects, to extend the behavior
        ///     of an agent at runtime.
        /// </summary>
        public void ClearSlot(int slotId)
        {
            if (_slots != null && _slots.TryGetValue(slotId, out var slot))
            {
                slot.Clear();
            }
        }
    }
}
