#include "pch.h"
#include "Contexts/Context.h"
#include "Effect.h"

namespace FluidHTN
{

void ActionEffect::Apply(IContext& ctx)
{
    if (ctx.LogDecomposition())
    {
        ctx.Log(_Name, "ActionEffect"s + std::to_string((int)_Type), ctx.CurrentDecompositionDepth() + 1, shared_from_this());
    }
    if (_action)
    {
        _action(ctx, _Type);
    }
}
} // namespace FluidHTN
