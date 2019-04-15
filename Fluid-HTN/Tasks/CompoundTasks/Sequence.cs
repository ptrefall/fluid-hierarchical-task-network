using System.Collections.Generic;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public class Sequence : CompoundTask, IDecomposeAll
    {
        // ========================================================= FIELDS

        private readonly Queue<ITask> Plan = new Queue<ITask>();

        // ========================================================= VALIDITY

        public override bool IsValid(IContext ctx)
        {
            // Check that our preconditions are valid first.
            if (base.IsValid(ctx) == false)
                return false;

            // Selector requires there to be children to successfully select from.
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
        protected override Queue<ITask> OnDecompose(IContext ctx, int startIndex)
        {
            Plan.Clear();

            //var oldCtx = ctx.Duplicate();
            var oldStackDepth = ctx.GetWorldStateChangeDepth();

            for (var taskIndex = startIndex; taskIndex < Subtasks.Count; taskIndex++)
            {
                var task = Subtasks[taskIndex];

                if (task.IsValid(ctx) == false)
                {
                    Plan.Clear();
                    //ctx.Copy( oldCtx );
                    ctx.TrimToStackDepth(oldStackDepth);
                    break;
                }

                if (task is ICompoundTask compoundTask)
                {
                    var result = compoundTask.Decompose(ctx, 0);

                    // If result is null, that means the entire planning procedure should cancel.
                    if (result == null)
                    {
                        Plan.Clear();
                        //ctx.Copy( oldCtx );
                        ctx.TrimToStackDepth(oldStackDepth);
                        return null;
                    }

                    // If the decomposition failed
                    if (result.Count == 0)
                    {
                        Plan.Clear();
                        //ctx.Copy( oldCtx );
                        ctx.TrimToStackDepth(oldStackDepth);
                        break;
                    }

                    while (result.Count > 0) Plan.Enqueue(result.Dequeue());

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

                        return Plan;
                    }
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

                    return Plan;
                }
            }

            return Plan;
        }
    }
}