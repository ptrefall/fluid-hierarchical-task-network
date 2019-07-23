using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public class Sequence : CompoundTask, IDecomposeAll
    {
        // ========================================================= FIELDS

        protected readonly Queue<ITask> Plan = new Queue<ITask>();

        // ========================================================= VALIDITY

        public override bool IsValid(IContext ctx)
        {
            // Check that our preconditions are valid first.
            if (base.IsValid(ctx) == false)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.IsValid:Failed:Preconditions not met!", ConsoleColor.Red);
                return false;
            }

            // Selector requires there to be subtasks to successfully select from.
            if (Subtasks.Count == 0)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.IsValid:Failed:No sub-tasks!", ConsoleColor.Red);
                return false;
            }

            if (ctx.LogDecomposition) Log(ctx, $"Sequence.IsValid:Success!", ConsoleColor.Green);
            return true;
        }

        // ========================================================= DECOMPOSITION

        /// <summary>
        ///     In a Sequence decomposition, all sub-tasks must be valid and successfully decomposed in order for the Sequence to
        ///     be successfully decomposed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        protected override DecompositionStatus OnDecompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            Plan.Clear();

            var oldStackDepth = ctx.GetWorldStateChangeDepth(ctx.Factory);

            for (var taskIndex = startIndex; taskIndex < Subtasks.Count; taskIndex++)
            {
                var task = Subtasks[taskIndex];
                if (ctx.LogDecomposition) Log(ctx, $"Selector.OnDecompose:Task index: {taskIndex}: {task?.Name}");

                var status = OnDecomposeTask(ctx, task, taskIndex, oldStackDepth, out result);
                switch (status)
                {
                    case DecompositionStatus.Rejected:
                    case DecompositionStatus.Failed:
                    case DecompositionStatus.Partial:
                    {
                        ctx.Factory.FreeArray(ref oldStackDepth);
                        return status;
                    }
                }
            }

            ctx.Factory.FreeArray(ref oldStackDepth);

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeTask(IContext ctx, ITask task, int taskIndex,
            int[] oldStackDepth, out Queue<ITask> result)
        {
            if (task.IsValid(ctx) == false)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeTask:Failed:Task {task.Name}.IsValid returned false!", ConsoleColor.Red);
                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            if (task is ICompoundTask compoundTask)
            {
                return OnDecomposeCompoundTask(ctx, compoundTask, taskIndex, oldStackDepth, out result);
            }
            else if (task is IPrimitiveTask primitiveTask)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeTask:Pushed {primitiveTask.Name} to plan!", ConsoleColor.Blue);
                primitiveTask.ApplyEffects(ctx);
                Plan.Enqueue(task);
            }
            else if (task is PausePlanTask)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeTask:Return partial plan at index {taskIndex}!", ConsoleColor.DarkBlue);
                ctx.HasPausedPartialPlan = true;
                ctx.PartialPlanQueue.Enqueue(new PartialPlanEntry()
                {
                    Task = this,
                    TaskIndex = taskIndex + 1,
                });

                result = Plan;
                return DecompositionStatus.Partial;
            }
            else if (task is Slot slot)
            {
                return OnDecomposeSlot(ctx, slot, taskIndex, oldStackDepth, out result);
            }

            result = Plan;
            var s = result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
            if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeTask:{s}!", s == DecompositionStatus.Succeeded ? ConsoleColor.Green : ConsoleColor.Red);
            return s;
        }

        protected override DecompositionStatus OnDecomposeCompoundTask(IContext ctx, ICompoundTask task,
            int taskIndex, int[] oldStackDepth, out Queue<ITask> result)
        {
            var status = task.Decompose(ctx, 0, out var subPlan);

            // If result is null, that means the entire planning procedure should cancel.
            if (status == DecompositionStatus.Rejected)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeCompoundTask:{status}: Decomposing {task.Name} was rejected.", ConsoleColor.Red);

                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);

                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeCompoundTask:{status}: Decomposing {task.Name} failed.", ConsoleColor.Red);

                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                var p = subPlan.Dequeue();
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeCompoundTask:Decomposing {task.Name}:Pushed {p.Name} to plan!", ConsoleColor.Blue);
                Plan.Enqueue(p);
            }

            if (ctx.HasPausedPartialPlan)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeCompoundTask:Return partial plan at index {taskIndex}!", ConsoleColor.DarkBlue);
                if (taskIndex < Subtasks.Count - 1)
                {
                    ctx.PartialPlanQueue.Enqueue(new PartialPlanEntry()
                    {
                        Task = this,
                        TaskIndex = taskIndex + 1,
                    });
                }

                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeCompoundTask:Succeeded!", ConsoleColor.Green);
            return DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeSlot(IContext ctx, Slot task,
            int taskIndex, int[] oldStackDepth, out Queue<ITask> result)
        {
            var status = task.Decompose(ctx, 0, out var subPlan);

            // If result is null, that means the entire planning procedure should cancel.
            if (status == DecompositionStatus.Rejected)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeSlot:{status}: Decomposing {task.Name} was rejected.", ConsoleColor.Red);

                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);

                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeSlot:{status}: Decomposing {task.Name} failed.", ConsoleColor.Red);

                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                var p = subPlan.Dequeue();
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeSlot:Decomposing {task.Name}:Pushed {p.Name} to plan!", ConsoleColor.Blue);
                Plan.Enqueue(p);
            }

            if (ctx.HasPausedPartialPlan)
            {
                if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeSlot:Return partial plan at index {taskIndex}!", ConsoleColor.DarkBlue);
                if (taskIndex < Subtasks.Count - 1)
                {
                    ctx.PartialPlanQueue.Enqueue(new PartialPlanEntry()
                    {
                        Task = this,
                        TaskIndex = taskIndex + 1,
                    });
                }

                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            if (ctx.LogDecomposition) Log(ctx, $"Sequence.OnDecomposeSlot:Succeeded!", ConsoleColor.Green);
            return DecompositionStatus.Succeeded;
        }
    }
}
