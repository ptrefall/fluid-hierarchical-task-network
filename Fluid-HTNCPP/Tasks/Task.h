#pragma once
#include "Tasks/CompoundTasks/DecompositionStatus.h"

namespace FluidHTN
{

class CompoundTask;

class IContext;

class ICondition;

enum class TaskStatus
{
    Continue,
    Success,
    Failure
};

// custom RTTI, because most game engines don't like C++ RTTI
enum class ITaskDerivedClassName
{
    ITaskType,
    CompoundTask,
    PrimitiveTask,
    SelectorCompoundTask,
    TaskRoot,
    SequenceCompoundTask,
    Slot,
    PausePlanTask,
    RandomSelector
};

// not strictly an "interface", but design patterns are so 1999
class ITask : public std::enable_shared_from_this<ITask>
{
    ITask() {}

    ITaskDerivedClassName _Type = ITaskDerivedClassName::ITaskType;

protected:
    std::unordered_set<ITaskDerivedClassName> _SubTypes;
    explicit ITask(ITaskDerivedClassName n)
    {
        _Type = n;
        _SubTypes.insert(n);
    }
    std::string                              _Name;
    std::shared_ptr<CompoundTask>            _Parent;
    std::vector<std::shared_ptr<ICondition>> _Conditions;
    TaskStatus                               _LastStatus = TaskStatus::Failure;

public:
    ITaskDerivedClassName GetType() const { return _Type; }
    bool                  IsTypeOf(ITaskDerivedClassName thetype)
    {
        return ((thetype == _Type) || (thetype == ITaskDerivedClassName::ITaskType) ||
                (_SubTypes.find(thetype) != _SubTypes.end()));
    }

    virtual std::string& Name() { return _Name; }

    virtual std::shared_ptr<CompoundTask>& Parent() { return _Parent; }

    virtual std::vector<std::shared_ptr<ICondition>>& Conditions() { return _Conditions; }

    virtual TaskStatus LastStatus() { return _LastStatus; }

    virtual bool AddCondition(std::shared_ptr<ICondition>& Condition)
    {
        _Conditions.push_back(Condition);
        return true;
    }

    virtual bool IsValid(IContext& ctx) = 0;

    virtual DecompositionStatus OnIsValidFailed(IContext& ctx) = 0;
};

typedef std::queue<std::shared_ptr<ITask>> TaskQueueType;

} // namespace FluidHTN