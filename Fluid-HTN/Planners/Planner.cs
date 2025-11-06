using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN
{
    /// <summary>
    ///     A planner is a responsible for handling the management of finding plans in a domain, replan when the state of the
    ///     running plan
    ///     demands it, or look for a new potential plan if the world state gets dirty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Planner<T> where T : IContext
    {
        // ========================================================= TICK PLAN

        /// <summary>
        ///     Call this with a domain and context instance to have the planner manage plan and task handling for the domain at
        ///     runtime.
        ///     If the plan completes or fails, the planner will find a new plan, or if the context is marked dirty, the planner
        ///     will attempt
        ///     a replan to see whether we can find a better plan now that the state of the world has changed.
        ///     This planner can also be used as a blueprint for writing a custom planner.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <param name="allowImmediateReplan"></param>
        public void Tick(Domain<T> domain, T ctx, bool allowImmediateReplan = true)
        {
            if (ctx.IsInitialized == false)
            {
                throw new Exception("Context was not initialized!");
            }

            var decompositionStatus = DecompositionStatus.Failed;
            var isTryingToReplacePlan = false;
            
            // Check whether state has changed or the current plan has finished running.
            // and if so, try to find a new plan.
            if (ShouldFindNewPlan(ctx))
            {
                isTryingToReplacePlan = TryFindNewPlan(domain, ctx, out decompositionStatus);
            }

            // If the plan has more tasks, we try to select the next one.
            if (CanSelectNextTaskInPlan(ctx))
            {
                // Select the next task, but check whether the conditions of the next task failed to validate.
                if (SelectNextTaskInPlan(domain, ctx) == false)
                {
                    return;
                }

                if (ctx.PlannerState.CurrentTask is IPrimitiveTask taskToStart)
                {
                    if (TryStartPrimitiveTaskOperator(domain, ctx, taskToStart, allowImmediateReplan) == false)
                    {
                        return;
                    }
                }
            }

            // If the current task is a primitive task, we try to tick its operator.
            if (ctx.PlannerState.CurrentTask is IPrimitiveTask task)
            {
                if (TryTickPrimitiveTaskOperator(domain, ctx, task, allowImmediateReplan) == false)
                {
                    return;
                }
            }

            // Check whether the planner failed to find a plan
            if (HasFailedToFindPlan(isTryingToReplacePlan, decompositionStatus, ctx))
            {
                ctx.PlannerState.LastStatus = TaskStatus.Failure;
            }
        }

        /// <summary>
        /// Check whether state has changed or the current plan has finished running.
        /// and if so, try to find a new plan.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private bool ShouldFindNewPlan(T ctx)
        {
            return ctx.IsDirty || (ctx.PlannerState.CurrentTask == null && ctx.PlannerState.Plan.Count == 0);
        }

        private bool TryFindNewPlan(Domain<T> domain, T ctx, out DecompositionStatus decompositionStatus)
        {
            var lastPartialPlanQueue = PrepareDirtyWorldStateForReplan(ctx);
            var isTryingToReplacePlan = ctx.PlannerState.Plan.Count > 0;

            decompositionStatus = domain.FindPlan(ctx, out var newPlan);

            if (HasFoundNewPlan(decompositionStatus))
            {
                OnFoundNewPlan(ctx, newPlan);
            }
            else if (lastPartialPlanQueue != null)
            {
                RestoreLastPartialPlan(ctx, lastPartialPlanQueue);
                RestoreLastMethodTraversalRecord(ctx);
            }

            return isTryingToReplacePlan;
        }

        /// <summary>
        /// If we're simply re-evaluating whether to replace the current plan because
        /// some world state got dirty, then we do not intend to continue a partial plan
        /// right now, but rather see whether the world state changed to a degree where
        /// we should pursue a better plan.
        /// </summary>
        private Queue<PartialPlanEntry> PrepareDirtyWorldStateForReplan(T ctx)
        {
            if (ctx.IsDirty == false)
            {
                return null;
            }

            ctx.IsDirty = false;

            var lastPartialPlan = CacheLastPartialPlan(ctx);
            if (lastPartialPlan == null)
            {
                return null;
            }

            // We also need to ensure that the last mtr is up to date with the on-going MTR of the partial plan,
            // so that any new potential plan that is decomposing from the domain root has to beat the currently
            // running partial plan.
            CopyMtrToLastMtr(ctx);

            return lastPartialPlan;
        }

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

        private void RestoreLastPartialPlan(T ctx, Queue<PartialPlanEntry> lastPartialPlanQueue)
        {
            ctx.HasPausedPartialPlan = true;
            ctx.PartialPlanQueue.Clear();

            while (lastPartialPlanQueue.Count > 0)
            {
                ctx.PartialPlanQueue.Enqueue(lastPartialPlanQueue.Dequeue());
            }

            ctx.Factory.FreeQueue(ref lastPartialPlanQueue);
        }

        private bool HasFoundNewPlan(DecompositionStatus decompositionStatus)
        {
            return decompositionStatus == DecompositionStatus.Succeeded ||
                   decompositionStatus == DecompositionStatus.Partial;
        }

        private void OnFoundNewPlan(T ctx, Queue<ITask> newPlan)
        {
            if (ctx.PlannerState.OnReplacePlan != null && (ctx.PlannerState.Plan.Count > 0 || ctx.PlannerState.CurrentTask != null))
            {
                ctx.PlannerState.OnReplacePlan.Invoke(ctx.PlannerState.Plan, ctx.PlannerState.CurrentTask, newPlan);
            }
            else if (ctx.PlannerState.OnNewPlan != null && ctx.PlannerState.Plan.Count == 0)
            {
                ctx.PlannerState.OnNewPlan.Invoke(newPlan);
            }

            ctx.PlannerState.Plan.Clear();
            while (newPlan.Count > 0)
            {
                ctx.PlannerState.Plan.Enqueue(newPlan.Dequeue());
            }

            // If a task was running from the previous plan, we stop it.
            if (ctx.PlannerState.CurrentTask != null && ctx.PlannerState.CurrentTask is IPrimitiveTask t)
            {
                ctx.PlannerState.OnStopCurrentTask?.Invoke(t);
                t.Stop(ctx);
                ctx.PlannerState.CurrentTask = null;
            }

            // Copy the MTR into our LastMTR to represent the current plan's decomposition record
            // that must be beat to replace the plan.
            CopyMtrToLastMtr(ctx);
        }

        /// <summary>
        /// Copy the MTR into our LastMTR to represent the current plan's decomposition record
        /// that must be beat to replace the plan.
        /// </summary>
        /// <param name="ctx"></param>
        private void CopyMtrToLastMtr(T ctx)
        {
            if (ctx.MethodTraversalRecord != null)
            {
                ctx.LastMTR.Clear();
                foreach (var record in ctx.MethodTraversalRecord)
                {
                    ctx.LastMTR.Add(record);
                }
                
                if (ctx.DebugMTR)
                {
                    ctx.LastMTRDebug.Clear();
                    foreach (var record in ctx.MTRDebug)
                    {
                        ctx.LastMTRDebug.Add(record);
                    }
                }
            }
        }

        /// <summary>
        /// Copy the Last MTR back into our MTR. This is done during rollback when a new plan
        /// failed to beat the last plan.
        /// </summary>
        /// <param name="ctx"></param>
        private void RestoreLastMethodTraversalRecord(T ctx)
        {
            if (ctx.LastMTR.Count > 0)
            {
                ctx.MethodTraversalRecord.Clear();
                foreach (var record in ctx.LastMTR)
                {
                    ctx.MethodTraversalRecord.Add(record);
                }
                ctx.LastMTR.Clear();

                if (ctx.DebugMTR == false)
                {
                    return;
                }

                ctx.MTRDebug.Clear();
                foreach (var record in ctx.LastMTRDebug)
                {
                    ctx.MTRDebug.Add(record);
                }
                ctx.LastMTRDebug.Clear();
            }
        }

        /// <summary>
        /// If current task is null, we need to verify that the plan has more tasks queued.
        /// </summary>
        /// <returns></returns>
        private bool CanSelectNextTaskInPlan(T ctx)
        {
            return ctx.PlannerState.CurrentTask == null && ctx.PlannerState.Plan.Count > 0;
        }

        /// <summary>
        /// Dequeues the next task of the plan and checks its conditions. If a condition fails, we require a replan.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private bool SelectNextTaskInPlan(Domain<T> domain, T ctx)
        {
            ctx.PlannerState.CurrentTask = ctx.PlannerState.Plan.Dequeue();
            if (ctx.PlannerState.CurrentTask != null)
            {
                ctx.PlannerState.OnNewTask?.Invoke(ctx.PlannerState.CurrentTask);

                return IsConditionsValid(ctx);
            }

            return true;
        }

        /// <summary>
        /// When a new task is selected, we should run Start on its Operator.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="allowImmediateReplan"></param>
        /// <returns></returns>
        private bool TryStartPrimitiveTaskOperator(Domain<T> domain, T ctx, IPrimitiveTask task, bool allowImmediateReplan)
        {
            if (task.Operator != null)
            {
                ctx.PlannerState.LastStatus = task.Operator.Start(ctx);

                // If the operation finished successfully already on start, we set task to null so that we dequeue the next task in the plan the following tick.
                if (ctx.PlannerState.LastStatus == TaskStatus.Success)
                {
                    // We have to first invoke that the task operator has run its start function successfully, before we report that the operator finished.
                    ctx.PlannerState.OnCurrentTaskStarted?.Invoke(task);

                    OnOperatorFinishedSuccessfully(domain, ctx, task, allowImmediateReplan);
                    return true;
                }

                // If the operation failed to start, we need to fail the entire plan, so that we will replan the next tick.
                if (ctx.PlannerState.LastStatus == TaskStatus.Failure)
                {
                    FailEntirePlan(domain, ctx, task, allowImmediateReplan);
                    return true;
                }

                // Otherwise the operation started as expected, and we are ready to start running Update ticks on the operator.
                ctx.PlannerState.OnCurrentTaskStarted?.Invoke(task);
                return true;
            }

            // This should not really happen if a domain is set up properly.
            task.Abort(ctx);
            ctx.PlannerState.CurrentTask = null;
            ctx.PlannerState.LastStatus = TaskStatus.Failure;
            return true;
        }

        /// <summary>
        /// While we have a valid primitive task running, we should tick it each tick of the plan execution.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="allowImmediateReplan"></param>
        /// <returns></returns>
        private bool TryTickPrimitiveTaskOperator(Domain<T> domain, T ctx, IPrimitiveTask task, bool allowImmediateReplan)
        {
            if (task.Operator != null)
            {
                if (IsExecutingConditionsValid(domain, ctx, task, allowImmediateReplan) == false)
                {
                    return false;
                }

                ctx.PlannerState.LastStatus = task.Operator.Update(ctx);

                // If the operation finished successfully, we set task to null so that we dequeue the next task in the plan the following tick.
                if (ctx.PlannerState.LastStatus == TaskStatus.Success)
                {
                    OnOperatorFinishedSuccessfully(domain, ctx, task, allowImmediateReplan);
                    return true;
                }

                // If the operation failed to finish, we need to fail the entire plan, so that we will replan the next tick.
                if (ctx.PlannerState.LastStatus == TaskStatus.Failure)
                {
                    FailEntirePlan(domain, ctx, task, allowImmediateReplan);
                    return true;
                }

                // Otherwise the operation isn't done yet and need to continue.
                ctx.PlannerState.OnCurrentTaskContinues?.Invoke(task);
                return true;
            }

            // This should not really happen if a domain is set up properly.
            task.Abort(ctx);
            ctx.PlannerState.CurrentTask = null;
            ctx.PlannerState.LastStatus = TaskStatus.Failure;
            return true;
        }

        /// <summary>
        /// Ensure conditions are valid when a new task is selected from the plan
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private bool IsConditionsValid(T ctx)
        {
            foreach (var condition in ctx.PlannerState.CurrentTask.Conditions)
            {
                // If a condition failed, then the plan failed to progress! A replan is required.
                if (condition.IsValid(ctx) == false)
                {
                    ctx.PlannerState.OnNewTaskConditionFailed?.Invoke(ctx.PlannerState.CurrentTask, condition);
                    AbortTask(ctx, ctx.PlannerState.CurrentTask as IPrimitiveTask);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Ensure executing conditions are valid during plan execution
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="allowImmediateReplan"></param>
        /// <returns></returns>
        private bool IsExecutingConditionsValid(Domain<T> domain, T ctx, IPrimitiveTask task, bool allowImmediateReplan)
        {
            foreach (var condition in task.ExecutingConditions)
            {
                // If a condition failed, then the plan failed to progress! A replan is required.
                if (condition.IsValid(ctx) == false)
                {
                    ctx.PlannerState.OnCurrentTaskExecutingConditionFailed?.Invoke(task, condition);

                    AbortTask(ctx, task);

                    if (allowImmediateReplan)
                    {
                        Tick(domain, ctx, allowImmediateReplan: false);
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// When a task is aborted (due to failed condition checks),
        /// we prepare the context for a replan next tick.
        /// </summary>
        /// <param name="ctx"></param>
        private void AbortTask(T ctx, IPrimitiveTask task)
        {
            task?.Abort(ctx);
            ClearPlanForReplan(ctx);
        }

        /// <summary>
        /// If the operation finished successfully, we set task to null so that we dequeue the next task in the plan the following tick.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="allowImmediateReplan"></param>
        private void OnOperatorFinishedSuccessfully(Domain<T> domain, T ctx, IPrimitiveTask task, bool allowImmediateReplan)
        {
            ctx.PlannerState.OnCurrentTaskCompletedSuccessfully?.Invoke(task);

            // All effects that is a result of running this task should be applied when the task is a success.
            foreach (var effect in task.Effects)
            {
                if (effect.Type == EffectType.PlanAndExecute)
                {
                    ctx.PlannerState.OnApplyEffect?.Invoke(effect);
                    effect.Apply(ctx);
                }
            }

            ctx.PlannerState.CurrentTask = null;
            if (ctx.PlannerState.Plan.Count == 0)
            {
                ctx.LastMTR.Clear();

                if (ctx.DebugMTR)
                {
                    ctx.LastMTRDebug.Clear();
                }

                ctx.IsDirty = false;

                if (allowImmediateReplan)
                {
                    Tick(domain, ctx, allowImmediateReplan: false);
                }
            }
        }

        /// <summary>
        /// If the operation failed to finish, we need to fail the entire plan, so that we will replan the next tick.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        private void FailEntirePlan(Domain<T> domain, T ctx, IPrimitiveTask task, bool allowImmediateReplan)
        {
            ctx.PlannerState.OnCurrentTaskFailed?.Invoke(task);

            task.Abort(ctx);
            ClearPlanForReplan(ctx);

            if (allowImmediateReplan)
            {
                Tick(domain, ctx, allowImmediateReplan: false);
            }
        }
        
        /// <summary>
        /// Prepare the planner state and context for a clean replan
        /// </summary>
        /// <param name="ctx"></param>
        private void ClearPlanForReplan(T ctx)
        {
            ctx.PlannerState.CurrentTask = null;
            ctx.PlannerState.Plan.Clear();

            ctx.LastMTR.Clear();

            if (ctx.DebugMTR)
            {
                ctx.LastMTRDebug.Clear();
            }

            ctx.HasPausedPartialPlan = false;
            ctx.PartialPlanQueue.Clear();
            ctx.IsDirty = false;
        }

        /// <summary>
        /// If current task is null, and plan is empty, and we're not trying to replace the current plan, and decomposition failed or was rejected, then the planner failed to find a plan.
        /// </summary>
        /// <param name="isTryingToReplacePlan"></param>
        /// <param name="decompositionStatus"></param>
        /// <returns></returns>
        private bool HasFailedToFindPlan(bool isTryingToReplacePlan, DecompositionStatus decompositionStatus, T ctx)
        {
            return ctx.PlannerState.CurrentTask == null && ctx.PlannerState.Plan.Count == 0 && isTryingToReplacePlan == false &&
                   (decompositionStatus == DecompositionStatus.Failed ||
                    decompositionStatus == DecompositionStatus.Rejected);
        }

        // ========================================================= RESET

        public void Reset(T ctx)
        {
            ctx.PlannerState.Plan.Clear();

            if (ctx.PlannerState.CurrentTask != null && ctx.PlannerState.CurrentTask is IPrimitiveTask task)
            {
                task.Stop(ctx);
            }

            ClearPlanForReplan(ctx);
        }
    }
}
