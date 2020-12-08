#pragma once

#include "Tasks/Task.h"
#include "Contexts/Context.h"

namespace FluidHTN
{

class PausePlanTask : public ITask
{
protected:
    virtual void Log(IContext& ctx, std::string description)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), shared_from_this(), ConsoleColor::Green);
    }

public:
    PausePlanTask()
        : ITask(ITaskDerivedClassName::PausePlanTask)
    {
    }
    virtual DecompositionStatus OnIsValidFailed(IContext&) { return DecompositionStatus::Failed; }

    virtual bool AddCondition(std::shared_ptr<ICondition>&) override
    {
        FHTN_FATAL_EXCEPTION(false, "PausePlan Tasks do not support conditions");
    }

    bool AddEffect(std::shared_ptr<class IEffect>&) { FHTN_FATAL_EXCEPTION(false, "Pause Plan tasks do not support effects"); }

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
