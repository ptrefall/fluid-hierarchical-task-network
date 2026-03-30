#pragma once
#include "Effects/EffectType.h"
#include "CoreIncludes/WorldState.h"
#include "DebugInterfaces/DecompositionLogEntry.h"

namespace FluidHTN
{

enum class ContextState
{
    Planning,
    Executing
};

struct PartialPlanEntry
{
    SharedPtr<class ITask> Task;
    int                    TaskIndex;
};

typedef Queue<PartialPlanEntry> PartialPlanQueueType;
class IContext
{
public:
    virtual ~IContext(){}
    virtual void  Init() = 0;
    virtual bool  IsInitialized() const = 0;
    virtual bool& IsDirty() = 0;

    virtual ContextState GetContextState() const = 0;
    virtual void         SetContextState(ContextState s) = 0;

    virtual int& CurrentDecompositionDepth() = 0;

    /// <summary>
    ///     The Method Traversal Record is used while decomposing a domain and
    ///     records the valid decomposition indices as we go through our
    ///     decomposition process.
    ///     It "should" be enough to only record decomposition traversal in Selectors.
    ///     This can be used to compare LastMTR with the MTR, and reject
    ///     a new plan early if it is of lower priority than the last plan.
    ///     It is the user's responsibility to set the instance of the MTR, so that
    ///     the user is free to use pooled instances, or whatever optimization they
    ///     see fit.
    /// </summary>
    virtual ArrayType<int>&        MethodTraversalRecord() = 0;
    virtual ArrayType<StringType>& MTRDebug() = 0;

    /// <summary>
    ///     The Method Traversal Record that was recorded for the currently
    ///     running plan.
    ///     If a plan completes successfully, this should be cleared.
    ///     It is the user's responsibility to set the instance of the MTR, so that
    ///     the user is free to use pooled instances, or whatever optimization they
    ///     see fit.
    /// </summary>
    virtual ArrayType<int>&        LastMTR() = 0;
    virtual ArrayType<StringType>& LastMTRDebug() = 0;

    /// <summary>
    /// Whether the planning system should collect debug information about our Method Traversal Record.
    /// </summary>
    virtual bool& DebugMTR() = 0;

    /// <summary>
    /// </summary>
    virtual Queue<IBaseDecompositionLogEntry>& DecompositionLog() = 0;
    /// <summary>
    /// Whether our planning system should log our decomposition. Specially condition success vs failure.
    /// </summary>
    virtual bool LogDecomposition() = 0;
    virtual void SetLogDecomposition(bool) = 0;

    virtual PartialPlanQueueType& PartialPlanQueue() = 0;
    virtual void                  PartialPlanQueue(PartialPlanQueueType p) = 0;
    virtual void                  ClearPartialPlanQueue() = 0;

    virtual bool& HasPausedPartialPlan() = 0;

    /// <summary>
    ///     Reset the context state to default values.
    /// </summary>
    virtual void Reset() = 0;

    virtual void TrimForExecution() = 0;
    virtual void TrimToStackDepth(ArrayType<int>& stackDepth) = 0;

    virtual ArrayType<int> GetWorldStateChangeDepth() = 0;

    virtual void RealTimeLog(StringType name, StringType description){}
    virtual void Log(StringType             name,
                     StringType             description,
                     int                    depth,
                     SharedPtr<class ITask> task,
                     ConsoleColor           color = ConsoleColor::White) = 0;
    virtual void Log(StringType                  name,
                     StringType                  description,
                     int                         depth,
                     SharedPtr<class ICondition> condition,
                     ConsoleColor                color = ConsoleColor::DarkGreen) = 0;
    virtual void Log(StringType               name,
                     StringType               description,
                     int                      depth,
                     SharedPtr<class IEffect> effect,
                     ConsoleColor             color = ConsoleColor::DarkYellow) = 0;
};
} // namespace FluidHTN
