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
    SharedPtr<Domain>             _domain;
    ArrayType<SharedPtr<ITask>> _pointers;
    bool                                _PointersValid = true;

public:
    BaseDomainBuilder(const StringType& domainName)
    {
        _domain = MakeSharedPtr<Domain>(domainName);
        _pointers.Add(_domain->Root());
    }
    const SharedPtr<ITask> Pointer()
    {
        FHTN_FATAL_EXCEPTION(_PointersValid, "Pointers are null");
        if (_pointers.size() == 0)
        {
            return nullptr;
        }
        return _pointers.Back();
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
    bool AddCompoundTask(StringType name, SharedPtr<CompoundTask> task)
    {
        task->Name() = name;

        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");

        auto baseTask = StaticCastPtr<ITask>(task);
        auto compoundTask = StaticCastPtr<CompoundTask>(Pointer());

        if (_domain->Add(compoundTask, baseTask))
        {
            _pointers.Add(task);
            return true;
        }

        return false;
    }
    template<typename T>
    bool AddCompoundTask(StringType name)
	{
        SharedPtr<CompoundTask> ptr = MakeSharedPtr<T>(name);
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
    bool AddPrimitiveTask(const StringType& name)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf( ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");

        auto parent = MakeSharedPtr<PrimitiveTask>();
        parent->Name() = name;

        auto baseTask = StaticCastPtr<ITask>(parent);
        auto compoundTask = StaticCastPtr<CompoundTask>(Pointer());

        if (_domain->Add(compoundTask, baseTask))
        {

            _pointers.Add(parent);
            return true;
        }
        return false;
    }
    bool AddPausePlanTask()
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::SequenceCompoundTask),
                             "Pointer is not a Sequence. Maybe you tried to Pause Plan a "
                             "Selector, or forget an End() after a Primitive Task Action was defined?");

        auto parent = MakeSharedPtr<PausePlanTask>();
        parent->Name() = "Pause Plan"s;
        auto baseTask = StaticCastPtr<ITask>(parent);
        auto compoundTask = StaticCastPtr<CompoundTask>(Pointer());
        return _domain->Add(compoundTask, baseTask);
    }
    bool AddSequence(const StringType& name)
    {
        return AddCompoundTask<Sequence>(name);
    }
    bool AddAction(const StringType& name) { return AddPrimitiveTask(name); }
    bool AddSelector(const StringType& name)
    {
        return AddCompoundTask<Selector>(name);
    }
    bool AddCondition(const StringType& name, FunctionConditionType func)
    {
        auto condition = MakeSharedPtr<FuncCondition>(name, func);
        auto base = StaticCastPtr<ICondition>(condition);
        return Pointer()->AddCondition(base);
    }
    bool AddExecutingCondition(const StringType& name, FunctionConditionType func)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add an Executing Condition, but the Pointer is not a Primitive Task!");
        auto condition = MakeSharedPtr<FuncCondition>(name, func);
        auto base = StaticCastPtr<ICondition>(condition);
        auto task = StaticCastPtr<PrimitiveTask>(Pointer());
        return task->AddExecutingCondition(base);
    }
    bool AddOperator(FuncOperatorType action, StopOperatorType stopAction= nullptr)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add Operator, but the Pointer is not a Primitive Task!");
        auto op = MakeSharedPtr<FuncOperator>(action, stopAction);
        auto base = StaticCastPtr<IOperator>(op);
        auto task = StaticCastPtr<PrimitiveTask>(Pointer());
        return task->SetOperator(base);
    }
    bool AddEffect(const StringType& name, EffectType effectType, ActionType action)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask),
                             "Tried to add an Effect, but the Pointer is not a Primitive Task!");
        auto effect = MakeSharedPtr<ActionEffect>(name, effectType, action);
        auto base = StaticCastPtr<IEffect>(effect);
        auto task = StaticCastPtr<PrimitiveTask>(Pointer());
        return task->AddEffect(base);
    }

    bool AddSlot(int slotId)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask), "Pointer() is not compound task");
        auto slot = MakeSharedPtr<Slot>();
        auto compoundTask = StaticCastPtr<CompoundTask>(Pointer());
        slot->SlotId(slotId);
        return _domain->Add(compoundTask, slot);
    }
    bool AddRandomSelector(const StringType& name) { return AddCompoundTask<RandomSelector>(name); }
    void End() { _pointers.PopBack(); }
    bool Splice(Domain& domain)
    {
        FHTN_FATAL_EXCEPTION(Pointer()->IsTypeOf(ITaskDerivedClassName::CompoundTask),
                             "Pointer is not a compound task type. Did you forget an End()?");

        auto compoundTask = StaticCastPtr<CompoundTask>(Pointer());
        _domain->Add(compoundTask, domain.Root());

        return true;
    }
    bool    PausePlan() { return AddPausePlanTask(); }
    SharedPtr<Domain> Build()
    {
        FHTN_FATAL_EXCEPTION(Pointer() == _domain->Root(), "Domain definition lacks one or more End() statements");
        _pointers.clear();
        _PointersValid = false; // C# code frees the pointers so that further access to Pointer() throws null reference
        return _domain;
    }
};

} // namespace FluidHTN
