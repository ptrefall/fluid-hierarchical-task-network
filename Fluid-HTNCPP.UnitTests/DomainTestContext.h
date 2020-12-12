#pragma once
#include "Contexts/BaseContext.h"

using namespace FluidHTN;

enum class DomainTestState
{
    HasA,
    HasB,
    HasC
};
class DomainTestWorldState : public IWorldState<DomainTestState,uint8_t, DomainTestWorldState>
{
    uint8_t MyState[3];

public:
    bool HasState(DomainTestState state, uint8_t value) 
    {
        return (MyState[(int)state] == value);
    }

    uint8_t& GetState(DomainTestState state) { return MyState[(int)state]; }

    void SetState(DomainTestState state, uint8_t value) { MyState[(int)state] = value; }

    int GetMaxPropertyCount() { return 3; }
};
class DomainTestContext : public BaseContext<DomainTestState,uint8_t,DomainTestWorldState>
{
    bool _done = false;

public:
    DomainTestContext() { _WorldState = MakeSharedPtr<DomainTestWorldState>(); }

    bool& Done() { return _done; }

    bool HasStateOneParam(DomainTestState state)
    {
        uint8_t one = 1;
        return BaseContext::HasState(state, one);
    }

    void SetStateDTS(DomainTestState state, int value)
    {
        _WorldState->SetState(static_cast<DomainTestState>(state), static_cast<uint8_t>(value));
    }
    void SetStateDTS(DomainTestState state, int value, bool dirty, EffectType eff)
    {
        BaseContext::SetState(static_cast<DomainTestState>(state), static_cast<uint8_t>(value),dirty,eff);
    }

    uint8_t GetStateDTS(DomainTestState state) { return BaseContext::GetState(static_cast<DomainTestState>(state)); }

};
typedef BaseContext<DomainTestState, uint8_t, DomainTestWorldState> BaseContextType;

class MyDebugContext : public DomainTestContext
{
public:
    MyDebugContext()
    {
        _DebugMTR = true;
        _LogDecomposition = true;
    }
};
