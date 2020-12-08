#pragma once
#include "Contexts/BaseContext.h"

using namespace FluidHTN;

enum class DomainTestState
{
    HasA,
    HasB,
    HasC
};
class DomainTestWorldState : public IWorldState
{
    uint8_t MyState[3];

public:
    bool HasState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE value) override
    {
        return (MyState[state] == value);
    }

    WORLDSTATEPROPERTY_VALUE_TYPE& GetState(WORLDSTATEPROPERTY_ID_TYPE state) override { return MyState[state]; }

    void SetState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE value) override { MyState[state] = value; }

    int GetMaxPropertyCount() { return 3; }
};
class DomainTestContext : public BaseContext
{
    bool _done = false;

public:
    DomainTestContext() { _WorldState = std::make_shared<DomainTestWorldState>(); }

    bool& Done() { return _done; }

    bool HasStateOneParam(WORLDSTATEPROPERTY_ID_TYPE state)
    {
        WORLDSTATEPROPERTY_VALUE_TYPE one = 1;
        return BaseContext::HasState(state, one);
    }
    bool HasStateOneParam(DomainTestState state) { return HasStateOneParam(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(state)); }

    void SetStateDTS(DomainTestState state, int value)
    {
        _WorldState->SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(state), static_cast<WORLDSTATEPROPERTY_VALUE_TYPE>(value));
    }
    void SetStateDTS(DomainTestState state, int value, bool dirty, EffectType eff)
    {
        BaseContext::SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(state), static_cast<WORLDSTATEPROPERTY_VALUE_TYPE>(value),dirty,eff);
    }

    WORLDSTATEPROPERTY_VALUE_TYPE GetStateDTS(DomainTestState state) { return BaseContext::GetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(state)); }

};
class MyDebugContext : public DomainTestContext
{
public:
    MyDebugContext()
    {
        _DebugMTR = true;
        _LogDecomposition = true;
    }
};
