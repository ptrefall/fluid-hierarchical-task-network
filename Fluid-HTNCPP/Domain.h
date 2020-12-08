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
    std::shared_ptr<TaskRoot>                      _Root;
    std::unordered_map<int, std::shared_ptr<Slot>> _slots;

public:
    Domain(const std::string& name);

    virtual std::shared_ptr<TaskRoot>& Root() { return _Root; }
    virtual bool Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<ITask>& subtask) ;
    virtual bool Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<PrimitiveTask>& pt) ;
    virtual bool Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<TaskRoot>& root) ;
    virtual bool Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<Slot>& slot) ;
    virtual bool Add(std::shared_ptr<TaskRoot>& root, std::shared_ptr<CompoundTask>& subtask) ;
    virtual bool Add(std::shared_ptr<CompoundTask>& parent, std::shared_ptr<CompoundTask>& subtask) ;

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
