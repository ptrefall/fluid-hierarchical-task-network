#pragma once
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/PausePlanTask.h"
#include "Tasks/CompoundTasks/CompoundTask.h"
#include "Contexts/Context.h"

namespace FluidHTN
{

// IDecompose all does not exist in C++ because it causes a diamond inheritance and Sequence seems to be the only class that uses it.
// If more classes that need IDecomposeAll are created in the future  we can virtually inherit from a common ICompoundTask 
class Sequence : public CompoundTask
{
protected:
    TaskQueueType _Plan;

    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) override
    {
        _Plan = TaskQueueType();

        auto oldStackDepth = ctx.GetWorldStateChangeDepth();

        for (auto taskIndex = startIndex; taskIndex < Subtasks().size(); taskIndex++)
        {
            auto task = Subtasks()[taskIndex];

            if (ctx.LogDecomposition())
            {
                Log(ctx, "Selection::OnDecompose task index "s + ToString(taskIndex));
            }

            auto status = OnDecomposeTask(ctx, task, taskIndex, oldStackDepth, result);
            switch (status)
            {
                case DecompositionStatus::Rejected:
                case DecompositionStatus::Failed:
                case DecompositionStatus::Partial:
                {
                    return status;
                }
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
                Log(ctx, "Sequence.OnDecomposeTask:Failed:Task"s + task->Name() + ".IsValid returned false!"s, ConsoleColor::Red);
            }
            _Plan = TaskQueueType();
            ctx.TrimToStackDepth(oldStackDepth);
            result = _Plan;
            return task->OnIsValidFailed(ctx);
        }

        if (task->IsTypeOf(ITaskDerivedClassName::CompoundTask))
        {
            auto compoundTask = StaticCastPtr<CompoundTask>(task);
            return OnDecomposeCompoundTask(ctx, compoundTask, taskIndex, oldStackDepth, result);
        }
        else if (task->IsTypeOf(ITaskDerivedClassName::PrimitiveTask))
        {
            auto primitiveTask = StaticCastPtr<PrimitiveTask>(task);

            if (ctx.LogDecomposition())
            {
                Log(ctx, "Sequence.OnDecomposeTask:Pushed"s + primitiveTask->Name() + " to plan!"s, ConsoleColor::Blue);
            }
            primitiveTask->ApplyEffects(ctx);
            _Plan.push(task);
        }
        else if (task->IsTypeOf(ITaskDerivedClassName::PausePlanTask))
        {
            auto pausePlanTask = StaticCastPtr<PausePlanTask>(task);

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeTask:Return partial plan at index "s + ToString(taskIndex) + "!"s,
                    ConsoleColor::DarkBlue);
            }

            PartialPlanEntry pentry;
            pentry.Task = SharedFromThis();
            pentry.TaskIndex = taskIndex + 1;
            ctx.HasPausedPartialPlan() = true;
            ctx.PartialPlanQueue().push(pentry);

            result = _Plan;
            return DecompositionStatus::Partial;
        }
        else if (task->IsTypeOf(ITaskDerivedClassName::Slot))
        {
            auto slot = StaticCastPtr<Slot>(task);
            return OnDecomposeSlot(ctx, slot, taskIndex, oldStackDepth, result);
        }

        result = _Plan;
        auto s = (result.size() == 0 ? DecompositionStatus::Failed : DecompositionStatus::Succeeded);

        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "Sequence.OnDecomposeTask:"s + ToString((int)s),
                s == DecompositionStatus::Succeeded ? ConsoleColor::Green : ConsoleColor::Red);
        }
        return s;
    }
    virtual DecompositionStatus OnDecomposeCompoundTask(
        IContext& ctx, SharedPtr<CompoundTask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override
    {
        TaskQueueType subPlan;
        auto          status = task->Decompose(ctx, 0, subPlan);

        // If result is null, that means the entire planning procedure should cancel.
        if (status == DecompositionStatus::Rejected)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeCompoundTask:"s + ToString((int)status) + ": Decomposing "s + task->Name() +
                        " was rejected."s,
                    ConsoleColor::Red);
            }
            _Plan = TaskQueueType();
            ctx.TrimToStackDepth(oldStackDepth);
            result = TaskQueueType();
            return DecompositionStatus::Rejected;
        }

        // If the decomposition failed
        if (status == DecompositionStatus::Failed)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeCompoundTask:"s + ToString((int)status) + ": Decomposing "s + task->Name() + " failed.",
                    ConsoleColor::Red);
            }
            _Plan = TaskQueueType();
            ctx.TrimToStackDepth(oldStackDepth);
            result = _Plan;
            return DecompositionStatus::Failed;
        }

        while (subPlan.size() > 0)
        {
            auto p = subPlan.front();
            _Plan.push(p);

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeCompoundTask:Decomposing "s + task->Name() + " :Pushed "s + p->Name() + " to plan!"s,
                    ConsoleColor::Blue);
            }
            subPlan.pop();
        }

        if (ctx.HasPausedPartialPlan())
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeCompoundTask:Return partial plan at index "s + ToString(taskIndex) + "!"s,
                    ConsoleColor::DarkBlue);
            }
            if (taskIndex < Subtasks().size() - 1)
            {
                PartialPlanEntry pentry;
                pentry.Task = SharedFromThis();
                pentry.TaskIndex = taskIndex + 1;
                ctx.PartialPlanQueue().push(pentry);
            }

            result = _Plan;
            return DecompositionStatus::Partial;
        }

        result = _Plan;
        if (ctx.LogDecomposition())
        {
            Log(ctx, "Sequence.OnDecomposeCompoundTask:Succeeded!", ConsoleColor::Green);
        }
        return DecompositionStatus::Succeeded;
    }
    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, SharedPtr<Slot>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override
    {
        TaskQueueType subPlan;
        auto          status = task->Decompose(ctx, 0, subPlan);

        // If result is null, that means the entire planning procedure should cancel.
        if (status == DecompositionStatus::Rejected)
        {

            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeSlot: "s + ToString((int)status) + ": Decomposing "s + task->Name() + " was rejected."s,
                    ConsoleColor::Red);
            }
            _Plan = TaskQueueType();
            ctx.TrimToStackDepth(oldStackDepth);

            result = TaskQueueType();
            return DecompositionStatus::Rejected;
        }

        // If the decomposition failed
        if (status == DecompositionStatus::Failed)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeSlot: "s + ToString((int)status) + ": Decomposing "s + task->Name() + " was failed."s,
                    ConsoleColor::Red);
            }
            _Plan = TaskQueueType();
            ctx.TrimToStackDepth(oldStackDepth);
            result = _Plan;
            return DecompositionStatus::Failed;
        }

        while (subPlan.size() > 0)
        {
            auto p = subPlan.front();
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "Sequence.OnDecomposeSlot:Return partial plan at index "s + ToString(taskIndex) + "!"s,
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
                    "Sequence.OnDecomposeSlot:Return partial plan at index "s + ToString(taskIndex) + "!"s,
                    ConsoleColor::DarkBlue);
            }
            if (taskIndex < Subtasks().size() - 1)
            {
                PartialPlanEntry pentry;
                pentry.Task = SharedFromThis();
                pentry.TaskIndex = taskIndex + 1;
                ctx.PartialPlanQueue().push(pentry);
            }

            result = _Plan;
            return DecompositionStatus::Partial;
        }

        result = _Plan;
        if (ctx.LogDecomposition())
        {
            Log(ctx, "Sequence.OnDecomposeSlot:Succeeded!", ConsoleColor::Green);
        }
        return DecompositionStatus::Succeeded;
    }

public:
    Sequence()
        : CompoundTask(ITaskDerivedClassName::SequenceCompoundTask)
    {
    }
    explicit Sequence(const StringType& name)
        : CompoundTask(ITaskDerivedClassName::SequenceCompoundTask)
    {
        _Name = name;
    }
    virtual bool IsValid(IContext& ctx) override
    {
        if (CompoundTask::IsValid(ctx) == false)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx, "Sequence.IsValid failed, preconditions not met!"s, ConsoleColor::Red);
            }
            return false;
        }
        if (Subtasks().size() == 0)
        {
            if (ctx.LogDecomposition())
            {
                Log(ctx, "Sequence.IsValid failed: No sub-tasks!"s, ConsoleColor::Red);
            }
            return false;
        }
        if (ctx.LogDecomposition())
        {
            Log(ctx, "Sequence.IsValid Success!"s, ConsoleColor::Green);
        }
        return true;
    }
};

} // namespace FluidHTN
