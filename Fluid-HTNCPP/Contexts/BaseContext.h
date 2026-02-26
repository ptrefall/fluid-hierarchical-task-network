#pragma once
#include "Contexts/Context.h"
#include "DebugInterfaces/DecompositionLogEntry.h"
#include "Conditions/Condition.h"
#include "Effects/Effect.h"
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{

template <typename WSIDTYPE, typename WSVALTYPE, typename WSDERIVEDTYPE>
class BaseContext : public IContext
{
    void throw_if_not_intialized() { FHTN_FATAL_EXCEPTION(_IsInitialized, "Context is not initialized"); }

protected:
    bool                              _IsInitialized = false;
    bool                              _IsDirty = false;
    ContextState                      _ContextState = ContextState::Executing;
    int                               _CurrentDecompositionDepth = 0;
    bool                              _DebugMTR = false;
    Queue<IBaseDecompositionLogEntry> _DecompositionLog;
    bool                              _LogDecomposition = false;
    bool                              _RealTimeLog = false;
    ArrayType<int>                    _MethodTraversalRecord;
    ArrayType<StringType>             _MTRDebug;

    ArrayType<int>        _LastMTR;
    ArrayType<StringType> _LastMTRDebug;

    PartialPlanQueueType                                       _PartialPlanQueue;
    bool                                                       _HasPausedPartialPlan = false;
    SharedPtr<IWorldState<WSIDTYPE, WSVALTYPE, WSDERIVEDTYPE>> _WorldState;

    // An array of stacks per property of the world state.
    typedef Stack<Pair<EffectType, WSVALTYPE>> WorldStateStackType;
    typedef ArrayType<WorldStateStackType>     WorldStateStackArrayType;

    WorldStateStackArrayType _WorldStateChangeStackArray;

public:
    virtual bool                               IsInitialized() const override final { return _IsInitialized; }
    virtual bool&                              IsDirty() override final { return _IsDirty; }
    virtual ContextState                       GetContextState() const override final { return _ContextState; }
    virtual void                               SetContextState(ContextState s) override final { _ContextState = s; }
    virtual int&                               CurrentDecompositionDepth() override final { return _CurrentDecompositionDepth; }
    virtual ArrayType<int>&                    MethodTraversalRecord() override final { return _MethodTraversalRecord; }
    virtual ArrayType<StringType>&             MTRDebug() override final { return _MTRDebug; }
    virtual ArrayType<int>&                    LastMTR() override final { return _LastMTR; }
    virtual ArrayType<StringType>&             LastMTRDebug() override final { return _LastMTRDebug; }
    virtual bool&                              DebugMTR() override final { return _DebugMTR; }
    virtual Queue<IBaseDecompositionLogEntry>& DecompositionLog() override final { return _DecompositionLog; }
    virtual bool                               LogDecomposition() override final { return _LogDecomposition; }
    virtual void                               SetLogDecomposition(bool decomp) override final { _LogDecomposition = decomp; }
    virtual void                               SetRealTimeLog(bool dolog) final {_RealTimeLog = dolog; }
    virtual PartialPlanQueueType&              PartialPlanQueue() override final { return _PartialPlanQueue; }
    virtual void                               PartialPlanQueue(PartialPlanQueueType p) override final { _PartialPlanQueue = p; }
    virtual void                               ClearPartialPlanQueue() override final { _PartialPlanQueue = PartialPlanQueueType(); }
    virtual bool&                              HasPausedPartialPlan() override final { return _HasPausedPartialPlan; }

    IWorldState<WSIDTYPE, WSVALTYPE, WSDERIVEDTYPE>& GetWorldState() { return *_WorldState; }
    /// <summary>
    ///     A stack of changes applied to each world state entry during planning.
    ///     This is necessary if one wants to support planner-only and plan&execute effects.
    /// </summary>
    WorldStateStackArrayType& GetWorldStateChangeStack() { return _WorldStateChangeStackArray; }

    virtual void Init() override
    {
        if (_WorldState != nullptr)
        {
            for(int i =0 ; i < _WorldState->GetMaxPropertyCount(); i++)
            {
                _WorldStateChangeStackArray.Add(WorldStateStackType());
            }
        }
        _IsInitialized = true;
    }

    virtual bool       HasState(WSIDTYPE state, WSVALTYPE value) { return (GetState(state) == value); }
    virtual WSVALTYPE  GetState(WSIDTYPE state) 
    {
        if (_ContextState == ContextState::Executing)
        {
            return _WorldState->GetState(state);
        }
        if (_WorldStateChangeStackArray[(int)state].size() == 0)
        {
            return _WorldState->GetState(state);
        }
        return _WorldStateChangeStackArray[(int)state].top().Second();
    }
    virtual void SetState(WSIDTYPE   state,
                          WSVALTYPE  value,
                          bool       setAsDirty /* = true */,
                          EffectType e /* = EffectType::Permanent */) 
    {
        if (_ContextState == ContextState::Executing)
        {
            // Prevent setting the world state dirty if we're not changing anything.
            if (_WorldState->GetState(state) == value)
            {
                return;
            }
            _WorldState->SetState(state, value);
            if (setAsDirty)
            {
                _IsDirty = true;
            }
        }
        else
        {
            Pair p(e, value);
            _WorldStateChangeStackArray[(int)state].push(p);
        }
    }
    virtual ArrayType<int> GetWorldStateChangeDepth() override
    {
        throw_if_not_intialized();
        ArrayType<int> stackDepth(_WorldStateChangeStackArray.size());
        for (size_t i = 0; i < _WorldStateChangeStackArray.size(); i++)
        {
            //stackDepth.Add((int)_WorldStateChangeStackArray[i].size());
            // Nodifyed by kaminaritukane@163.com, to pass the DomainTests
            stackDepth[i] = (int)_WorldStateChangeStackArray[i].size();
        }
        return stackDepth;
    }
    virtual void TrimForExecution() override
    {
        FHTN_FATAL_EXCEPTION(_ContextState != ContextState::Executing, "Can not trim a context when in execution mode");

        for (size_t si = 0; si < _WorldStateChangeStackArray.size();si++)
        {
            auto& stack =  _WorldStateChangeStackArray[si];
            while (stack.size() != 0 && stack.top().First() != EffectType::Permanent)
            {
                stack.pop();
            }
        }
    }

    virtual void TrimToStackDepth(ArrayType<int>& stackDepth) override
    {
        FHTN_FATAL_EXCEPTION(_ContextState != ContextState::Executing, "Can not trim a context when in execution mode");

        for (size_t i = 0; i < stackDepth.size(); i++)
        {
            auto& stack = _WorldStateChangeStackArray[i];
            while (stack.size() > stackDepth[i])
            {
                stack.pop();
            }
        }
    }
    virtual void Reset() override
    {
        _MethodTraversalRecord.clear();
        _LastMTR.clear();

        _IsInitialized = false;
    }
    // ========================================================= DECOMPOSITION LOGGING
    virtual void RealTimeLog(StringType Name, StringType description) override{}
    void Log(StringType name, StringType description, int depth, SharedPtr<ITask> task, ConsoleColor color = ConsoleColor::White)
    {
        if(_RealTimeLog)
        {
            RealTimeLog(name,description);
        }
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedCompoundTaskEntry{
            {name, description, depth, color},
            StaticCastPtr<CompoundTask>(task),
        });
    }
    void Log(StringType            name,
             StringType            description,
             int                   depth,
             SharedPtr<ICondition> condition,
             ConsoleColor          color = ConsoleColor::DarkGreen)
    {
        if(_RealTimeLog)
        {
            RealTimeLog(name,description);
        }
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedConditionEntry{{name, description, depth, color}, condition});
    }
    void Log(StringType         name,
             StringType         description,
             int                depth,
             SharedPtr<IEffect> effect,
             ConsoleColor       color = ConsoleColor::DarkYellow)
    {
        if(_RealTimeLog)
        {
            RealTimeLog(name,description);
        }
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedEffectEntry{
            {name, description, depth, color},
            effect,
        });
    }
};

} // namespace FluidHTN
