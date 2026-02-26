#pragma once
#include "CoreIncludes/STLTypes.h"
#include "Tasks/Task.h"
#include "Effects/Effect.h"
#include "Operators/Operator.h"
#include "Conditions/Condition.h"
#include "Contexts/Context.h"

namespace FluidHTN
{
class PrimitiveTask : public ITask
{
protected:
    ArrayType<SharedPtr<class ICondition>> _ExecutingConditions;
    SharedPtr<class IOperator>               _Operator;
    ArrayType<SharedPtr<class IEffect>>    _Effects;

public:
    PrimitiveTask() : ITask(ITaskDerivedClassName::PrimitiveTask){}
    explicit PrimitiveTask(const StringType& name):ITask(ITaskDerivedClassName::PrimitiveTask) { _Name = name; }
    virtual const ArrayType<SharedPtr<ICondition>>& ExecutingConditions() const { return _ExecutingConditions; }

    virtual SharedPtr<IOperator> Operator() { return _Operator; }
    virtual ArrayType<SharedPtr<IEffect>>& Effects() { return _Effects; }
    virtual DecompositionStatus OnIsValidFailed(IContext&) override { return DecompositionStatus::Failed; }

    // If these functions took non-reference parameters, they could be construction inline in the function call.
    // However, we probably want to share implementations of conditions across multiple domains, so avoiding the
    // shared_ptr copy costs per function call seems reasonable.
    bool AddExecutingCondition(SharedPtr<ICondition>& condition) 
    {
        _ExecutingConditions.Add(condition);
        return true;
    }
    bool AddEffect(SharedPtr<IEffect>& effect) 
    {
        _Effects.Add(effect);
        return true;
    }
    bool SetOperator(SharedPtr<IOperator>& action)  
    {
        FHTN_FATAL_EXCEPTION(_Operator == nullptr, "A Primitive Task can only contain a single operator");
        _Operator = action;
        return true;
    }

    void ApplyEffects(class IContext& ctx)
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
        for(size_t si = 0; si < _Effects.size();si++)
        {
            auto& effect = _Effects[si];
            effect->Apply(ctx);
        }
        if (ctx.LogDecomposition())
        {
            ctx.CurrentDecompositionDepth() -= 1;
        }
    }
    void Stop(IContext& ctx)
    {
        if (_Operator)
        {
            _Operator->Stop(ctx);
        }
    }

    virtual bool IsValid(IContext& ctx) override
    {
        if (ctx.LogDecomposition())
        {
            Log(ctx, "PrimitiveTask.IsValid check");
        }
        for (size_t si = 0; si < _Conditions.size(); si++)
        {
            auto& condition = _Conditions[si];
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
                    "PrimitiveTask.IsValid:"s + ToString(result) + " for condition "s + condition->Name(),
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

protected:
    virtual void Log(IContext& ctx, StringType description, ConsoleColor color = ConsoleColor::White)
    {
        ctx.Log(_Name, description, ctx.CurrentDecompositionDepth() + 1, SharedFromThis(), color);
    }
};
} // namespace FluidHTN
