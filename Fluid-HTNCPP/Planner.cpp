#include "pch.h"
#include "Contexts/Context.h"
#include "Conditions/Condition.h"
#include "Operators/Operator.h"
#include "Domain.h"
#include "Planners/Planner.h"

namespace FluidHTN
{
void Planner::Tick(class Domain& domain, IContext& ctx, bool allowImmediateReplan /*= true*/)
{
    FHTN_FATAL_EXCEPTION(ctx.IsInitialized(), "Context was not initialized");

    DecompositionStatus decompositionStatus = DecompositionStatus::Failed;
    bool                isTryingToReplacePlan = false;

    // Check whether state has changed or the current plan has finished running.
    // and if so, try to find a new plan.
    if (((_currentTask == nullptr) && (_plan.size() == 0)) || ctx.IsDirty())
    {
        std::queue<PartialPlanEntry> lastPartialPlanQueue;
        bool                         worldStateDirtyReplan = ctx.IsDirty();

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
                for (auto record : ctx.MethodTraversalRecord())
                {
                    ctx.LastMTR().push_back(record);
                }
                if(ctx.DebugMTR())
                {
                    ctx.LastMTRDebug().clear();
                    for (auto record : ctx.MTRDebug())
                    {
                        ctx.LastMTRDebug().push_back(record);
                    }
                }
            }
        }
        TaskQueueType newPlan;
        decompositionStatus = domain.FindPlan(ctx, newPlan);
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

            if (_currentTask != nullptr && _currentTask->IsTypeOf(ITaskDerivedClassName::PrimitiveTask) )
            {
                auto tPrimitive = std::static_pointer_cast<PrimitiveTask>(_currentTask);
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
                for (auto& record : ctx.MethodTraversalRecord())
                {
                    ctx.LastMTR().push_back(record);
                }
                if(ctx.DebugMTR())
                {
                    ctx.LastMTRDebug().clear();
                    for (auto record : ctx.MTRDebug())
                    {
                        ctx.LastMTRDebug().push_back(record);
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
                for (auto& record : ctx.LastMTR())
                {
                    ctx.MethodTraversalRecord().push_back(record);
                }
                ctx.LastMTR().clear();
                if(ctx.DebugMTR())
                {
                    ctx.LastMTRDebug().clear();
                    for (auto record : ctx.MTRDebug())
                    {
                        ctx.LastMTRDebug().push_back(record);
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
            if(OnNewTask != nullptr)
            {
                OnNewTask(_currentTask);
            }
            for (auto& condition : _currentTask->Conditions())
                // If a condition failed, then the plan failed to progress! A replan is required.
                if (condition->IsValid(ctx) == false)
                {
                    if(OnNewTaskConditionFailed)
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
    if (_currentTask != nullptr)
    {
        if (_currentTask->IsTypeOf(ITaskDerivedClassName::PrimitiveTask))
        {
			auto task = std::static_pointer_cast<PrimitiveTask>(_currentTask);
            if (task->Operator() != nullptr)
            {
                for (auto condition : task->ExecutingConditions())
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
                    for (auto effect : task->Effects())
                    {
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
                            Tick(domain, ctx, false);
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

} // namespace FluidHTN
