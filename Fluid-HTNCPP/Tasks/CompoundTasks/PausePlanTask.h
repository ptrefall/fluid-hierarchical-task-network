#pragma once

#include "Tasks/Task.h"
#include "Contexts/Context.h"

namespace FluidHTN
{

class PausePlanTask : public ITask
{
protected:
    virtual void Log(IContext& ctx, StringType description)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), SharedFromThis(), ConsoleColor::Green);
    }

public:
    PausePlanTask()
        : ITask(ITaskDerivedClassName::PausePlanTask)
    {
    }
    virtual DecompositionStatus OnIsValidFailed(IContext&) { return DecompositionStatus::Failed; }

    virtual bool AddCondition(SharedPtr<ICondition>&) override
    {
        FHTN_FATAL_EXCEPTION(false, "PausePlan Tasks do not support conditions");
        return false;
    }

    bool AddEffect(SharedPtr<class IEffect>&) { FHTN_FATAL_EXCEPTION(false, "Pause Plan tasks do not support effects"); }

    void ApplyEffects(IContext&) {}

    virtual bool IsValid(IContext& ctx) override
    {
        if (ctx.LogDecomposition())
        {
            Log(ctx, "PausePlanTask.IsValid:Success!");
        }
        return true;
    };
};
} // namespace FluidHTN
