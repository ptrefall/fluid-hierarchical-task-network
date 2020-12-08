#pragma once
#include "Tasks/Task.h"
#include "Tasks/CompoundTasks/DecompositionStatus.h"

namespace FluidHTN
{

class TaskRoot;
class CompoundTask;
class PrimitiveTask;
class ITask;
class Slot;

//=====================================================================================
// Base class
class IContext;
class Domain 
{
protected:
    SharedPtr<TaskRoot>                      _Root;
    std::unordered_map<int, SharedPtr<Slot>> _slots;

public:
    Domain(const StringType& name);

    virtual SharedPtr<TaskRoot>& Root() { return _Root; }
    virtual bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<ITask>& subtask) ;
    virtual bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<PrimitiveTask>& pt) ;
    virtual bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<TaskRoot>& root) ;
    virtual bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<Slot>& slot) ;
    virtual bool Add(SharedPtr<TaskRoot>& root, SharedPtr<CompoundTask>& subtask) ;
    virtual bool Add(SharedPtr<CompoundTask>& parent, SharedPtr<CompoundTask>& subtask) ;

    DecompositionStatus FindPlan(IContext& ctx, TaskQueueType& plan);

    // ========================================================= SLOTS

    /// <summary>
    ///     At runtime, set a sub-domain to the slot with the given id.
    ///     This can be used with Smart Objects, to extend the behavior
    ///     of an agent at runtime.
    /// </summary>
    bool TrySetSlotDomain(int slotId, Domain& subDomain);

    /// <summary>
    ///     At runtime, clear the sub-domain from the slot with the given id.
    ///     This can be used with Smart Objects, to extend the behavior
    ///     of an agent at runtime.
    /// </summary>
    void ClearSlot(int slotId);
};
//=====================================================================================

} // namespace FluidHTN
