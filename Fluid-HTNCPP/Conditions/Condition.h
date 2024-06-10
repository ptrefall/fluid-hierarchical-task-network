#pragma once
#include "Contexts/Context.h"

namespace FluidHTN
{
class ICondition : public EnableSharedFromThis<ICondition>
{
protected:
    StringType _Name;

public:
    virtual ~ICondition(){}
    StringType& Name() { return _Name; }
    virtual bool IsValid(class IContext&) = 0;
};

typedef IDecompositionLogEntry<ICondition> DecomposedConditionEntry;
typedef std::function<bool(IContext&)> FunctionConditionType;

class FuncCondition : public ICondition
{
    FunctionConditionType _func;

public:
    FuncCondition(const StringType& name, FunctionConditionType func)
        : _func(func)
    {
        _Name = name;
    }
    bool IsValid(IContext& ctx)
    {
        bool result = false;
        if (_func)
        {
            result = _func(ctx);
        }
        if (ctx.LogDecomposition())
        {
            ctx.Log(_Name,
                    "FuncCondition.IsValid:"s + ToString(result),
                    ctx.CurrentDecompositionDepth() + 1,
                    SharedFromThis());
        }

        return result;
    }
};
} // namespace FluidHTN
