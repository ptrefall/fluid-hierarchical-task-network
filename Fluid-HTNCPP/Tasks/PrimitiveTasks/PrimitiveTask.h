#pragma once
#include "Tasks/Task.h"
#include "Effects/Effect.h"
#include "Operators/Operator.h"
#include "Conditions/Condition.h"

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
        _ExecutingConditions.push_back(condition);
        return true;
    }
    bool AddEffect(SharedPtr<IEffect>& effect) 
    {
        _Effects.push_back(effect);
        return true;
    }
    bool SetOperator(SharedPtr<IOperator>& action)  
    {
        FHTN_FATAL_EXCEPTION(_Operator == nullptr, "A Primitive Task can only contain a single operator");
        _Operator = action;
        return true;
    }

    void ApplyEffects(class IContext& ctx);
    
    void Stop(IContext& ctx) 
    {
        if (_Operator)
        {
            _Operator->Stop(ctx);
        }
    }

    virtual bool IsValid(IContext& ctx) override;
    

protected:
    virtual void Log(IContext& ctx, StringType description, ConsoleColor color = ConsoleColor::White);
};
} // namespace FluidHTN
