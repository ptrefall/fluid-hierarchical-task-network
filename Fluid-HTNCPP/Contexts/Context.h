#pragma once
#include "Effects/EffectType.h"
#include "WorldState.h"
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
    int                          TaskIndex;
};

static_assert(std::is_integral<WORLDSTATEPROPERTY_ID_TYPE>::value,
              "WorldState Id must be integral type. Change the vector type to hash table otherwise");
// An array of stacks per property of the world state.
typedef Stack<Pair<EffectType, WORLDSTATEPROPERTY_VALUE_TYPE>> WorldStateStackType;
typedef ArrayType<WorldStateStackType>                                 WorldStateStackArrayType;

typedef Queue<PartialPlanEntry> PartialPlanQueueType;
class IContext
{
protected:
    bool                                   _IsInitialized = false;
    bool                                   _IsDirty = false;
    ContextState                           _ContextState = ContextState::Executing;
    int                                    _CurrentDecompositionDepth = 0;
    bool                                   _DebugMTR = false;
    Queue<IBaseDecompositionLogEntry> _DecompositionLog;
    bool                                   _LogDecomposition = false;
    ArrayType<int>                       _MethodTraversalRecord;
    ArrayType<StringType>               _MTRDebug;

    ArrayType<int>         _LastMTR;
    ArrayType<StringType> _LastMTRDebug;

    PartialPlanQueueType               _PartialPlanQueue;
    bool                               _HasPausedPartialPlan = false;
    SharedPtr<class IWorldState> _WorldState;

    WorldStateStackArrayType _WorldStateChangeStackArray;

public:
    bool  IsInitialized() const { return _IsInitialized; }
    bool& IsDirty() { return _IsDirty; }

    ContextState GetContextState() const { return _ContextState; }
    void         SetContextState(ContextState s) { _ContextState = s; }

    int& CurrentDecompositionDepth() { return _CurrentDecompositionDepth; }

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
    ArrayType<int>&         MethodTraversalRecord() { return _MethodTraversalRecord; }
    ArrayType<StringType>& MTRDebug() { return _MTRDebug; }

    /// <summary>
    ///     The Method Traversal Record that was recorded for the currently
    ///     running plan.
    ///     If a plan completes successfully, this should be cleared.
    ///     It is the user's responsibility to set the instance of the MTR, so that
    ///     the user is free to use pooled instances, or whatever optimization they
    ///     see fit.
    /// </summary>
    virtual ArrayType<int>& LastMTR() { return _LastMTR; }
    virtual ArrayType<StringType>& LastMTRDebug() { return _LastMTRDebug; }

    /// <summary>
    /// Whether the planning system should collect debug information about our Method Traversal Record.
    /// </summary>
    bool& DebugMTR() { return _DebugMTR; }

    /// <summary>
    /// </summary>
    Queue<IBaseDecompositionLogEntry>& DecompositionLog() { return _DecompositionLog; }
    /// <summary>
    /// Whether our planning system should log our decomposition. Specially condition success vs failure.
    /// </summary>
    bool LogDecomposition() { return _LogDecomposition; }

    PartialPlanQueueType& PartialPlanQueue() { return _PartialPlanQueue; }
    void                  PartialPlanQueue(PartialPlanQueueType p) { _PartialPlanQueue = p; }
    void                  ClearPartialPlanQueue() { _PartialPlanQueue = PartialPlanQueueType(); }

    bool& HasPausedPartialPlan() { return _HasPausedPartialPlan; }

    IWorldState& GetWorldState() { return *_WorldState; }
    /// <summary>
    ///     A stack of changes applied to each world state entry during planning.
    ///     This is necessary if one wants to support planner-only and plan&execute effects.
    /// </summary>
    WorldStateStackArrayType& GetWorldStateChangeStack() { return _WorldStateChangeStackArray; }

    /// <summary>
    ///     Reset the context state to default values.
    /// </summary>
    virtual void Reset() = 0;

    virtual void TrimForExecution() = 0;
    virtual void TrimToStackDepth(ArrayType<int>& stackDepth) = 0;

    virtual bool                           HasState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE& value) = 0;
    virtual WORLDSTATEPROPERTY_VALUE_TYPE& GetState(WORLDSTATEPROPERTY_ID_TYPE state) = 0;
    virtual void                           SetState(WORLDSTATEPROPERTY_ID_TYPE    state,
                                                    WORLDSTATEPROPERTY_VALUE_TYPE value,
                                                    bool                          setAsDirty = true,
                                                    EffectType                    e = EffectType::Permanent) = 0;

    virtual ArrayType<int> GetWorldStateChangeDepth() = 0;

    virtual void Log(StringType            name,
                     StringType            description,
                     int                    depth,
                     SharedPtr<class ITask> task,
                     ConsoleColor           color = ConsoleColor::White) = 0;
    virtual void Log(StringType                 name,
                     StringType                 description,
                     int                         depth,
                     SharedPtr<class ICondition> condition,
                     ConsoleColor                color = ConsoleColor::DarkGreen) = 0;
    virtual void Log(StringType              name,
                     StringType              description,
                     int                      depth,
                     SharedPtr<class IEffect> effect,
                     ConsoleColor             color = ConsoleColor::DarkYellow) = 0;
};
} // namespace FluidHTN
