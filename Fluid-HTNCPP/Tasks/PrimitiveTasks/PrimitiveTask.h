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
    std::vector<std::shared_ptr<class ICondition>> _ExecutingConditions;
    std::shared_ptr<class IOperator>               _Operator;
    std::vector<std::shared_ptr<class IEffect>>    _Effects;

public:
    PrimitiveTask() : ITask(ITaskDerivedClassName::PrimitiveTask){}
    explicit PrimitiveTask(const std::string& name):ITask(ITaskDerivedClassName::PrimitiveTask) { _Name = name; }
    virtual const std::vector<std::shared_ptr<ICondition>>& ExecutingConditions() const { return _ExecutingConditions; }

    virtual std::shared_ptr<IOperator> Operator() { return _Operator; }
    virtual std::vector<std::shared_ptr<IEffect>>& Effects() { return _Effects; }
    virtual DecompositionStatus OnIsValidFailed(IContext&) override { return DecompositionStatus::Failed; }

    // If these functions took non-reference parameters, they could be construction inline in the function call.
    // However, we probably want to share implementations of conditions across multiple domains, so avoiding the
    // shared_ptr copy costs per function call seems reasonable.
    bool AddExecutingCondition(std::shared_ptr<ICondition>& condition) 
    {
        _ExecutingConditions.push_back(condition);
        return true;
    }
    bool AddEffect(std::shared_ptr<IEffect>& effect) 
    {
        _Effects.push_back(effect);
        return true;
    }
    bool SetOperator(std::shared_ptr<IOperator>& action)  
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
    virtual void Log(IContext& ctx, std::string description, ConsoleColor color = ConsoleColor::White);
};
} // namespace FluidHTN
