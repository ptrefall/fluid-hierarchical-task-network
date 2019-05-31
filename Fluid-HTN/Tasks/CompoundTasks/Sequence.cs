using System.Collections.Generic;
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
                return false;

            // Selector requires there to be subtasks to successfully select from.
            if (Subtasks.Count == 0)
                return false;

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
                primitiveTask.ApplyEffects(ctx);
                Plan.Enqueue(task);
            }
            else if (task is PausePlanTask)
            {
                ctx.HasPausedPartialPlan = true;
                ctx.PartialPlanQueue.Enqueue(new PartialPlanEntry()
                {
                    Task = this,
                    TaskIndex = taskIndex + 1,
                });

                result = Plan;
                return DecompositionStatus.Partial;
            }

            result = Plan;
            return result.Count == 0 ? DecompositionStatus.Failed : DecompositionStatus.Succeeded;
        }

        protected override DecompositionStatus OnDecomposeCompoundTask(IContext ctx, ICompoundTask task,
            int taskIndex, int[] oldStackDepth, out Queue<ITask> result)
        {
            var status = task.Decompose(ctx, 0, out var subPlan);

            // If result is null, that means the entire planning procedure should cancel.
            if (status == DecompositionStatus.Rejected)
            {
                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);

                result = null;
                return DecompositionStatus.Rejected;
            }

            // If the decomposition failed
            if (status == DecompositionStatus.Failed)
            {
                Plan.Clear();
                ctx.TrimToStackDepth(oldStackDepth);
                result = Plan;
                return DecompositionStatus.Failed;
            }

            while (subPlan.Count > 0)
            {
                Plan.Enqueue(subPlan.Dequeue());
            }

            if (ctx.HasPausedPartialPlan)
            {
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
            return DecompositionStatus.Succeeded;
        }
    }
}
