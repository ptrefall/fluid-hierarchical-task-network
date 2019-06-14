using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Debug;
using FluidHTN.Factory;

namespace FluidHTN.Contexts
{
    public abstract class BaseContext : IContext
    {
        // ========================================================= PROPERTIES

        public bool IsDirty { get; set; }
        public ContextState ContextState { get; set; } = ContextState.Executing;
        public abstract IFactory Factory { get; set; }
        public List<int> MethodTraversalRecord { get; set; } = new List<int>();
        public List<int> LastMTR { get; } = new List<int>();
        public abstract List<string> MTRDebug { get; set; }
        public abstract List<string> LastMTRDebug { get; set; }
        public abstract bool DebugMTR { get; }
        public abstract Stack<IBaseDecompositionLogEntry> DecompositionLog { get; set; }
        public abstract bool LogDecomposition { get; }
        public Queue<PartialPlanEntry> PartialPlanQueue { get; set; } = new Queue<PartialPlanEntry>();
        public bool HasPausedPartialPlan { get; set; } = false;

        public abstract byte[] WorldState { get; }

        public Stack<KeyValuePair<EffectType, byte>>[] WorldStateChangeStack { get; protected set; }

        // ========================================================= INITIALIZATION

        public virtual void Init()
        {
            if (WorldStateChangeStack == null)
            {
                WorldStateChangeStack = new Stack<KeyValuePair<EffectType, byte>>[WorldState.Length];
                for (var i = 0; i < WorldState.Length; i++)
                    WorldStateChangeStack[i] = new Stack<KeyValuePair<EffectType, byte>>();
            }

            if (DebugMTR)
            {
                if (MTRDebug == null) MTRDebug = new List<string>();
                if (LastMTRDebug == null) LastMTRDebug = new List<string>();
            }

            if (LogDecomposition)
            {
                if (DecompositionLog == null) DecompositionLog = new Stack<IBaseDecompositionLogEntry>();
            }
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
        }

        // ========================================================= DECOMPOSITION LOGGING

        public void TryLogDecomposition(string name, string description, ICompoundTask task, DecompositionStatus status, Queue<ITask> plan)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Push(new DecomposedCompoundTaskEntry
            {
                Name = name,
                Description = description,
                Entry = new DecomposedCompoundTask
                {
                    Status = status,
                    TaskType = this.GetType().ToString(),
                    Plan = ToDecomposedPrimitiveTasks(plan),
                }
            });
        }

        private DecomposedPrimitiveTask[] ToDecomposedPrimitiveTasks(Queue<ITask> plan)
        {
            if (plan == null || plan.Count == null)
                return null;

            var result = Factory.CreateArray<DecomposedPrimitiveTask>(plan.Count);
            var array = plan.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                result[i] = new DecomposedPrimitiveTask
                {
                    Name = array[i].Name,
                    TaskType = array[i].GetType().ToString(),
                };
            }
            return result;
        }

        public void TryLogDecomposition(string name, string description, ICondition condition, bool result)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Push(new DecomposedConditionEntry
            {
                Name = name,
                Description = description,
                Entry = new DecomposedCondition
                {
                    Result = result,
                    ConditionType = condition.GetType().ToString(),
                }
            });
        }

        public void TryLogDecomposition(string name, string description, IEffect effect)
        {
            if (LogDecomposition == false)
                return;

            DecompositionLog.Push(new DecomposedEffectEntry
            {
                Name = name,
                Description = description,
                Entry = new DecomposedEffect
                {
                    Name = effect.Name,
                    EffectType = effect.GetType().ToString(),
                }
            });
        }
    }
}
