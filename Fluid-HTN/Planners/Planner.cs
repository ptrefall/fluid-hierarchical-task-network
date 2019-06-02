using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
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
        // ========================================================= FIELDS

        private ITask _currentTask;
        private readonly Queue<ITask> _plan = new Queue<ITask>();

        // ========================================================= FIELDS
        public TaskStatus LastStatus { get; protected set; }

        // ========================================================= CALLBACKS

        /// <summary>
        ///		OnNewPlan(newPlan) is called when we found a new plan, and there is no
        ///		old plan to replace.
        /// </summary>
        public Action<Queue<ITask>> OnNewPlan = null;

        /// <summary>
        ///		OnReplacePlan(oldPlan, currentTask, newPlan) is called when we're about to replace the
        ///		current plan with a new plan.
        /// </summary>
        public Action<Queue<ITask>, ITask, Queue<ITask>> OnReplacePlan = null;

        /// <summary>
        ///		OnNewTask(task) is called after we popped a new task off the current plan.
        /// </summary>
        public Action<ITask> OnNewTask = null;

        /// <summary>
        ///		OnNewTaskConditionFailed(task, failedCondition) is called when we failed to
        ///		validate a condition on a new task.
        /// </summary>
        public Action<ITask, ICondition> OnNewTaskConditionFailed = null;

        /// <summary>
        ///		OnStopCurrentTask(task) is called when the currently running task was stopped
        ///		forcefully.
        /// </summary>
        public Action<IPrimitiveTask> OnStopCurrentTask = null;

        /// <summary>
        ///		OnCurrentTaskCompletedSuccessfully(task) is called when the currently running task
        ///		completes successfully, and before its effects are applied.
        /// </summary>
        public Action<IPrimitiveTask> OnCurrentTaskCompletedSuccessfully = null;

        /// <summary>
        ///		OnApplyEffect(effect) is called for each effect of the type PlanAndExecute on a
        ///		completed task.
        /// </summary>
        public Action<IEffect> OnApplyEffect = null;

        /// <summary>
        ///		OnCurrentTaskFailed(task) is called when the currently running task fails to complete.
        /// </summary>
        public Action<IPrimitiveTask> OnCurrentTaskFailed = null;

        /// <summary>
        ///		OnCurrentTaskContinues(task) is called every tick that a currently running task
        ///		needs to continue.
        /// </summary>
        public Action<IPrimitiveTask> OnCurrentTaskContinues = null;

        /// <summary>
        ///		OnCurrentTaskExecutingConditionFailed(task, condition) is called if an Executing Condition
        ///		fails. The Executing Conditions are checked before every call to task.Operator.Update(...).
        /// </summary>
        public Action<IPrimitiveTask, ICondition> OnCurrentTaskExecutingConditionFailed = null;

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
        public void Tick(Domain<T> domain, T ctx, bool allowImmediateReplan = true)
        {
            // Check whether state has changed or the current plan has finished running.
            // and if so, try to find a new plan.
            if (_currentTask == null && (_plan.Count == 0) || ctx.IsDirty)
            {
                Queue<PartialPlanEntry> lastPartialPlanQueue = null;

                var worldStateDirtyReplan = ctx.IsDirty;
                ctx.IsDirty = false;

                if (worldStateDirtyReplan)
                {
                    // If we're simply re-evaluating whether to replace the current plan because
                    // some world state got dirt, then we do not intend to continue a partial plan
                    // right now, but rather see whether the world state changed to a degree where
                    // we should pursue a better plan. Thus, if this replan fails to find a better
                    // plan, we have to add back the partial plan temps cached above.
                    if (ctx.HasPausedPartialPlan)
                    {
                        ctx.HasPausedPartialPlan = false;
                        lastPartialPlanQueue = ctx.Factory.CreateQueue<PartialPlanEntry>();
                        while (ctx.PartialPlanQueue.Count > 0)
                        {
                            lastPartialPlanQueue.Enqueue(ctx.PartialPlanQueue.Dequeue());
                        }

                        // We also need to ensure that the last mtr is up to date with the on-going MTR of the partial plan,
                        // so that any new potential plan that is decomposing from the domain root has to beat the currently
                        // running partial plan.
                        ctx.LastMTR.Clear();
                        foreach (var record in ctx.MethodTraversalRecord) ctx.LastMTR.Add(record);

                        if (ctx.DebugMTR)
                        {
                            ctx.LastMTRDebug.Clear();
                            foreach (var record in ctx.MTRDebug) ctx.LastMTRDebug.Add(record);
                        }
                    }
                }

                var status = domain.FindPlan(ctx, out var newPlan);
                if (status == DecompositionStatus.Succeeded || status == DecompositionStatus.Partial)
                {
                    if (OnReplacePlan != null && (_plan.Count > 0 || _currentTask != null))
                    {
                        OnReplacePlan.Invoke(_plan, _currentTask, newPlan);
                    }
                    else if (OnNewPlan != null && _plan.Count == 0)
                    {
                        OnNewPlan.Invoke(newPlan);
                    }

                    _plan.Clear();
                    while (newPlan.Count > 0) _plan.Enqueue(newPlan.Dequeue());

                    if (_currentTask != null && _currentTask is IPrimitiveTask t)
                    {
                        OnStopCurrentTask?.Invoke(t);
                        t.Stop(ctx);
                        _currentTask = null;
                    }

                    // Copy the MTR into our LastMTR to represent the current plan's decomposition record
                    // that must be beat to replace the plan.
                    if (ctx.MethodTraversalRecord != null)
                    {
                        ctx.LastMTR.Clear();
                        foreach (var record in ctx.MethodTraversalRecord) ctx.LastMTR.Add(record);

                        if (ctx.DebugMTR)
                        {
                            ctx.LastMTRDebug.Clear();
                            foreach (var record in ctx.MTRDebug) ctx.LastMTRDebug.Add(record);
                        }
                    }
                }
                else if (lastPartialPlanQueue != null)
                {
                    ctx.HasPausedPartialPlan = true;
                    ctx.PartialPlanQueue.Clear();
                    while (lastPartialPlanQueue.Count > 0)
                    {
                        ctx.PartialPlanQueue.Enqueue(lastPartialPlanQueue.Dequeue());
                    }
                    ctx.Factory.FreeQueue(ref lastPartialPlanQueue);

                    if (ctx.LastMTR.Count > 0)
                    {
                        ctx.MethodTraversalRecord.Clear();
                        foreach (var record in ctx.LastMTR) ctx.MethodTraversalRecord.Add(record);
                        ctx.LastMTR.Clear();

                        if (ctx.DebugMTR)
                        {
                            ctx.MTRDebug.Clear();
                            foreach (var record in ctx.LastMTRDebug) ctx.MTRDebug.Add(record);
                            ctx.LastMTRDebug.Clear();
                        }
                    }
                }
            }

            if (_currentTask == null && _plan.Count > 0)
            {
                _currentTask = _plan.Dequeue();
                if (_currentTask != null)
                {
                    OnNewTask?.Invoke(_currentTask);
                    foreach (var condition in _currentTask.Conditions)
                        // If a condition failed, then the plan failed to progress! A replan is required.
                        if (condition.IsValid(ctx) == false)
                        {
                            OnNewTaskConditionFailed?.Invoke(_currentTask, condition);

                            _currentTask = null;
                            _plan.Clear();

                            ctx.LastMTR.Clear();
                            if (ctx.DebugMTR) ctx.LastMTRDebug.Clear();

                            ctx.HasPausedPartialPlan = false;
                            ctx.PartialPlanQueue.Clear();
                            ctx.IsDirty = false;

                            return;
                        }
                }
            }

            if (_currentTask != null)
                if (_currentTask is IPrimitiveTask task)
                {
                    if (task.Operator != null)
                    {
                        foreach (var condition in task.ExecutingConditions)
                            // If a condition failed, then the plan failed to progress! A replan is required.
                            if (condition.IsValid(ctx) == false)
                            {
                                OnCurrentTaskExecutingConditionFailed?.Invoke(task, condition);

                                _currentTask = null;
                                _plan.Clear();

                                ctx.LastMTR.Clear();
                                if (ctx.DebugMTR) ctx.LastMTRDebug.Clear();

                                ctx.HasPausedPartialPlan = false;
                                ctx.PartialPlanQueue.Clear();
                                ctx.IsDirty = false;

                                return;
                            }

                        LastStatus = task.Operator.Update(ctx);

                        // If the operation finished successfully, we set task to null so that we dequeue the next task in the plan the following tick.
                        if (LastStatus == TaskStatus.Success)
                        {
                            OnCurrentTaskCompletedSuccessfully?.Invoke(task);

                            // All effects that is a result of running this task should be applied when the task is a success.
                            foreach (var effect in task.Effects)
                            {
                                if (effect.Type == EffectType.PlanAndExecute)
                                {
                                    OnApplyEffect?.Invoke(effect);
                                    effect.Apply(ctx);
                                }
                            }

                            _currentTask = null;
                            if (_plan.Count == 0)
                            {
                                ctx.LastMTR.Clear();
                                if (ctx.DebugMTR) ctx.LastMTRDebug.Clear();

                                ctx.IsDirty = false;

                                if (allowImmediateReplan) Tick(domain, ctx, allowImmediateReplan: false);
                            }
                        }

                        // If the operation failed to finish, we need to fail the entire plan, so that we will replan the next tick.
                        else if (LastStatus == TaskStatus.Failure)
                        {
                            OnCurrentTaskFailed?.Invoke(task);

                            _currentTask = null;
                            _plan.Clear();

                            ctx.LastMTR.Clear();
                            if (ctx.DebugMTR) ctx.LastMTRDebug.Clear();

                            ctx.HasPausedPartialPlan = false;
                            ctx.PartialPlanQueue.Clear();
                            ctx.IsDirty = false;
                        }

                        // Otherwise the operation isn't done yet and need to continue.
                        else
                        {
                            OnCurrentTaskContinues?.Invoke(task);
                        }
                    }
                    else
                    {
                        // This should not really happen if a domain is set up properly.
                        _currentTask = null;
                        LastStatus = TaskStatus.Failure;
                    }
                }
        }

        // ========================================================= RESET

        public void Reset(IContext ctx)
        {
            _plan.Clear();
            
            if(_currentTask != null && _currentTask is IPrimitiveTask task)
            {
                task.Stop(ctx);
            }
            _currentTask = null;
        }

        // ========================================================= GETTERS

        /// <summary>
        ///     Get the current plan. This is not a copy of the running plan, so treat it as read-only.
        /// </summary>
        /// <returns></returns>
        public Queue<ITask> GetPlan()
        {
            return _plan;
        }

        /// <summary>
        ///		Get the current task.
        /// </summary>
        /// <returns></returns>
        public ITask GetCurrentTask()
        {
            return _currentTask;
        }
    }
}
