#pragma once
#include "Tasks/Task.h"

namespace FluidHTN
{
class IOperator
{
public:
    virtual TaskStatus Update(class IContext& ctx) = 0;
    virtual void       Stop(IContext& ctx) = 0;
};

typedef std::function<TaskStatus(IContext& ctx)> FuncOperatorType;
typedef std::function<void(IContext&)>           StopOperatorType;

class FuncOperator : public IOperator
{
    FuncOperatorType _func;
    StopOperatorType _funcStop;

public:
    virtual ~FuncOperator(){}
    FuncOperator(FuncOperatorType func, StopOperatorType stp = nullptr)
    {
        _func = func;
        _funcStop = stp;
    }
    virtual TaskStatus Update(IContext& ctx) override
    {
        if (!_func)
        {
            return TaskStatus::Failure;
        }
        return _func(ctx);
    }
    virtual void Stop(IContext& ctx) override
    {
        if (_funcStop != nullptr)
        {

            _funcStop(ctx);
        }
    }
};
} // namespace FluidHTN
