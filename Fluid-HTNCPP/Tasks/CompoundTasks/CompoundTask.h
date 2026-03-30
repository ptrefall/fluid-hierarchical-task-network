#pragma once
#include "CoreIncludes/STLTypes.h"
#include "Tasks/Task.h"
#include "Conditions/Condition.h"
#include "DebugInterfaces/DecompositionLogEntry.h"
#include "Contexts/Context.h"

namespace FluidHTN
{

class Slot;

class CompoundTask : public ITask
{
    ArrayType<SharedPtr<ITask>> _Tasks;

protected:
    CompoundTask(ITaskDerivedClassName t)
        : ITask(t)
    {
        _SubTypes.Insert(ITaskDerivedClassName::CompoundTask);
    }

public:
    CompoundTask()
        : ITask(ITaskDerivedClassName::CompoundTask)
    {
    }

    ArrayType<SharedPtr<ITask>>& Subtasks() { return _Tasks; }

    bool AddSubTask(SharedPtr<ITask> subtask)
    {
        _Tasks.Add(subtask);
        return true;
    }

    virtual DecompositionStatus OnIsValidFailed(IContext&) override { return DecompositionStatus::Failed; }

    DecompositionStatus Decompose(IContext& ctx, int startIndex, TaskQueueType& result)
    {
        if (ctx.LogDecomposition())
        {
            ctx.CurrentDecompositionDepth() += 1;
        }
        auto status = OnDecompose(ctx, startIndex, result);
        if (ctx.LogDecomposition())
        {
            ctx.CurrentDecompositionDepth() -= 1;
        }

        return status;
    }

    virtual bool IsValid(IContext& ctx) override
    {
        for(size_t si = 0; si < _Conditions.size();si++)
        {
            auto& condition = _Conditions[si];
            bool result = condition->IsValid(ctx);
            if (ctx.LogDecomposition())
            {
                Log(ctx,
                    "CompoundTask.IsValid: "s + ToString(result) + " for "s + condition->Name(),
                    result ? ConsoleColor::DarkGreen : ConsoleColor::DarkRed);
            }
            if (!result)
            {
                return false;
            }
        }
        return true;
    }

protected:
    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeTask(
        IContext& ctx, SharedPtr<ITask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeCompoundTask(
        IContext& ctx, SharedPtr<CompoundTask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, SharedPtr<Slot>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) = 0;

    virtual void Log(IContext& ctx, StringType description, ConsoleColor color = ConsoleColor::White)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), SharedFromThis(), color);
    }
};

typedef IDecompositionLogEntry<ITask> DecomposedCompoundTaskEntry;

} // namespace FluidHTN
