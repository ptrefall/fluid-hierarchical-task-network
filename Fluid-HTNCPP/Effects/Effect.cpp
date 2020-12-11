#include "pch.h"
#include "Contexts/Context.h"
#include "Effect.h"

namespace FluidHTN
{

void ActionEffect::Apply(IContext& ctx)
{
    if (ctx.LogDecomposition())
    {
        ctx.Log(_Name, "ActionEffect"s + ToString((int)_Type), ctx.CurrentDecompositionDepth() + 1, SharedFromThis());
    }
    if (_action)
    {
        _action(ctx, _Type);
    }
}
} // namespace FluidHTN
