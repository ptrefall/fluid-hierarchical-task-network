using System;
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

    public struct DecomposedCompoundTaskEntry<TWorldStateEntry> : IDecompositionLogEntry<ITask<TWorldStateEntry>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public ITask<TWorldStateEntry> Entry { get; set; }
    }

    public struct DecomposedConditionEntry<TWorldStateEntry> : IDecompositionLogEntry<ICondition<TWorldStateEntry>> {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public ICondition<TWorldStateEntry> Entry { get; set; }
    }

    public struct DecomposedEffectEntry<TWorldStateEntry> : IDecompositionLogEntry<IEffect<TWorldStateEntry>> {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public ConsoleColor Color { get; set; }
        public IEffect<TWorldStateEntry> Entry { get; set; }
    }
}
