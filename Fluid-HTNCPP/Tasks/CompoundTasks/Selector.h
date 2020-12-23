#pragma once
#include "CoreIncludes/STLTypes.h"
#include "Tasks/CompoundTasks/CompoundTask.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/OtherTasks/Slot.h"
#include "Contexts/Context.h"

namespace FluidHTN
{
class Selector : public CompoundTask
{
protected:
    Selector(ITaskDerivedClassName t)
        : CompoundTask(t)
    {
    }
    TaskQueueType _Plan;

    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) override
    {
        _Plan = TaskQueueType();

        for (auto taskIndex = startIndex; taskIndex < Subtasks().size(); taskIndex++)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecompose:Task index: "s + ToString(taskIndex) + ": "s +
                        (Subtasks()[taskIndex] ? Subtasks()[taskIndex]->Name() : " no task"s));
            }
            // If the last plan is still running, we need to check whether the
            // new decomposition can possibly beat it.
            if (ctx.LastMTR().size() > 0)
            {
                if (ctx.MethodTraversalRecord().size() < ctx.LastMTR().size())
                {
                    // If the last plan's traversal record for this decomposition layer
                    // has a smaller index than the current task index we're about to
                    // decompose, then the new decomposition can't possibly beat the
                    // running plan, so we cancel finding a new plan.
                    auto currentDecompositionIndex = ctx.MethodTraversalRecord().size();
                    if (ctx.LastMTR()[currentDecompositionIndex] < taskIndex)
                    {
                        ctx.MethodTraversalRecord().Add(-1);
                        if (ctx.DebugMTR())
                        {
                            ctx.MTRDebug().Add("REPLAN FAIL "s + Subtasks()[taskIndex]->Name());
                        }

                        if (ctx.LogDecomposition())
                        {
                            Log(ctx,
                                "Selector.OnDecompose:Rejected:Index "s + ToString(currentDecompositionIndex) +
                                    " is beat by last method traversal record!"s,
                                ConsoleColor::Red);
                        }
                        result = TaskQueueType();
                        return DecompositionStatus::Rejected;
                    }
                }
            }

            auto task = Subtasks()[taskIndex];

            auto status = OnDecomposeTask(ctx, task, taskIndex, ArrayType<int>(), result);
            switch (status)
            {
                case DecompositionStatus::Rejected:
                case DecompositionStatus::Succeeded:
                case DecompositionStatus::Partial:
                    return status;
                case DecompositionStatus::Failed:
                default:
                    continue;
            }
        }

        result = _Plan;
        return (result.size() == 0 ? DecompositionStatus::Failed : DecompositionStatus::Succeeded);
    }
    virtual DecompositionStatus OnDecomposeTask(
        IContext& ctx, SharedPtr<ITask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override
    {
        if (task->IsValid(ctx) == false)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selector.OnDecomposeTask:Failed:Task "s + task->Name() + ".IsValid returned false!"s, ConsoleColor::Red);
            }
            result = _Plan;
            return task->OnIsValidFailed(ctx);
        }

        if (task->IsTypeOf(ITaskDerivedClassName::CompoundTask))
        {
            auto compoundTask = StaticCastPtr<CompoundTask>(task);
            return OnDecomposeCompoundTask(ctx, compoundTask, taskIndex, ArrayType<int>(), result);
        }

        if (task->IsTypeOf(ITaskDerivedClassName::PrimitiveTask))
        {
            auto primitiveTask = StaticCastPtr<PrimitiveTask>(task);
            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selector.OnDecomposeTask:Pushed "s + primitiveTask->Name() + "to plan!"s, ConsoleColor::Blue);
            }
            primitiveTask->ApplyEffects(ctx);
            _Plan.push(task);
        }

        if (task->IsTypeOf(ITaskDerivedClassName::Slot))
        {
            auto slot = StaticCastPtr<Slot>(task);
            return OnDecomposeSlot(ctx, slot, taskIndex, ArrayType<int>(), result);
        }

        result = _Plan;
        auto status = (result.size() == 0 ? DecompositionStatus::Failed : DecompositionStatus::Succeeded);

        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "Selector.OnDecomposeTask " + ToString((int)status) + "!"s,
                status == DecompositionStatus::Succeeded ? ConsoleColor::Green : ConsoleColor::Red);
        }
        return status;
    }
    virtual DecompositionStatus OnDecomposeCompoundTask(
        IContext& ctx, SharedPtr<CompoundTask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override
    {
        // We need to record the task index before we decompose the task,
        // so that the traversal record is set up in the right order.
        ctx.MethodTraversalRecord().Add(taskIndex);
        if (ctx.DebugMTR())
        {
            ctx.MTRDebug().Add(task->Name());
        }

        TaskQueueType subPlan;
        auto          status = task->Decompose(ctx, 0, subPlan);

        // If status is rejected, that means the entire planning procedure should cancel.
        if (status == DecompositionStatus::Rejected)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeCompoundTask:"s + ToString((int)status) + ": Decomposing "s + task->Name() +
                        " was rejected."s,
                    ConsoleColor::Red);
            }
            result = TaskQueueType();
            return DecompositionStatus::Rejected;
        }

        // If the decomposition failed
        if (status == DecompositionStatus::Failed)
        {
            // Remove the taskIndex  (pushed at top of function) if it failed to decompose.
            ctx.MethodTraversalRecord().PopBack();
            if (ctx.DebugMTR())
            {
                ctx.MTRDebug().PopBack();
            }
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeCompoundTask:"s + ToString((int)status) + ": Decomposing "s + task->Name() + " failed."s,
                    ConsoleColor::Red);
            }
            result = _Plan;
            return DecompositionStatus::Failed;
        }

        while (subPlan.size() > 0)
        {
            auto p = subPlan.front();
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeCompoundTask:Decomposing "s + task->Name() + ":Pushed " + p->Name() + " to plan!"s,
                    ConsoleColor::Blue);
            }
            _Plan.push(p);
            subPlan.pop();
        }
        if (ctx.HasPausedPartialPlan())
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeCompoundTask:Return partial plan at index "s + ToString(taskIndex) + "!"s,
                    ConsoleColor::DarkBlue);
            }
            result = _Plan;
            return DecompositionStatus::Partial;
        }

        result = _Plan;
        auto s = (result.size() == 0 ? DecompositionStatus::Failed : DecompositionStatus::Succeeded);
        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "Selector.OnDecomposeCompoundTask:"s + ToString((int)s),
                s == DecompositionStatus::Succeeded ? ConsoleColor::Green : ConsoleColor::Red);
        }
        return s;
    }
    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, SharedPtr<Slot>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override
    {
        // We need to record the task index before we decompose the task,
        // so that the traversal record is set up in the right order.
        ctx.MethodTraversalRecord().Add(taskIndex);
        if (ctx.DebugMTR())
        {
            ctx.MTRDebug().Add(task->Name());
        }

        TaskQueueType subPlan;
        auto          status = task->Decompose(ctx, 0, subPlan);

        // If status is rejected, that means the entire planning procedure should cancel.
        if (status == DecompositionStatus::Rejected)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeSlot:"s + ToString((int)status) + ": Decomposing "s + task->Name() + " was rejected."s,
                    ConsoleColor::Red);
            }
            result = TaskQueueType();
            return DecompositionStatus::Rejected;
        }

        // If the decomposition failed
        if (status == DecompositionStatus::Failed)
        {
            // Remove the taskIndex if it failed to decompose.
            ctx.MethodTraversalRecord().PopBack();
            if (ctx.DebugMTR())
            {
                ctx.MTRDebug().PopBack();
            }
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeSlot:"s + ToString((int)status) + ": Decomposing "s + task->Name() + " failed."s,
                    ConsoleColor::Red);
            }
            result = _Plan;
            return DecompositionStatus::Failed;
        }

        while (subPlan.size() > 0)
        {
            auto p = subPlan.front();
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Selector.OnDecomposeSlot:Decomposing "s + task->Name() + ":Pushed "s + p->Name() + " to plan!"s,
                    ConsoleColor::Blue);
            }
            _Plan.push(p);
            subPlan.pop();
        }

        if (ctx.HasPausedPartialPlan())
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selector.OnDecomposeSlot:Return partial plan!"s, ConsoleColor::DarkBlue);
            }
            result = _Plan;
            return DecompositionStatus::Partial;
        }

        result = _Plan;
        auto s = (result.size() == 0 ? DecompositionStatus::Failed : DecompositionStatus::Succeeded);
        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "Selector.OnDecomposeSlot:"s + ToString((int)s) + "!"s,
                s == DecompositionStatus::Succeeded ? ConsoleColor::Green : ConsoleColor::Red);
        }
        return s;
    }

public:
    Selector()
        : CompoundTask(ITaskDerivedClassName::SelectorCompoundTask)
    {
    }
    explicit Selector(const StringType& name)
        : CompoundTask(ITaskDerivedClassName::SelectorCompoundTask)
    {
        _Name = name;
    }
    virtual bool IsValid(IContext& ctx) override
    {
        if (CompoundTask::IsValid(ctx) == false)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selector.IsValid:Failed:Preconditions not met!"s, ConsoleColor::Red);
            }
            return false;
        }
        if (Subtasks().size() == 0)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selector.IsValid:Failed:No sub-tasks!"s, ConsoleColor::Red);
            }
            return false;
        }
        if (ctx.LogDecomposition())
        {
            Log(ctx, "Selector.IsValid:Success!"s, ConsoleColor::Green);
        }
        return true;
    }
};

class TaskRoot : public Selector
{
public:
    TaskRoot()
        : Selector(ITaskDerivedClassName::TaskRoot)
    {
    }
};
} // namespace FluidHTN
