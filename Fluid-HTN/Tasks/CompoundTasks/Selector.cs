using System;
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
            {
                if (ctx.LogDecomposition) Log(ctx, $"Selector.IsValid:Failed:Preconditions not met!", ConsoleColor.Red);
                return false;
            }

            // Selector requires there to be at least one sub-task to successfully select from.
            if (Subtasks.Count == 0)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Selector.IsValid:Failed:No sub-tasks!", ConsoleColor.Red);
                return false;
            }

            if (ctx.LogDecomposition) Log(ctx, $"Selector.IsValid:Success!", ConsoleColor.Green);
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
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecompose:Task index: {taskIndex}: {Subtasks[taskIndex]?.Name}");
                // If the last plan is still running, we need to check whether the
                // new decomposition can possibly beat it.
                if (ctx.LastMTR != null && ctx.LastMTR.Count > 0)
                {
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

                            if (ctx.LogDecomposition)
                                Log(ctx,
                                    $"Selector.OnDecompose:Rejected:Index {currentDecompositionIndex} is beat by last method traversal record!", ConsoleColor.Red);
                            result = null;
                            return DecompositionStatus.Rejected;
                        }
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
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeTask:Failed:Task {task.Name}.IsValid returned false!", ConsoleColor.Red);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            if (task is ICompoundTask compoundTask)
            {
                return OnDecomposeCompoundTask(ctx, compoundTask, taskIndex, null, out result);
            }

            if (task is IPrimitiveTask primitiveTask)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeTask:Pushed {primitiveTask.Name} to plan!", ConsoleColor.Blue);
                primitiveTask.ApplyEffects(ctx);
                Plan.Enqueue(task);
            }

            if (task is Slot slot)
            {
                return OnDecomposeSlot(ctx, slot, taskIndex, null, out result);
            }

            result = Plan;
            var status = result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;

            if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeTask:{status}!", status == DecompositionStatus.Succeeded ? ConsoleColor.Green : ConsoleColor.Red);
            return status;
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
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeCompoundTask:{status}: Decomposing {task.Name} was rejected.", ConsoleColor.Red);
                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                // Remove the taskIndex if it failed to decompose.
                ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count - 1);
                if (ctx.DebugMTR) ctx.MTRDebug.RemoveAt(ctx.MTRDebug.Count - 1);

                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeCompoundTask:{status}: Decomposing {task.Name} failed.", ConsoleColor.Red);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                var p = subPlan.Dequeue();
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeCompoundTask:Decomposing {task.Name}:Pushed {p.Name} to plan!", ConsoleColor.Blue);
                Plan.Enqueue(p);
            }

            if (ctx.HasPausedPartialPlan)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeCompoundTask:Return partial plan at index {taskIndex}!", ConsoleColor.DarkBlue);
                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            var s = result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
            if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeCompoundTask:{s}!", s == DecompositionStatus.Succeeded ? ConsoleColor.Green : ConsoleColor.Red);
            return s;
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
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeSlot:{status}: Decomposing {task.Name} was rejected.", ConsoleColor.Red);
                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                // Remove the taskIndex if it failed to decompose.
                ctx.MethodTraversalRecord.RemoveAt(ctx.MethodTraversalRecord.Count - 1);
                if (ctx.DebugMTR) ctx.MTRDebug.RemoveAt(ctx.MTRDebug.Count - 1);

                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeSlot:{status}: Decomposing {task.Name} failed.", ConsoleColor.Red);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                var p = subPlan.Dequeue();
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeSlot:Decomposing {task.Name}:Pushed {p.Name} to plan!", ConsoleColor.Blue);
                Plan.Enqueue(p);
            }

            if (ctx.HasPausedPartialPlan)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeSlot:Return partial plan!", ConsoleColor.DarkBlue);
                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            var s = result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
            if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecomposeSlot:{s}!", s == DecompositionStatus.Succeeded ? ConsoleColor.Green : ConsoleColor.Red);
            return s;
        }
    }
}
