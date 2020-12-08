#pragma once
#include "Domain.h"
#include "Tasks/CompoundTasks/PausePlanTask.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/Sequence.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/CompoundTasks/RandomSelector.h"
#include "Tasks/OtherTasks/Slot.h"
#include "Conditions/Condition.h"
#include "Operators/Operator.h"
#include "Effects/Effect.h"

namespace FluidHTN
{

class BaseDomainBuilder
{
protected:
    std::shared_ptr<Domain>             _domain;
    std::vector<std::shared_ptr<ITask>> _pointers;
    bool                                _PointersValid = true;

public:
    BaseDomainBuilder(const std::string& domainName)
    {
        _domain = std::make_shared<Domain>(domainName);
        _pointers.push_back(_domain->Root());
    }
    const std::shared_ptr<ITask> Pointer()
    {
        if (!_PointersValid)
        {
            throw std::exception("Pointers are null");
        }
        if (_pointers.size() == 0)
        {
            return nullptr;
        }
        return _pointers.back();
    }
    // ========================================================= HIERARCHY HANDLING

    /// <summary>
    ///     Compound tasks are where HTN get their “hierarchical” nature. You can think of a compound task as
    ///     a high level task that has multiple ways of being accomplished. There are primarily two types of
    ///     compound tasks. Selectors and Sequencers. A Selector must be able to decompose a single sub-task,
    ///     while a Sequence must be able to decompose all its sub-tasks successfully for itself to have decomposed
    ///     successfully. There is nothing stopping you from extending this toolset with RandomSelect, UtilitySelect,
    ///     etc. These tasks are decomposed until we're left with only Primitive Tasks, which represent a final plan.
    ///     Compound tasks are comprised of a set of subtasks and a set of conditions.
    ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
    /// </summary>
    /// <typeparam name="P">The type of compound task</typeparam>
    /// <param name="name">The name given to the task, mainly for debug/display purposes</param>
    /// <returns></returns>
    bool AddCompoundTask(std::string name, std::shared_ptr<CompoundTask> task)
    {
        task->Name() = name;

        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");

        auto baseTask = std::static_pointer_cast<ITask>(task);
        auto compoundTask = std::static_pointer_cast<CompoundTask>(Pointer());

        if (_domain->Add(compoundTask, baseTask))
        {
            _pointers.push_back(task);
            return true;
        }

        return false;
    }
    template<typename T>
    bool AddCompoundTask(std::string name)
	{
        std::shared_ptr<CompoundTask> ptr = std::make_shared<T>(name);
        return AddCompoundTask(name, ptr);
    };
    /// <summary>
    ///     Primitive tasks represent a single step that can be performed by our AI. A set of primitive tasks is
    ///     the plan that we are ultimately getting out of the HTN. Primitive tasks are comprised of an operator,
    ///     a set of effects, a set of conditions and a set of executing conditions.
    ///     http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
    /// </summary>
    /// <typeparam name="P">The type of primitive task</typeparam>
    /// <param name="name">The name given to the task, mainly for debug/display purposes</param>
    /// <returns></returns>
    bool AddPrimitiveTask(const std::string& name)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf( ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");

        auto parent = std::make_shared<PrimitiveTask>();
        parent->Name() = name;

        auto baseTask = std::static_pointer_cast<ITask>(parent);
        auto compoundTask = std::static_pointer_cast<CompoundTask>(Pointer());

        if (_domain->Add(compoundTask, baseTask))
        {

            _pointers.push_back(parent);
            return true;
        }
        return false;
    }
    bool AddPausePlanTask()
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::SequenceCompoundTask),
                             "Pointer is not a Sequence. Maybe you tried to Pause Plan a "
                             "Selector, or forget an End() after a Primitive Task Action was defined?");

        auto parent = std::make_shared<PausePlanTask>();
        parent->Name() = "Pause Plan"s;
        auto baseTask = std::static_pointer_cast<ITask>(parent);
        auto compoundTask = std::static_pointer_cast<CompoundTask>(Pointer());
        return _domain->Add(compoundTask, baseTask);
    }
    bool AddSequence(const std::string& name)
    {
        return AddCompoundTask<Sequence>(name);
    }
    bool AddAction(const std::string& name) { return AddPrimitiveTask(name); }
    bool AddSelector(const std::string& name)
    {
        return AddCompoundTask<Selector>(name);
    }
    bool AddCondition(const std::string& name, FunctionConditionType func)
    {
        auto condition = std::make_shared<FuncCondition>(name, func);
        auto base = std::static_pointer_cast<ICondition>(condition);
        return Pointer()->AddCondition(base);
    }
    bool AddExecutingCondition(const std::string& name, FunctionConditionType func)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add an Executing Condition, but the Pointer is not a Primitive Task!");
        auto condition = std::make_shared<FuncCondition>(name, func);
        auto base = std::static_pointer_cast<ICondition>(condition);
        auto task = std::static_pointer_cast<PrimitiveTask>(Pointer());
        return task->AddExecutingCondition(base);
    }
    bool AddOperator(FuncOperatorType action, StopOperatorType stopAction= nullptr)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add Operator, but the Pointer is not a Primitive Task!");
        auto op = std::make_shared<FuncOperator>(action, stopAction);
        auto base = std::static_pointer_cast<IOperator>(op);
        auto task = std::static_pointer_cast<PrimitiveTask>(Pointer());
        return task->SetOperator(base);
    }
    bool AddEffect(const std::string& name, EffectType effectType, ActionType action)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add an Effect, but the Pointer is not a Primitive Task!");
        auto effect = std::make_shared<ActionEffect>(name, effectType, action);
        auto base = std::static_pointer_cast<IEffect>(effect);
        auto task = std::static_pointer_cast<PrimitiveTask>(Pointer());
        return task->AddEffect(base);
    }

    bool AddSlot(int slotId)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");
        auto slot = std::make_shared<Slot>();
        auto compoundTask = std::static_pointer_cast<CompoundTask>(Pointer());
        slot->SlotId(slotId);
        return _domain->Add(compoundTask, slot);
    }
    bool AddRandomSelector(const std::string& name) { return AddCompoundTask<RandomSelector>(name); }
    void End() { _pointers.pop_back(); }
    bool Splice(Domain& domain)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask),
                             "Pointer is not a compound task type. Did you forget an End()?");

        auto compoundTask = std::static_pointer_cast<CompoundTask>(Pointer());
        _domain->Add(compoundTask, domain.Root());

        return true;
    }
    bool    PausePlan() { return AddPausePlanTask(); }
    std::shared_ptr<Domain> Build()
    {
        FHTN_FATAL_EXCEPTION(Pointer() == _domain->Root(), "Domain definition lacks one or more End() statements");
        _pointers.clear();
        _PointersValid = false; // C# code frees the pointers so that further access to Pointer() throws null reference
        return _domain;
    }
};

} // namespace FluidHTN
