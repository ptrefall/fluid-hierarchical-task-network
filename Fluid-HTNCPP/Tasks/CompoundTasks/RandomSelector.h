#pragma once
#include "Tasks/CompoundTasks/Selector.h"

namespace FluidHTN
{

class RandomSelector : public Selector
{
    // ========================================================= FIELDS

protected:
    RandomSelector(ITaskDerivedClassName t)
        : Selector(t)
    {
		std::srand((unsigned int)std::time(nullptr));
    }

    // ========================================================= DECOMPOSITION

    /// <summary>
    ///     In a Random Selector decomposition, we simply select a sub-task randomly, and stick with it for the duration of the
    ///     plan as if it was the only sub-task.
    ///     So if the sub-task fail to decompose, that means the entire Selector failed to decompose (we don't try to decompose
    ///     any other sub-tasks).
    ///     Because of the nature of the Random Selector, we don't do any MTR tracking for it, since it doesn't do any real
    ///     branching.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
protected:
    virtual DecompositionStatus OnDecompose(IContext& ctx, int startIndex, TaskQueueType& result) override
    {
        _Plan = TaskQueueType();

        int taskIndex = startIndex + std::rand() %(Subtasks().size()  - startIndex);
        auto task = Subtasks()[taskIndex];

        std::vector<int> td;
        return OnDecomposeTask(ctx, task, taskIndex, td, result);
    }

public:
    RandomSelector()
        : Selector(ITaskDerivedClassName::RandomSelector)
    {
		std::srand((unsigned int)std::time(nullptr));
    }
    RandomSelector(const std::string& name)
        : Selector(ITaskDerivedClassName::RandomSelector)
    {
        _Name = name;
        std::srand((unsigned int)std::time(nullptr));
    }
};

} // namespace FluidHTN
