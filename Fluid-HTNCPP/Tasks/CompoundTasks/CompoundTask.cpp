#include "pch.h"
#include "Contexts/Context.h"
#include "Conditions/Condition.h"
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{

DecompositionStatus CompoundTask::Decompose(IContext& ctx, int startIndex, TaskQueueType& result)
{
    if (ctx.LogDecomposition())
    {
        ctx.CurrentDecompositionDepth() += 1;
    }
    auto status = OnDecompose(ctx, startIndex, result);
    if (ctx.LogDecomposition())
    {
        ctx.CurrentDecompositionDepth() -= 1;
    }

    return status;
}

bool CompoundTask::IsValid(IContext& ctx)
{
    for (auto& condition : _Conditions)
    {
        bool result = condition->IsValid(ctx);
        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "CompoundTask.IsValid: "s + ToString(result) + " for "s + condition->Name(),
                result ? ConsoleColor::DarkGreen : ConsoleColor::DarkRed);
        }
        if (!result)
        {
            return false;
        }
    }
    return true;
}

void CompoundTask::Log(IContext& ctx, StringType description, ConsoleColor color /*= ConsoleColor::White*/)
{
    ctx.Log(_Name, description, ctx.CurrentDecompositionDepth(), SharedFromThis(), color);
}
} // namespace FluidHTN