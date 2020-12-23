#pragma once
#include "Tasks/Task.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"

namespace FluidHTN
{

class Planner
{
    SharedPtr<ITask> _currentTask;
    TaskQueueType          _plan;
    TaskStatus             _LastStatus;

public:
    TaskStatus LastStatus() { return _LastStatus; }

    /// <summary>
    ///		OnNewPlan(newPlan) is called when we found a new plan, and there is no
    ///		old plan to replace.
    /// </summary>
    std::function<void(TaskQueueType)> OnNewPlan;

    /// <summary>
    ///		OnReplacePlan(oldPlan, currentTask, newPlan) is called when we're about to replace the
    ///		current plan with a new plan.
    /// </summary>
    std::function<void(TaskQueueType, SharedPtr<ITask>&, TaskQueueType)> OnReplacePlan;

    /// <summary>
    ///		OnNewTask(task) is called after we popped a new task off the current plan.
    /// </summary>
    std::function<void(SharedPtr<ITask>&)> OnNewTask;

    /// <summary>
    ///		OnNewTaskConditionFailed(task, failedCondition) is called when we failed to
    ///		validate a condition on a new task.
    /// </summary>
    std::function<void(SharedPtr<ITask>&, SharedPtr<ICondition>&)> OnNewTaskConditionFailed;

    /// <summary>
    ///		OnStopCurrentTask(task) is called when the currently running task was stopped
    ///		forcefully.
    /// </summary>
    std::function<void(SharedPtr<PrimitiveTask>&)> OnStopCurrentTask;

    /// <summary>
    ///		OnCurrentTaskCompletedSuccessfully(task) is called when the currently running task
    ///		completes successfully, and before its effects are applied.
    /// </summary>
    std::function<void(SharedPtr<PrimitiveTask>&)> OnCurrentTaskCompletedSuccessfully;

    /// <summary>
    ///		OnApplyEffect(effect) is called for each effect of the type PlanAndExecute on a
    ///		completed task.
    /// </summary>
    std::function<void(SharedPtr<IEffect>&)> OnApplyEffect;

    /// <summary>
    ///		OnCurrentTaskFailed(task) is called when the currently running task fails to complete.
    /// </summary>
    std::function<void(SharedPtr<PrimitiveTask>&)> OnCurrentTaskFailed;

    /// <summary>
    ///		OnCurrentTaskContinues(task) is called every tick that a currently running task
    ///		needs to continue.
    /// </summary>
    std::function<void(SharedPtr<PrimitiveTask>&)> OnCurrentTaskContinues;

    /// <summary>
    ///		OnCurrentTaskExecutingConditionFailed(task, condition) is called if an Executing Condition
    ///		fails. The Executing Conditions are checked before every call to task.Operator.Update(...).
    /// </summary>
    std::function<void(SharedPtr<PrimitiveTask>&, SharedPtr<ICondition>&)> OnCurrentTaskExecutingConditionFailed;

    void Reset(IContext& ctx)
    {
        _plan = TaskQueueType();
        if (_currentTask != nullptr)
        {
            if (_currentTask->IsTypeOf(ITaskDerivedClassName::PrimitiveTask) )
            {
				auto task = StaticCastPtr<PrimitiveTask>(_currentTask);
                task->Stop(ctx);
            }
            _currentTask = nullptr;
        }
    }
    const TaskQueueType&          GetPlan() { return _plan; }
    const SharedPtr<ITask>& GetCurrentTask() { return _currentTask; }

    // ========================================================= TICK PLAN
    template<typename WSIDTYPE, typename WSVALTYPE, typename WSDERIVEDTYPE>
    void Tick(Domain& domain, IContext& ctx, bool allowImmediateReplan = true)
    {
        FHTN_FATAL_EXCEPTION(ctx.IsInitialized(), "Context was not initialized");

        DecompositionStatus decompositionStatus = DecompositionStatus::Failed;
        bool                isTryingToReplacePlan = false;

        // Check whether state has changed or the current plan has finished running.
        // and if so, try to find a new plan.
        if (((_currentTask == nullptr) && (_plan.size() == 0)) || ctx.IsDirty())
        {
            Queue<PartialPlanEntry> lastPartialPlanQueue;
            bool                    worldStateDirtyReplan = ctx.IsDirty();

            if (worldStateDirtyReplan)
            {
                // If we're simply re-evaluating whether to replace the current plan because
                // some world state got dirt, then we do not intend to continue a partial plan
                // right now, but rather see whether the world state changed to a degree where
                // we should pursue a better plan. Thus, if this replan fails to find a better
                // plan, we have to add back the partial plan temps cached above.
                if (ctx.HasPausedPartialPlan())
                {
                    ctx.HasPausedPartialPlan() = false;
                    while (ctx.PartialPlanQueue().size() > 0)
                    {
                        lastPartialPlanQueue.push(ctx.PartialPlanQueue().front());
                        ctx.PartialPlanQueue().pop();
                    }
                    // We also need to ensure that the last mtr is up to date with the on-going MTR of the partial plan,
                    // so that any new potential plan that is decomposing from the domain root has to beat the currently
                    // running partial plan.
                    ctx.LastMTR().clear();
                    for (size_t si = 0; si < ctx.MethodTraversalRecord().size();si++)
                    {
                        auto record = ctx.MethodTraversalRecord()[si];
                        ctx.LastMTR().Add(record);
                    }
                    if (ctx.DebugMTR())
                    {
                        ctx.LastMTRDebug().clear();
                        for (size_t si =0 ; si < ctx.MTRDebug().size();si++)
                        {
                            auto record = ctx.MTRDebug()[si];
                            ctx.LastMTRDebug().Add(record);
                        }
                    }
                }
            }
            TaskQueueType newPlan;
            decompositionStatus = domain.FindPlan(static_cast<BaseContext<WSIDTYPE,WSVALTYPE,WSDERIVEDTYPE>&>(ctx), newPlan);
            isTryingToReplacePlan = (_plan.size() > 0);
            if (decompositionStatus == DecompositionStatus::Succeeded || decompositionStatus == DecompositionStatus::Partial)
            {
                if (OnReplacePlan != nullptr && (_plan.size() > 0 || _currentTask != nullptr))
                {
                    OnReplacePlan(_plan, _currentTask, newPlan);
                }
                else if (OnNewPlan != nullptr && _plan.size() == 0)
                {
                    OnNewPlan(newPlan);
                }

                _plan = newPlan;

                if (_currentTask != nullptr && _currentTask->IsTypeOf(ITaskDerivedClassName::PrimitiveTask))
                {
                    auto tPrimitive = StaticCastPtr<PrimitiveTask>(_currentTask);
                    if (OnStopCurrentTask != nullptr)
                    {
                        OnStopCurrentTask(tPrimitive);
                    }
                    tPrimitive->Stop(ctx);
                    _currentTask = nullptr;
                }

                // Copy the MTR into our LastMTR to represent the current plan's decomposition record
                // that must be beat to replace the plan.
                if (ctx.MethodTraversalRecord().size() != 0)
                {
                    ctx.LastMTR().clear();
                    for (size_t si = 0; si < ctx.MethodTraversalRecord().size() ;si++)
                    {
                        auto& record = ctx.MethodTraversalRecord()[si];
                        ctx.LastMTR().Add(record);
                    }
                    if (ctx.DebugMTR())
                    {
                        ctx.LastMTRDebug().clear();
                        for (size_t si =0 ; si < ctx.MTRDebug().size();si++)
                        {
                            auto record = ctx.MTRDebug()[si];
                            ctx.LastMTRDebug().Add(record);
                        }
                    }
                }
            }
            else if (lastPartialPlanQueue.size() != 0)
            {
                ctx.HasPausedPartialPlan() = true;
                ctx.ClearPartialPlanQueue();
                while (lastPartialPlanQueue.size() > 0)
                {
                    ctx.PartialPlanQueue().push(lastPartialPlanQueue.front());
                    lastPartialPlanQueue.pop();
                }
                if (ctx.LastMTR().size() > 0)
                {
                    ctx.MethodTraversalRecord().clear();
					for (size_t si =0 ; si < ctx.LastMTR().size();si++)
                    {
                        auto& record = ctx.LastMTR()[si];
                        ctx.MethodTraversalRecord().Add(record);
                    }
                    ctx.LastMTR().clear();
                    if (ctx.DebugMTR())
                    {
                        ctx.LastMTRDebug().clear();
                        for (size_t si =0 ; si < ctx.MTRDebug().size();si++)
                        {
                            auto record = ctx.MTRDebug()[si];
                            ctx.LastMTRDebug().Add(record);
                        }
                    }
                }
            }
        }
        if (_currentTask == nullptr && _plan.size() > 0)
        {
            _currentTask = _plan.front();
            _plan.pop();

            if (_currentTask != nullptr)
            {
                if (OnNewTask != nullptr)
                {
                    OnNewTask(_currentTask);
                }
                for (size_t si = 0; si < _currentTask->Conditions().size();si++)
                {
                    auto& condition = _currentTask->Conditions()[si];
                    // If a condition failed, then the plan failed to progress! A replan is required.
                    if (condition->IsValid(ctx) == false)
                    {
                        if (OnNewTaskConditionFailed)
                        {
                            OnNewTaskConditionFailed(_currentTask, condition);
                        }

                        _currentTask = nullptr;
                        _plan = TaskQueueType();

                        ctx.LastMTR().clear();
                        if (ctx.DebugMTR())
                        {
                            ctx.LastMTRDebug().clear();
                        }

                        ctx.HasPausedPartialPlan() = false;
                        ctx.ClearPartialPlanQueue();
                        ctx.IsDirty() = false;

                        return;
                    }
                }
            }
        }
        if (_currentTask != nullptr)
        {
            if (_currentTask->IsTypeOf(ITaskDerivedClassName::PrimitiveTask))
            {
                auto task = StaticCastPtr<PrimitiveTask>(_currentTask);
                if (task->Operator() != nullptr)
                {
                    for (size_t si = 0; si < task->ExecutingConditions().size();si++) 
                    {
                        auto condition = task->ExecutingConditions()[si];
                        // If a condition failed, then the plan failed to progress! A replan is required.
                        if (condition->IsValid(ctx) == false)
                        {
                            if (OnCurrentTaskExecutingConditionFailed)
                            {
                                OnCurrentTaskExecutingConditionFailed(task, condition);
                            }

                            _currentTask = nullptr;
                            _plan = TaskQueueType();

                            ctx.LastMTR().clear();
                            if (ctx.DebugMTR())
                            {
                                ctx.LastMTRDebug().clear();
                            }

                            ctx.HasPausedPartialPlan() = false;
                            ctx.ClearPartialPlanQueue();
                            ctx.IsDirty() = false;

                            return;
                        }
                    }

                    _LastStatus = task->Operator()->Update(ctx);

                    // If the operation finished successfully, we set task to null so that we dequeue the next task in the plan the
                    // following tick.
                    if (_LastStatus == TaskStatus::Success)
                    {
                        if (OnCurrentTaskCompletedSuccessfully)
                        {
                            OnCurrentTaskCompletedSuccessfully(task);
                        }

                        // All effects that is a result of running this task should be applied when the task is a success.
                        for (size_t si = 0; si < task->Effects().size();si++)
                        {
                            auto effect = task->Effects()[si];
                            if (effect->Type() == EffectType::PlanAndExecute)
                            {
                                if (OnApplyEffect)
                                {
                                    OnApplyEffect(effect);
                                }
                                effect->Apply(ctx);
                            }
                        }
                        _currentTask = nullptr;
                        if (_plan.size() == 0)
                        {
                            ctx.LastMTR().clear();
                            if (ctx.DebugMTR())
                            {
                                ctx.LastMTRDebug().clear();
                            }

                            ctx.IsDirty() = false;

                            if (allowImmediateReplan)
                                Tick<WSIDTYPE,WSVALTYPE,WSDERIVEDTYPE>(domain, static_cast<BaseContext<WSIDTYPE,WSVALTYPE,WSDERIVEDTYPE>&>(ctx), false);
                        }
                    }

                    // If the operation failed to finish, we need to fail the entire plan, so that we will replan the next tick.
                    else if (_LastStatus == TaskStatus::Failure)
                    {
                        if (OnCurrentTaskFailed)
                        {
                            OnCurrentTaskFailed(task);
                        }

                        _currentTask = nullptr;
                        _plan = TaskQueueType();

                        ctx.LastMTR().clear();
                        if (ctx.DebugMTR())
                        {
                            ctx.LastMTRDebug().clear();
                        }

                        ctx.HasPausedPartialPlan() = false;
                        ctx.ClearPartialPlanQueue();
                        ctx.IsDirty() = false;
                    }

                    // Otherwise the operation isn't done yet and need to continue.
                    else
                    {
                        if (OnCurrentTaskContinues)
                        {
                            OnCurrentTaskContinues(task);
                        }
                    }
                }
                else
                {
                    // This should not really happen if a domain is set up properly.
                    _currentTask = nullptr;
                    _LastStatus = TaskStatus::Failure;
                }
            }
        }

        if (_currentTask == nullptr && _plan.size() == 0 && isTryingToReplacePlan == false &&
            (decompositionStatus == DecompositionStatus::Failed || decompositionStatus == DecompositionStatus::Rejected))
        {
            _LastStatus = TaskStatus::Failure;
        }
    }
};
} // namespace FluidHTN
