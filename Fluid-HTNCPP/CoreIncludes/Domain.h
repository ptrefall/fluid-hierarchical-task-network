#pragma once
#include "Tasks/Task.h"
#include "Tasks/CompoundTasks/DecompositionStatus.h"
#include "Contexts/Context.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/CompoundTask.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/OtherTasks/Slot.h"
#include "Domain.h"

namespace FluidHTN
{

template <typename WSIDTYPE, typename WSVALTYPE, typename WSDERIVEDTYPE>
class BaseContext;

//=====================================================================================
// Base class
class Domain
{
protected:
    SharedPtr<TaskRoot>       _Root;
    Map<int, SharedPtr<Slot>> _slots;

public:
    Domain(const StringType& name)
    {
        _Root = MakeSharedPtr<TaskRoot>();
        _Root->Name() = name;
    }
    virtual ~Domain() {}

    virtual SharedPtr<TaskRoot>& Root() { return _Root; }

    bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<Slot>& slot)
    {
        FHTN_FATAL_EXCEPTION(StaticCastPtr<ITask>(parent) != StaticCastPtr<ITask>(slot), "Parent and slot cannot be the same");

        FHTN_FATAL_EXCEPTION(_slots.Find(slot->SlotId()) == _slots.End(), "slot already exists in domain definition");
        parent->AddSubTask(StaticCastPtr<ITask>(slot));
        slot->Parent() = parent;
        _slots.Insert(MakePair(slot->SlotId(), slot));
        return true;
    }

    bool Add(SharedPtr<TaskRoot>& root, SharedPtr<CompoundTask>& subtask)
    {
        auto compound = StaticCastPtr<CompoundTask>(root);
        return Add(compound, subtask);
    }

    bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<CompoundTask>& subtask)
    {
        auto s = StaticCastPtr<ITask>(subtask);
        return Add(parent, s);
    }
    bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<PrimitiveTask>& pt)
    {
        auto s = StaticCastPtr<ITask>(pt);
        return Add(parent, s);
    }

    bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<TaskRoot>& root)
    {
        auto s = StaticCastPtr<CompoundTask>(root);
        return Add(parent, s);
    }

    bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<ITask>& subtask)
    {
        FHTN_FATAL_EXCEPTION(subtask != parent, "parent and subtask cannot be the same");
        parent->AddSubTask(subtask);
        subtask->Parent() = parent;
        return true;
    }


    // ========================================================= SLOTS

    /// <summary>
    ///     At runtime, set a sub-domain to the slot with the given id.
    ///     This can be used with Smart Objects, to extend the behavior
    ///     of an agent at runtime.
    /// </summary>
    bool TrySetSlotDomain(int slotId, Domain& subDomain)
    {
        auto slot = _slots.Find(slotId);
        if (slot != _slots.End())
        {
            return slot->second->Set(subDomain.Root());
        }
        return false;
    }

    /// <summary>
    ///     At runtime, clear the sub-domain from the slot with the given id.
    ///     This can be used with Smart Objects, to extend the behavior
    ///     of an agent at runtime.
    /// </summary>
    void ClearSlot(int slotId)
    {
        auto iter = _slots.Find(slotId);
        if (iter != _slots.End())
        {
            iter->second->clear();
        }
    }
    template <typename WSIDTYPE, typename WSVALTYPE, typename WSDERIVEDTYPE>
    DecompositionStatus FindPlan(BaseContext<WSIDTYPE, WSVALTYPE, WSDERIVEDTYPE>& ctx, TaskQueueType& plan)
    {
        FHTN_FATAL_EXCEPTION(ctx.IsInitialized(), "Context was uninitialized");

        ctx.SetContextState(ContextState::Planning);

        plan.clear();

        auto status = DecompositionStatus::Rejected;

        // We first check whether we have a stored start task. This is true
        // if we had a partial plan pause somewhere in our plan, and we now
        // want to continue where we left off.
        // If this is the case, we don't erase the MTR, but continue building it.
        // However, if we have a partial plan, but LastMTR is not 0, that means
        // that the partial plan is still running, but something triggered a replan.
        // When this happens, we have to plan from the domain root (we're not
        // continuing the current plan), so that we're open for other plans to replace
        // the running partial plan.
        if (ctx.HasPausedPartialPlan() && ctx.LastMTR().size() == 0)
        {
            ctx.HasPausedPartialPlan() = false;
            while (ctx.PartialPlanQueue().size() > 0)
            {
                auto& pair = ctx.PartialPlanQueue().front();
                ctx.PartialPlanQueue().pop();

                FHTN_FATAL_EXCEPTION(pair.Task->IsTypeOf(ITaskDerivedClassName::CompoundTask),
                                     "PartialPlanEntry task must be a compound task");

                auto compoundTask = StaticCastPtr<CompoundTask>(pair.Task);
                if (plan.size() == 0)
                {
                    status = compoundTask->Decompose(ctx, pair.TaskIndex, plan);
                }
                else
                {
                    TaskQueueType p;
                    status = compoundTask->Decompose(ctx, pair.TaskIndex, p);
                    if (status == DecompositionStatus::Succeeded || status == DecompositionStatus::Partial)
                    {
                        while (p.size() > 0)
                        {
                            plan.push(p.front());
                            p.pop();
                        }
                    }
                }
                // While continuing a partial plan, we might encounter
                // a new pause.
                if (ctx.HasPausedPartialPlan())
                {
                    break;
                }
            }
            // If we failed to continue the paused partial plan,
            // then we have to start planning from the root.
            if (status == DecompositionStatus::Rejected || status == DecompositionStatus::Failed)
            {
                ctx.MethodTraversalRecord().clear();
                if (ctx.DebugMTR())
                {
                    ctx.MTRDebug().clear();
                }

                status = _Root->Decompose(ctx, 0, plan);
            }
        }
        else
        {
            Queue<PartialPlanEntry> lastPartialPlanQueue;
            if (ctx.HasPausedPartialPlan())
            {
                ctx.HasPausedPartialPlan() = false;
                while (ctx.PartialPlanQueue().size() > 0)
                {
                    lastPartialPlanQueue.push(ctx.PartialPlanQueue().front());
                    ctx.PartialPlanQueue().pop();
                }
            }
            // We only erase the MTR if we start from the root task of the domain.
            ctx.MethodTraversalRecord().clear();
            if (ctx.DebugMTR())
            {
                ctx.MTRDebug().clear();
            }

            status = _Root->Decompose(ctx, 0, plan);

            // If we failed to find a new plan, we have to restore the old plan,
            // if it was a partial plan.
            if (lastPartialPlanQueue.empty() != true)
            {
                if (status == DecompositionStatus::Rejected || status == DecompositionStatus::Failed)
                {
                    ctx.HasPausedPartialPlan() = true;
                    ctx.PartialPlanQueue().clear();
                    while (lastPartialPlanQueue.size() > 0)
                    {
                        ctx.PartialPlanQueue().push(lastPartialPlanQueue.front());
                        lastPartialPlanQueue.pop();
                    }
                }
            }
            // If this MTR equals the last MTR, then we need to double check whether we ended up
            // just finding the exact same plan. During decomposition each compound task can't check
            // for equality, only for less than, so this case needs to be treated after the fact.
            auto isMTRsEqual = (ctx.MethodTraversalRecord().size() == ctx.LastMTR().size());
            if (isMTRsEqual)
            {
                for (auto i = 0; i < ctx.MethodTraversalRecord().size(); i++)
                    if (ctx.MethodTraversalRecord()[i] < ctx.LastMTR()[i])
                    {
                        isMTRsEqual = false;
                        break;
                    }

                if (isMTRsEqual)
                {
                    plan = TaskQueueType();
                    status = DecompositionStatus::Rejected;
                }
            }
            if (status == DecompositionStatus::Succeeded || status == DecompositionStatus::Partial)
            {
                // Trim away any plan-only or plan&execute effects from the world state change stack, that only
                // permanent effects on the world state remains now that the planning is done.
                ctx.TrimForExecution();

                // Apply permanent world state changes to the actual world state used during plan execution.
                for (size_t i = 0; i < ctx.GetWorldStateChangeStack().size(); i++)
                {
                    auto& stack = ctx.GetWorldStateChangeStack()[i];
                    if (stack.size() != 0)
                    {
                        ctx.GetWorldState().SetState(static_cast<WSIDTYPE>(i), stack.top().Second());
                        stack.clear();
                    }
                }
            }
            else
            {
                // Clear away any changes that might have been applied to the stack
                // No changes should be made or tracked further when the plan failed.
                for (size_t i = 0; i < ctx.GetWorldStateChangeStack().size(); i++)
                {
                    auto& stack = ctx.GetWorldStateChangeStack()[i];
                    if (stack.size() != 0)
                    {
                        stack.clear();
                    }
                }
            }
            ctx.SetContextState(ContextState::Executing);
        }

        return status;
    }
};
//=====================================================================================

} // namespace FluidHTN
