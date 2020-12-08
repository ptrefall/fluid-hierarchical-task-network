#include "pch.h"

#include "Contexts/Context.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/CompoundTask.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/OtherTasks/Slot.h"
#include "Domain.h"

namespace FluidHTN
{

Domain::Domain(const std::string& name)
{
    _Root = std::make_shared<TaskRoot>();
    _Root->Name() = name;
}

bool Domain::Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<ITask>& subtask)
{
    FHTN_FATAL_EXCEPTION(subtask != parent,"parent and subtask cannot be the same");
    parent->AddSubTask(subtask);
    subtask->Parent() = parent;
    return true;
}

bool Domain::Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<Slot>& slot)
{
    FHTN_FATAL_EXCEPTION(std::static_pointer_cast<ITask>(parent) != std::static_pointer_cast<ITask>(slot),
                         "Parent and slot cannot be the same");

    if (_slots.find(slot->SlotId()) != _slots.end())
    {
        throw std::invalid_argument("slot already exists in domain definition");
    }
    parent->AddSubTask(std::static_pointer_cast<ITask>(slot));
    slot->Parent() = parent;
    _slots.insert(std::make_pair(slot->SlotId(), slot));
    return true;
}

bool Domain::Add(std::shared_ptr<TaskRoot>& root, std::shared_ptr<CompoundTask>& subtask)
{
    auto compound = std::static_pointer_cast<CompoundTask>(root);
    return Add(compound, subtask);
}

bool Domain::Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<CompoundTask>& subtask)
{
    auto s = std::static_pointer_cast<ITask>(subtask);
    return Add(parent, s);
}
bool Domain::Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<PrimitiveTask>& pt)
{
    auto s = std::static_pointer_cast<ITask>(pt);
    return Add(parent, s);
}

bool Domain::Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<TaskRoot>& root)
{
    auto s = std::static_pointer_cast<CompoundTask>(root);
    return Add(parent, s);
}

DecompositionStatus Domain::FindPlan(IContext& ctx, TaskQueueType& plan)
{
    FHTN_FATAL_EXCEPTION(ctx.IsInitialized(),"Context was uninitialized" );

    ctx.SetContextState(ContextState::Planning);

    TaskQueueType().swap(plan);

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

            auto compoundTask = std::static_pointer_cast<CompoundTask>(pair.Task);
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
            if(ctx.DebugMTR())
            {
                ctx.MTRDebug().clear();
            }

            status = _Root->Decompose(ctx, 0, plan);
        }
    }
    else
    {
        std::queue<PartialPlanEntry> lastPartialPlanQueue;
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
                std::queue<PartialPlanEntry>().swap(ctx.PartialPlanQueue());
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
                    ctx.GetWorldState().SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(i), stack.top().second);
                    stack = WorldStateStackType();
                }
            }
        }
        else
        {
            // Clear away any changes that might have been applied to the stack
            // No changes should be made or tracked further when the plan failed.
            for(size_t i = 0; i < ctx.GetWorldStateChangeStack().size(); i++)
            {
                auto& stack = ctx.GetWorldStateChangeStack()[i];
                if (stack.size() != 0)
                {
                    stack = WorldStateStackType();
                }
            }
        }
        ctx.SetContextState(ContextState::Executing);
    }

    return status;
}

bool Domain::TrySetSlotDomain(int slotId, Domain& subDomain)
{
    auto slot = _slots.find(slotId);
    if(slot != _slots.end())
        {
        return slot->second->Set(subDomain.Root());
    }
    return false;
}
void Domain::ClearSlot(int slotId)
{
    auto iter = _slots.find(slotId);
    if(iter != _slots.end())
    {
        iter->second->Clear();
    }
}

} // namespace FluidHTN