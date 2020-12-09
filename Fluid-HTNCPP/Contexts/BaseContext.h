#pragma once
#include "Contexts/Context.h"
#include "DebugInterfaces/DecompositionLogEntry.h"
#include "Conditions/Condition.h"
#include "Effects/Effect.h"
#include "Tasks/CompoundTasks/CompoundTask.h"

namespace FluidHTN
{
class BaseContext : public IContext
{
    void throw_if_not_intialized() { FHTN_FATAL_EXCEPTION(_IsInitialized, "Context is not initialized"); }

public:
    virtual void Init()
    {
        if (_WorldState != nullptr)
        {
            _WorldStateChangeStackArray.resize(_WorldState->GetMaxPropertyCount());
        }
        _IsInitialized = true;
    }

    virtual bool HasState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE& value) override
    {
        return (GetState(state) == value);
    }
    virtual WORLDSTATEPROPERTY_VALUE_TYPE& GetState(WORLDSTATEPROPERTY_ID_TYPE state) override
    {
        if (_ContextState == ContextState::Executing)
        {
            return _WorldState->GetState(state);
        }
        if (_WorldStateChangeStackArray[state].size() == 0)
        {
            return _WorldState->GetState(state);
        }
        return _WorldStateChangeStackArray[state].top().Second();
    }
    virtual void SetState(WORLDSTATEPROPERTY_ID_TYPE    state,
                          WORLDSTATEPROPERTY_VALUE_TYPE value,
                          bool                          setAsDirty /* = true */,
                          EffectType                    e /* = EffectType::Permanent */) override
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
            _WorldStateChangeStackArray[state].push(p);
        }
    }
    virtual ArrayType<int> GetWorldStateChangeDepth() override
    {
        throw_if_not_intialized();
        ArrayType<int> stackDepth(_WorldStateChangeStackArray.size());
        for (size_t i = 0; i < _WorldStateChangeStackArray.size(); i++)
        {
            stackDepth[i] = (int)_WorldStateChangeStackArray[i].size();
        }
        return stackDepth;
    }
    virtual void TrimForExecution() override
    {
        FHTN_FATAL_EXCEPTION(_ContextState != ContextState::Executing, "Can not trim a context when in execution mode");

        for (auto& stack : _WorldStateChangeStackArray)
        {
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
    void Log(
        StringType name, StringType description, int depth, SharedPtr<ITask> task, ConsoleColor color = ConsoleColor::White)
    {
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedCompoundTaskEntry{
            {name, description, depth, color},
            StaticCastPtr<CompoundTask>(task),
        });
    }
    void Log(StringType                 name,
             StringType                 description,
             int                         depth,
             SharedPtr<ICondition> condition,
             ConsoleColor                color = ConsoleColor::DarkGreen)
    {
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedConditionEntry{{name, description, depth, color}, condition});
    }
    void Log(StringType              name,
             StringType              description,
             int                      depth,
             SharedPtr<IEffect> effect,
             ConsoleColor             color = ConsoleColor::DarkYellow)
    {
        if (_LogDecomposition == false)
            return;

        _DecompositionLog.push(DecomposedEffectEntry{
            {name, description, depth, color},
            effect,
        });
    }
};

} // namespace FluidHTN
