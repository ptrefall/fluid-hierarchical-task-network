#pragma once
#include "Contexts/Context.h"

namespace FluidHTN
{
class ICondition : public std::enable_shared_from_this<ICondition>
{
protected:
    std::string _Name;

public:
    std::string& Name() { return _Name; }
    virtual bool IsValid(class IContext&) = 0;
};

typedef IDecompositionLogEntry<ICondition> DecomposedConditionEntry;
typedef std::function<bool(IContext&)> FunctionConditionType;

class FuncCondition : public ICondition
{
    FunctionConditionType _func;

public:
    FuncCondition(const std::string& name, FunctionConditionType func)
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
                    "FuncCondition.IsValid:"s + std::to_string(result),
                    ctx.CurrentDecompositionDepth() + 1,
                    shared_from_this());
        }

        return result;
    }
};
} // namespace FluidHTN
