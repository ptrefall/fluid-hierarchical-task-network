#include "pch.h"
#include "Contexts/Context.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"

namespace FluidHTN
{

void PrimitiveTask::ApplyEffects(class IContext& ctx)
{
    if (ctx.GetContextState() == ContextState::Planning)
    {
        if (ctx.LogDecomposition())
        {
            Log(ctx, "PrimitiveTask.ApplyEffects", ConsoleColor::Yellow);
        }
    }
    if (ctx.LogDecomposition())
    {
        ctx.CurrentDecompositionDepth() += 1;
    }
    for (auto& effect : _Effects)
    {
        effect->Apply(ctx);
    }
    if (ctx.LogDecomposition())
    {
        ctx.CurrentDecompositionDepth() -= 1;
    }
}

bool PrimitiveTask::IsValid(IContext& ctx)
{
    if (ctx.LogDecomposition())
    {
        Log(ctx,"PrimitiveTask.IsValid check");
    }
    for (auto& condition : _Conditions)
    {
        if (ctx.LogDecomposition())
        {
            ctx.CurrentDecompositionDepth() += 1;
        }

        bool result = condition->IsValid(ctx);

        if (ctx.LogDecomposition())
        {
            ctx.CurrentDecompositionDepth() -= 1;
        }

        if (ctx.LogDecomposition())
        {
            Log(ctx,
                "PrimitiveTask.IsValid:"s + ToString(result) + " for condition "s +  condition->Name(),
                result ? ConsoleColor::DarkGreen : ConsoleColor::DarkRed);
        }
        if (!result)
        {
            return false;
        }
    }
    if (ctx.LogDecomposition())
    {
        Log(ctx, "PrimitiveTask.IsValid:Success!"s, ConsoleColor::Green);
    }
    return true;
}
void PrimitiveTask::Log(IContext& ctx, StringType description, ConsoleColor color /*= ConsoleColor::White*/)
{
    ctx.Log(_Name, description, ctx.CurrentDecompositionDepth() + 1, SharedFromThis(), color);
}
} // namespace FluidHTN