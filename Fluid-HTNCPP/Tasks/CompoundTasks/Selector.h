#pragma once
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{
class Selector : public CompoundTask
{
protected:
    Selector(ITaskDerivedClassName t): CompoundTask(t){}
    TaskQueueType _Plan;

    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeTask(
        IContext& ctx, std::shared_ptr<ITask>& task, int taskIndex, std::vector<int> oldStackDepth, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeCompoundTask(
        IContext& ctx, std::shared_ptr<CompoundTask>& task, int taskIndex, std::vector<int> oldStackDepth, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, std::shared_ptr<Slot>& task, int taskIndex, std::vector<int> oldStackDepth, TaskQueueType& result) override;

public:
    Selector() : CompoundTask(ITaskDerivedClassName::SelectorCompoundTask){}
    explicit Selector(const std::string& name) : CompoundTask(ITaskDerivedClassName::SelectorCompoundTask){ _Name = name; }
    virtual bool IsValid(IContext& ctx) override;
};

class TaskRoot : public Selector
{
public:
    TaskRoot():Selector(ITaskDerivedClassName::TaskRoot){}
};
} // namespace FluidHTN
