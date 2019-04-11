using System;
using System.Collections.Generic;
using FluidHTN.Compounds;

namespace FluidHTN.Contexts
{
    public abstract class BaseContext : IContext
    {
        // ========================================================= PROPERTIES

        public bool IsDirty { get; set; }
        public ContextState ContextState { get; set; }
        public List<int> MethodTraversalRecord { get; set; } = new List<int>();
        public List<string> MTRDebug { get; set; } = new List<string>();

        public List<int> LastMTR { get; } = new List<int>();

        public List<string> LastMTRDebug { get; set; } = new List<string>();
        public Stack<string> DecompositionLog { get; set; } = new Stack<string>();
        public ICompoundTask PlanStartTaskParent { get; set; }
        public int PlanStartTaskChildIndex { get; set; }

        public abstract byte[] WorldState { get; }

        public Stack<KeyValuePair<EffectType, byte>>[] WorldStateChangeStack { get; protected set; }

        // ========================================================= INITIALIZATION

        public virtual void Init()
        {
            WorldStateChangeStack = new Stack<KeyValuePair<EffectType, byte>>[WorldState.Length];
            for (var i = 0; i < WorldState.Length; i++)
                WorldStateChangeStack[i] = new Stack<KeyValuePair<EffectType, byte>>();
        }

        // ========================================================= STATE HANDLING

        public bool HasState(int state, byte value)
        {
            return GetState(state) == value;
        }

        public byte GetState(int state)
        {
            if (ContextState == ContextState.Executing) return WorldState[state];

            if (WorldStateChangeStack[state].Count == 0) return WorldState[state];

            return WorldStateChangeStack[state].Peek().Value;
        }

        public virtual void SetState(int state, byte value, bool setAsDirty = true, EffectType e = EffectType.Permanent)
        {
            if (ContextState == ContextState.Executing)
            {
                // Prevent setting the world state dirty if we're not changing anything.
                if (WorldState[state] == value)
                    return;

                WorldState[state] = value;
                if (setAsDirty)
                    IsDirty = true; // When a state change during execution, we need to mark the context dirty for replanning!
            }
            else
            {
                WorldStateChangeStack[state].Push(new KeyValuePair<EffectType, byte>(e, value));
            }
        }

        // ========================================================= STATE STACK HANDLING

        public int[] GetWorldStateChangeDepth()
        {
            var stackDepth = new int[WorldStateChangeStack.Length]; // TODO: These should be pooled.
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

            MTRDebug?.Clear();
            LastMTRDebug?.Clear();
        }
    }
}