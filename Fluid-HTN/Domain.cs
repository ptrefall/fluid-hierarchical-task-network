using System;
using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN
{
    public class Domain<T> : IDomain where T : IContext
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
            {
                throw new Exception("Parent-task and Sub-task can't be the same instance!");
            }

            parent.AddSubtask(subtask);
            subtask.Parent = parent;
        }

        public void Add(ICompoundTask parent, Slot slot)
        {
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
            if (ctx.IsInitialized == false)
            {
                throw new Exception("Context was not initialized!");
            }

            if (ctx.MethodTraversalRecord == null)
            {
                throw new Exception("We require the Method Traversal Record to have a valid instance.");
            }

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
                status = OnPausedPartialPlan(ctx, ref plan, status);
            }
            else
            {
                status = OnReplanDuringPartialPlanning(ctx, ref plan, status);
            }

            // If this MTR equals the last MTR, then we need to double-check whether we ended up
            // just finding the exact same plan. During decomposition each compound task can't check
            // for equality, only for less than, so this case needs to be treated after the fact.
            if (HasFoundSamePlan(ctx))
            {
                plan = null;
                status = DecompositionStatus.Rejected;
            }

            if (HasDecompositionSucceeded(status))
            {
                // Apply permanent world state changes to the actual world state used during plan execution.
                ApplyPermanentWorldStateStackChanges(ctx);
            }
            else
            {
                // Clear away any changes that might have been applied to the stack
                // No changes should be made or tracked further when the plan failed.
                ClearWorldStateStackChanges(ctx);
            }

            ctx.ContextState = ContextState.Executing;
            return status;
        }

        /// <summary>
        /// We first check whether we have a stored start task. This is true
        /// if we had a partial plan pause somewhere in our plan, and we now
        /// want to continue where we left off.
        /// If this is the case, we don't erase the MTR, but continue building it.
        /// However, if we have a partial plan, but LastMTR is not 0, that means
        /// that the partial plan is still running, but something triggered a replan.
        /// When this happens, we have to plan from the domain root (we're not
        /// continuing the current plan), so that we're open for other plans to replace
        /// the running partial plan.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="plan"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private DecompositionStatus OnReplanDuringPartialPlanning(T ctx, ref Queue<ITask> plan, DecompositionStatus status)
        {
            var lastPartialPlanQueue = CacheLastPartialPlan(ctx);

            ClearMethodTraversalRecord(ctx);

            // Replan through decomposition of the hierarchy
            status = Root.Decompose(ctx, 0, out plan);

            if (HasDecompositionFailed(status))
            {
                RestoreLastPartialPlan(ctx, lastPartialPlanQueue, status);
            }

            return status;
        }

        /// <summary>
        /// If there is a paused partial plan, we cache it to a last partial plan queue.
        /// This is useful when we want to perform a replan, but don't know yet if it will
        /// win over the current plan.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private Queue<PartialPlanEntry> CacheLastPartialPlan(T ctx)
        {
            if (ctx.HasPausedPartialPlan == false)
            {
                return null;
            }

            ctx.HasPausedPartialPlan = false;
            var lastPartialPlanQueue = ctx.Factory.CreateQueue<PartialPlanEntry>();

            while (ctx.PartialPlanQueue.Count > 0)
            {
                lastPartialPlanQueue.Enqueue(ctx.PartialPlanQueue.Dequeue());
            }

            return lastPartialPlanQueue;

        }

        /// <summary>
        /// If we failed to find a new plan, we have to restore the old plan,
        /// if it was a partial plan.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="lastPartialPlanQueue"></param>
        /// <param name="status"></param>
        private void RestoreLastPartialPlan(T ctx, Queue<PartialPlanEntry> lastPartialPlanQueue, DecompositionStatus status)
        {
            if (lastPartialPlanQueue == null)
            {
                return;
            }

            ctx.HasPausedPartialPlan = true;
            ctx.PartialPlanQueue.Clear();

            while (lastPartialPlanQueue.Count > 0)
            {
                ctx.PartialPlanQueue.Enqueue(lastPartialPlanQueue.Dequeue());
            }

            ctx.Factory.FreeQueue(ref lastPartialPlanQueue);
        }

        /// <summary>
        /// We only erase the MTR if we start from the root task of the domain.
        /// </summary>
        /// <param name="ctx"></param>
        private void ClearMethodTraversalRecord(T ctx)
        {
            ctx.MethodTraversalRecord.Clear();

            if (ctx.DebugMTR)
            {
                ctx.MTRDebug.Clear();
            }
        }

        /// <summary>
        /// If decomposition status is failed or rejected, the replan failed.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private bool HasDecompositionFailed(DecompositionStatus status)
        {
            return status == DecompositionStatus.Rejected || status == DecompositionStatus.Failed;
        }

        /// <summary>
        /// If decomposition status is failed or rejected, the replan failed.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private bool HasDecompositionSucceeded(DecompositionStatus status)
        {
            return status == DecompositionStatus.Succeeded || status == DecompositionStatus.Partial;
        }

        /// <summary>
        /// We first check whether we have a stored start task. This is true
        /// if we had a partial plan pause somewhere in our plan, and we now
        /// want to continue where we left off.
        /// If this is the case, we don't erase the MTR, but continue building it.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="plan"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private DecompositionStatus OnPausedPartialPlan(T ctx, ref Queue<ITask> plan, DecompositionStatus status)
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
                    status = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var subPlan);
                    if (HasDecompositionSucceeded(status))
                    {
                        EnqueueToExistingPlan(ref plan, subPlan);
                    }
                }

                // While continuing a partial plan, we might encounter
                // a new pause.
                if (ctx.HasPausedPartialPlan)
                {
                    break;
                }
            }

            // If we failed to continue the paused partial plan,
            // then we have to start planning from the root.
            if (HasDecompositionFailed(status))
            {
                ClearMethodTraversalRecord(ctx);

                status = Root.Decompose(ctx, 0, out plan);
            }

            return status;
        }

        /// <summary>
        /// Enqueues the sub plan's queue onto the existing plan
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="subPlan"></param>
        private void EnqueueToExistingPlan(ref Queue<ITask> plan, Queue<ITask> subPlan)
        {
            while (subPlan.Count > 0)
            {
                plan.Enqueue(subPlan.Dequeue());
            }
        }

        /// <summary>
        /// If this MTR equals the last MTR, then we need to double-check whether we ended up
        /// just finding the exact same plan. During decomposition each compound task can't check
        /// for equality, only for less than, so this case needs to be treated after the fact.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private bool HasFoundSamePlan(T ctx)
        {
            var isMTRsEqual = ctx.MethodTraversalRecord.Count == ctx.LastMTR.Count;
            if (isMTRsEqual)
            {
                for (var i = 0; i < ctx.MethodTraversalRecord.Count; i++)
                {
                    if (ctx.MethodTraversalRecord[i] < ctx.LastMTR[i])
                    {
                        isMTRsEqual = false;
                        break;
                    }
                }

                return isMTRsEqual;
            }

            return false;
        }

        /// <summary>
        /// Apply permanent world state changes to the actual world state used during plan execution.
        /// </summary>
        /// <param name="ctx"></param>
        private void ApplyPermanentWorldStateStackChanges(T ctx)
        {
            // Trim away any plan-only or plan&execute effects from the world state change stack, that only
            // permanent effects on the world state remains now that the planning is done.
            ctx.TrimForExecution();
            
            for (int i = 0; i < ctx.WorldStateChangeStack.Length; i++)
            {
                var stack = ctx.WorldStateChangeStack[i];
                if (stack != null && stack.Count > 0)
                {
                    ctx.WorldState[i] = stack.Peek().Value;
                    stack.Clear();
                }
            }
        }

        /// <summary>
        /// Clear away any changes that might have been applied to the stack
        /// </summary>
        /// <param name="ctx"></param>
        private void ClearWorldStateStackChanges(T ctx)
        {
            foreach (var stack in ctx.WorldStateChangeStack)
            {
                if (stack != null && stack.Count > 0)
                {
                    stack.Clear();
                }
            }
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
