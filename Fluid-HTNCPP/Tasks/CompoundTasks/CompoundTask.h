#pragma once
#include "Tasks/Task.h"
#include "DebugInterfaces/DecompositionLogEntry.h"

namespace FluidHTN
{

class Slot;

class CompoundTask : public ITask
{
    std::vector<std::shared_ptr<ITask>> _Tasks;

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

    std::vector<std::shared_ptr<ITask>>& Subtasks() { return _Tasks; }

    bool AddSubTask(std::shared_ptr<ITask> subtask)
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
        IContext& ctx, std::shared_ptr<ITask>& task, int taskIndex, std::vector<int> oldStackDepth, TaskQueueType& result) = 0;
    virtual DecompositionStatus OnDecomposeCompoundTask(IContext&                      ctx,
                                                        std::shared_ptr<CompoundTask>& task,
                                                        int                            taskIndex,
                                                        std::vector<int>               oldStackDepth,
                                                        TaskQueueType&                 result) = 0;
    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, std::shared_ptr<Slot>& task, int taskIndex, std::vector<int> oldStackDepth, TaskQueueType& result) = 0;

    virtual void Log(IContext& ctx, std::string description, ConsoleColor color = ConsoleColor::White);
    
};

typedef IDecompositionLogEntry<ITask>      DecomposedCompoundTaskEntry;

} // namespace FluidHTN
