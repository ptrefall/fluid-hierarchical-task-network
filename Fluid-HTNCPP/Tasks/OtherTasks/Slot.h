#pragma once
#include "Contexts/Context.h"
#include "Tasks/Task.h"
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{

class Slot : public ITask
{
    int _SlotId;

    SharedPtr<class CompoundTask> _Subtask;

public:
    Slot():ITask(ITaskDerivedClassName::Slot){}
    int                  SlotId() { return _SlotId; }
    void                 SlotId(int s) { _SlotId = s; }
    const SharedPtr<CompoundTask> Subtask() { return _Subtask; }

    virtual DecompositionStatus OnIsValidFailed(IContext& ) override { return DecompositionStatus::Failed; }

    virtual bool AddCondition(SharedPtr<ICondition>&) override
    {
        FHTN_FATAL_EXCEPTION(false,"Slot Tasks do not support conditions");
    }
    bool Set(SharedPtr<CompoundTask> subtask)
    {
        if (_Subtask != nullptr)
        {
            return false;
        }

        _Subtask = subtask;
        return true;
    }
    void clear() { _Subtask = nullptr; }

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
            Log(ctx, "Slot.IsValid:"s + ToString(result) + "!"s, result ? ConsoleColor::Green : ConsoleColor::Red);
        }
            return result;
    }
protected:
    virtual void Log(IContext& ctx, StringType description, ConsoleColor color = ConsoleColor::White)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), SharedFromThis(), color);
    }
};

} // namespace FluidHTN
