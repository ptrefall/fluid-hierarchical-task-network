using System.Collections.Generic;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public class Selector : CompoundTask
    {
        // ========================================================= FIELDS

        protected readonly Queue<ITask> Plan = new Queue<ITask>();

        // ========================================================= VALIDITY

        public override bool IsValid(IContext ctx)
        {
            // Check that our preconditions are valid first.
            if (base.IsValid(ctx) == false)
                return false;

            // Selector requires there to be at least one sub-task to successfully select from.
            if (Subtasks.Count == 0)
                return false;

            return true;
        }

        // ========================================================= DECOMPOSITION

        /// <summary>
        ///     In a Selector decomposition, just a single sub-task must be valid and successfully decompose for the Selector to be
        ///     successfully decomposed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        protected override DecompositionStatus OnDecompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            Plan.Clear();

            for (var taskIndex = startIndex; taskIndex < Subtasks.Count; taskIndex++)
            {
                // If the last plan is still running, we need to check whether the
                // new decomposition can possibly beat it.
                if (ctx.LastMTR != null && ctx.LastMTR.Count > 0)
                    if (ctx.MethodTraversalRecord.Count < ctx.LastMTR.Count)
                    {
                        // If the last plan's traversal record for this decomposition layer 
                        // has a smaller index than the current task index we're about to
                        // decompose, then the new decomposition can't possibly beat the
                        // running plan, so we cancel finding a new plan.
                        var currentDecompositionIndex = ctx.MethodTraversalRecord.Count;
                        if (ctx.LastMTR[currentDecompositionIndex] < taskIndex)
                        {
                            ctx.MethodTraversalRecord.Add(-1);
                            if (ctx.DebugMTR) ctx.MTRDebug.Add($"REPLAN FAIL {Subtasks[taskIndex].Name}");

                            result = null;
                            return DecompositionStatus.Rejected;
                        }
                    }

                var task = Subtasks[taskIndex];

                var status = OnDecomposeTask(ctx, task, taskIndex, null, out result);
                switch (status)
                {
                    case DecompositionStatus.Rejected:
                    case DecompositionStatus.Succeeded:
                    case DecompositionStatus.Partial:
                        return status;
                    case DecompositionStatus.Failed:
                    default:
                        continue;
                }
            }

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeTask(IContext ctx, ITask task, int taskIndex,
            int[] oldStackDepth, out Queue<ITask> result)
        {
            if (task.IsValid(ctx) == false)
            {
                result = Plan;
                return DecompositionStatus.Failed;
            }

            if (task is ICompoundTask compoundTask)
            {
                return OnDecomposeCompoundTask(ctx, compoundTask, taskIndex, null, out result);
            }

            if (task is IPrimitiveTask primitiveTask)
            {
                primitiveTask.ApplyEffects(ctx);
                Plan.Enqueue(task);
            }

            if (task is Slot slot)
            {
                return OnDecomposeSlot(ctx, slot, taskIndex, null, out result);
            }

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeCompoundTask(IContext ctx, ICompoundTask task, int taskIndex,
            int[] oldStackDepth, out Queue<ITask> result)
        {
            // We need to record the task index before we decompose the task,
            // so that the traversal record is set up in the right order.
            ctx.MethodTraversalRecord.Add(taskIndex);
            if (ctx.DebugMTR) ctx.MTRDebug.Add(task.Name);

            var status = task.Decompose(ctx, 0, out var subPlan);

            // If status is rejected, that means the entire planning procedure should cancel.
            if (status == DecompositionStatus.Rejected)
            {
                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                // Remove the taskIndex if it failed to decompose.
                ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count - 1);
                if (ctx.DebugMTR) ctx.MTRDebug.RemoveAt(ctx.MTRDebug.Count - 1);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                Plan.Enqueue(subPlan.Dequeue());
            }

            if (ctx.HasPausedPartialPlan)
            {
                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeSlot(IContext ctx, Slot task, int taskIndex, int[] oldStackDepth, out Queue<ITask> result)
        {
            // We need to record the task index before we decompose the task,
            // so that the traversal record is set up in the right order.
            ctx.MethodTraversalRecord.Add(taskIndex);
            if (ctx.DebugMTR) ctx.MTRDebug.Add(task.Name);

            var status = task.Decompose(ctx, 0, out var subPlan);

            // If status is rejected, that means the entire planning procedure should cancel.
            if (status == DecompositionStatus.Rejected)
            {
                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                // Remove the taskIndex if it failed to decompose.
                ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count - 1);
                if (ctx.DebugMTR) ctx.MTRDebug.RemoveAt(ctx.MTRDebug.Count - 1);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                Plan.Enqueue(subPlan.Dequeue());
            }

            if (ctx.HasPausedPartialPlan)
            {
                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }
    }
}
