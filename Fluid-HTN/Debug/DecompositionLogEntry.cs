using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN.Debug
{
    public interface IBaseDecompositionLogEntry
    {
        string Name { get; set; }
        string Description { get; set; }
        string ToString();
    }

    public interface IDecompositionLogEntry<T> : IBaseDecompositionLogEntry
    {
        T Entry { get; set; }
    }

    public struct DecomposedCompoundTaskEntry : IDecompositionLogEntry<DecomposedCompoundTask>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DecomposedCompoundTask Entry { get; set; }
    }

    public struct DecomposedConditionEntry : IDecompositionLogEntry<DecomposedCondition> {
        public string Name { get; set; }
        public string Description { get; set; }
        public DecomposedCondition Entry { get; set; }
    }

    public struct DecomposedEffectEntry : IDecompositionLogEntry<DecomposedEffect> {
        public string Name { get; set; }
        public string Description { get; set; }
        public DecomposedEffect Entry { get; set; }
    }

    public struct DecomposedCompoundTask
    {
        public string TaskType { get; set; }
        public DecompositionStatus Status { get; set; }
        public DecomposedPrimitiveTask[] Plan { get; set; }
    }

    public struct DecomposedPrimitiveTask
    {
        public string Name { get; set; }
        public string TaskType { get; set; }
    }

    public struct DecomposedCondition
    {
        public string ConditionType { get; set; }
        public bool Result { get; set; }
    }

    public struct DecomposedEffect
    {
        public string Name { get; set; }
        public string EffectType { get; set; }
        public byte Result { get; set; }
    }
}
