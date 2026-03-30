#pragma once
#include "Effects/EffectType.h"
#include "DebugInterfaces/DecompositionLogEntry.h"
#include "Contexts/Context.h"

namespace FluidHTN
{
class IEffect  : public EnableSharedFromThis<IEffect>
{
protected:
    StringType _Name;
    EffectType  _Type;


public:
    virtual ~IEffect(){}
    const StringType& Name() { return _Name; }
    EffectType         Type() { return _Type; }
    virtual void       Apply(class IContext& ctx) = 0;
};

typedef IDecompositionLogEntry<IEffect>    DecomposedEffectEntry;
typedef std::function<void(IContext&, EffectType)> ActionType;

class ActionEffect : public IEffect
{
    ActionType _action;

public:
    ActionEffect() =delete;
    ActionEffect(const StringType name, EffectType type, ActionType action)
    {
        _Name = name;
        _Type = type;
        _action = action;
    }
    void Apply(IContext& ctx)
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
};
} // namespace FluidHTN
