#pragma once
#include "Tasks/Task.h"
#include "DebugInterfaces/DecompositionLogEntry.h"

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
        _SubTypes.insert(ITaskDerivedClassName::CompoundTask);
    }

public:
    CompoundTask()
        : ITask(ITaskDerivedClassName::CompoundTask)
    {
    }

    ArrayType<SharedPtr<ITask>>& Subtasks() { return _Tasks; }

    bool AddSubTask(SharedPtr<ITask> subtask)
    {
        _Tasks.push_back(subtask);
        return true;
    }

    virtual DecompositionStatus OnIsValidFailed(IContext&) override { return DecompositionStatus::Failed; }

    DecompositionStatus Decompose(IContext& ctx, int startIndex, TaskQueueType& result);
    

    virtual bool IsValid(IContext& ctx) override;
    

protected:
    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeTask(
        IContext& ctx, SharedPtr<ITask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeCompoundTask(IContext&                      ctx,
                                                        SharedPtr<CompoundTask>& task,
                                                        int                            taskIndex,
                                                        ArrayType<int>               oldStackDepth,
                                                        TaskQueueType&                 result) = 0;
    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, SharedPtr<Slot>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) = 0;

    virtual void Log(IContext& ctx, StringType description, ConsoleColor color = ConsoleColor::White);
    
};

typedef IDecompositionLogEntry<ITask>      DecomposedCompoundTaskEntry;

} // namespace FluidHTN
