#pragma once
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{

// IDecompose all does not exist in C++ because it causes a diamond inheritance and Sequence seems to be the only class that uses it.
// If more classes that need IDecomposeAll are created in the future  we can virtually inherit from a common ICompoundTask 
class Sequence : public CompoundTask
{
protected:
    TaskQueueType _Plan;

    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeTask(
        IContext& ctx, SharedPtr<ITask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeCompoundTask(
        IContext& ctx, SharedPtr<CompoundTask>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override;

    virtual DecompositionStatus OnDecomposeSlot(
        IContext& ctx, SharedPtr<Slot>& task, int taskIndex, ArrayType<int> oldStackDepth, TaskQueueType& result) override;

public:
    Sequence() : CompoundTask(ITaskDerivedClassName::SequenceCompoundTask){}
    explicit Sequence(const StringType& name) : CompoundTask(ITaskDerivedClassName::SequenceCompoundTask) { _Name = name; }
    virtual bool IsValid(IContext& ctx) override;
    
};

} // namespace FluidHTN
