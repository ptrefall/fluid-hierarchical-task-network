using System;
using System.Collections.Generic;
using FluidHTN.Conditions;
using FluidHTN.Debug;
using FluidHTN.Factory;

namespace FluidHTN.Contexts
{
    public abstract class BaseContext<TWorldStateEntry> : IContext<TWorldStateEntry>
    {
        // ========================================================= PROPERTIES

        public bool IsInitialized { get; protected set; } = false;
        public bool IsDirty { get; set; }
        public ContextState ContextState { get; set; } = ContextState.Executing;
        public int CurrentDecompositionDepth { get; set; } = 0;
        public abstract IFactory Factory { get; set; }
        public List<int> MethodTraversalRecord { get; set; } = new List<int>();
        public List<int> LastMTR { get; } = new List<int>();
        public abstract List<string> MTRDebug { get; set; }
        public abstract List<string> LastMTRDebug { get; set; }
        public abstract bool DebugMTR { get; }
        public abstract Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; }
        public abstract bool LogDecomposition { get; }
        public Queue<PartialPlanEntry<TWorldStateEntry>> PartialPlanQueue { get; set; } = new Queue<PartialPlanEntry<TWorldStateEntry>>();
        public bool HasPausedPartialPlan { get; set; } = false;

        public abstract TWorldStateEntry[] WorldState { get; }

        public Stack<KeyValuePair<EffectType, TWorldStateEntry>>[] WorldStateChangeStack { get; protected set; }

        // ========================================================= INITIALIZATION

        public virtual void Init()
        {
            if (WorldStateChangeStack == null)
            {
                WorldStateChangeStack = new Stack<KeyValuePair<EffectType, TWorldStateEntry>>[WorldState.Length];
                for (var i = 0; i < WorldState.Length; i++)
                    WorldStateChangeStack[i] = new Stack<KeyValuePair<EffectType, TWorldStateEntry>> ();
            }

            if (DebugMTR)
            {
                if (MTRDebug == null) MTRDebug = new List<string>();
                if (LastMTRDebug == null) LastMTRDebug = new List<string>();
            }

            if (LogDecomposition)
            {
                if (DecompositionLog == null) DecompositionLog = new Queue<IBaseDecompositionLogEntry>();
            }

            IsInitialized = true;
        }

        // ========================================================= STATE HANDLING

        public bool HasState(int state, TWorldStateEntry value)
        {
            return GetState(state).Equals(value);
        }

        public TWorldStateEntry GetState(int state)
        {
            if (ContextState == ContextState.Executing) return WorldState[state];

            if (WorldStateChangeStack[state].Count == 0) return WorldState[state];

            return WorldStateChangeStack[state].Peek().Value;
        }

        public virtual void SetState(int state, TWorldStateEntry value, bool setAsDirty = true, EffectType e = EffectType.Permanent)
        {
            if (ContextState == ContextState.Executing)
            {
                // Prevent setting the world state dirty if we're not changing anything.
                if (WorldState[state].Equals(value))
                    return;

                WorldState[state] = value;
                if (setAsDirty)
                    IsDirty = true; // When a state change during execution, we need to mark the context dirty for replanning!
            }
            else
            {
                WorldStateChangeStack[state].Push(new KeyValuePair<EffectType, TWorldStateEntry> (e, value));
            }
        }

        // ========================================================= STATE STACK HANDLING

        public int[] GetWorldStateChangeDepth(IFactory factory)
        {
            var stackDepth = factory.CreateArray<int>(WorldStateChangeStack.Length);
            for (var i = 0; i < WorldStateChangeStack.Length; i++) stackDepth[i] = WorldStateChangeStack[i]?.Count ?? 0;

            return stackDepth;
        }

        public void TrimForExecution()
        {
            if (ContextState == ContextState.Executing)
                throw new Exception("Can not trim a context when in execution mode");

            foreach (var stack in WorldStateChangeStack)
                while (stack.Count != 0 && stack.Peek().Key != EffectType.Permanent)
                    stack.Pop();
        }

        public void TrimToStackDepth(int[] stackDepth)
        {
            if (ContextState == ContextState.Executing)
                throw new Exception("Can not trim a context when in execution mode");

            for (var i = 0; i < stackDepth.Length; i++)
            {
                var stack = WorldStateChangeStack[i];
                while (stack.Count > stackDepth[i]) stack.Pop();
            }
        }

        // ========================================================= STATE RESET

        public virtual void Reset()
        {
            MethodTraversalRecord?.Clear();
            LastMTR?.Clear();

            if (DebugMTR)
            {
                MTRDebug?.Clear();
                LastMTRDebug?.Clear();
            }

            IsInitialized = false;
        }

        // ========================================================= DECOMPOSITION LOGGING

        public void Log(string name, string description, int depth, ITask<TWorldStateEntry> task, ConsoleColor color = ConsoleColor.White)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Enqueue(new DecomposedCompoundTaskEntry<TWorldStateEntry>
            {
                Name = name,
                Description = description,
                Entry = task,
                Depth = depth,
                Color = color,
            });
        }

        public void Log(string name, string description, int depth, ICondition<TWorldStateEntry> condition, ConsoleColor color = ConsoleColor.DarkGreen)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Enqueue(new DecomposedConditionEntry<TWorldStateEntry>
            {
                Name = name,
                Description = description,
                Entry = condition,
                Depth = depth,
                Color = color
            });
        }

        public void Log(string name, string description, int depth, IEffect<TWorldStateEntry> effect, ConsoleColor color = ConsoleColor.DarkYellow)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Enqueue(new DecomposedEffectEntry<TWorldStateEntry>
            {
                Name = name,
                Description = description,
                Entry = effect,
                Depth = depth,
                Color = color,
            });
        }
    }
}
