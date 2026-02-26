#pragma once

namespace FluidHTN
{

template <typename IDTYPE, typename VALUETYPE, typename DerivedType>
class IWorldState
{
    static_assert(std::is_enum<IDTYPE>::value, "WorldState Id must be an enum type");

public:
    typedef IDTYPE IdType;
    typedef VALUETYPE ValueType;

    bool HasState(IdType state, ValueType value)
    {
        return static_cast<DerivedType*>(this)->HasState(state, value);
    }
    ValueType GetState(IdType state)
    {
        return static_cast<DerivedType*>(this)->GetState(state);
    }
    void SetState(IdType state, ValueType value)
    {
        return static_cast<DerivedType*>(this)->SetState(state, value);
    }
    int GetMaxPropertyCount() { return static_cast<DerivedType*>(this)->GetMaxPropertyCount(); }
};
} // namespace FluidHTN
