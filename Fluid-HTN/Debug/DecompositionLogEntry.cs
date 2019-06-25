using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;

namespace FluidHTN.Debug
{
    public static class Debug
    {
        public static string DepthToString(int depth)
        {
            string s = "";
            for (var i = 0; i < depth; i++)
            {
                s += "\t";
            }

            s += "- ";
            return s;
        }
    }
    public interface IBaseDecompositionLogEntry
    {
        string Name { get; set; }
        string Description { get; set; }
        int Depth { get; set; }
        ConsoleColor Color { get; set; }
        string ToString();
    }

    public interface IDecompositionLogEntry<T> : IBaseDecompositionLogEntry
    {
        T Entry { get; set; }
    }

    public struct DecomposedCompoundTaskEntry : IDecompositionLogEntry<ITask>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public ITask Entry { get; set; }
    }

    public struct DecomposedConditionEntry : IDecompositionLogEntry<ICondition> {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public ICondition Entry { get; set; }
    }

    public struct DecomposedEffectEntry : IDecompositionLogEntry<IEffect> {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public IEffect Entry { get; set; }
    }
}
