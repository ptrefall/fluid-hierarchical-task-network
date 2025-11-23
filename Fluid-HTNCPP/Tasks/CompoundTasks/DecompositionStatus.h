
#pragma once

namespace FluidHTN
{

enum class DecompositionStatus
{
    Succeeded,
    Partial,
    Failed,
    Rejected
};

inline StringType DecompositionStatusToString(DecompositionStatus st)
{
    switch (st)
    {
        case DecompositionStatus::Failed:
            return "DecompositionStatus::Failed"s;
        case DecompositionStatus::Partial:
            return "DecompositionStatus::Partial"s;
        case DecompositionStatus::Rejected:
            return "DecompositionStatus::Rejected"s;
        case DecompositionStatus::Succeeded:
            return "DecompositionStatus::Succeded"s;
        default:
            return "ThisSatisifesCompilerUselessWarnings"s;
    }
}

} // namespace FluidHTN
