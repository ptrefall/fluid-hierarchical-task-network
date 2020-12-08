#pragma once

namespace FluidHTN
{
// These should be template parameters to IContext, but that propagates templates everywhere and I hate it.
// Just define your own types here
//
#if !USE_CUSTOM_WORDSTATE_PROPERTIES
typedef int WORLDSTATEPROPERTY_ID_TYPE ;
typedef uint8_t WORLDSTATEPROPERTY_VALUE_TYPE;
#endif

class IWorldState
{
public:
    virtual bool HasState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE value) = 0;
    virtual WORLDSTATEPROPERTY_VALUE_TYPE& GetState(WORLDSTATEPROPERTY_ID_TYPE state) = 0;
    virtual void                           SetState(WORLDSTATEPROPERTY_ID_TYPE state, WORLDSTATEPROPERTY_VALUE_TYPE value) = 0;

    virtual int GetMaxPropertyCount() = 0;
};
} // namespace FluidHTN
