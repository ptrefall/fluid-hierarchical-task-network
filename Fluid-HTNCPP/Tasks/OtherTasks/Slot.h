#pragma once
#include "Contexts/Context.h"
#include "Tasks/Task.h"
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{

class Slot : public ITask
{
    int _SlotId;

    std::shared_ptr<class CompoundTask> _Subtask;

public:
    Slot():ITask(ITaskDerivedClassName::Slot){}
    int                  SlotId() { return _SlotId; }
    void                 SlotId(int s) { _SlotId = s; }
    const std::shared_ptr<CompoundTask> Subtask() { return _Subtask; }

    virtual DecompositionStatus OnIsValidFailed(IContext& ) override { return DecompositionStatus::Failed; }

    virtual bool AddCondition(std::shared_ptr<ICondition>&) override
    {
        FHTN_FATAL_EXCEPTION(false,"Slot Tasks do not support conditions");
    }
    bool Set(std::shared_ptr<CompoundTask> subtask)
    {
        if (_Subtask != nullptr)
        {
            return false;
        }

        _Subtask = subtask;
        return true;
    }
    void Clear() { _Subtask = nullptr; }

    DecompositionStatus Decompose(IContext& ctx, int startIndex, TaskQueueType& result)
    {
        if (_Subtask != nullptr)
        {
            return _Subtask->Decompose(ctx, startIndex, result);
        }
        result = TaskQueueType();
        return DecompositionStatus::Failed;
    }

    virtual bool IsValid(IContext& ctx) override
    {
        bool result =   (_Subtask != nullptr);
        if (ctx.LogDecomposition())
        {
            Log(ctx, "Slot.IsValid:"s + std::to_string(result) + "!"s, result ? ConsoleColor::Green : ConsoleColor::Red);
        }
            return result;
    }
protected:
    virtual void Log(IContext& ctx, std::string description, ConsoleColor color = ConsoleColor::White)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), shared_from_this(), color);
    }
};

} // namespace FluidHTN
