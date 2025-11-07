using System;
using FluidHTN;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class BaseContextTests
    {
        /// <summary>
        /// Verifies that a newly created context initializes with a ContextState of Executing by default.
        /// The context state tracks whether the planner is in Planning mode (building a new plan) or Executing mode (running the current plan).
        /// Executing is the default state because the planner typically starts in execution mode before transitioning to planning when needed.
        /// This test ensures the context begins in the correct state without requiring explicit initialization for typical use cases.
        /// </summary>
        [TestMethod]
        public void DefaultContextStateIsExecuting_ExpectedBehavior()
        {
            var ctx = new MyContext();

            Assert.IsTrue(ctx.ContextState == ContextState.Executing);
        }

        /// <summary>
        /// Verifies that Init properly initializes the context's world state tracking structures without enabling debug facilities.
        /// Init must be called before planning/execution to set up the WorldStateChangeStack, a collection that tracks all state modifications made during planning.
        /// The stack array has one entry per world state enum value, allowing the planner to rewind state changes when backtracking during decomposition.
        /// This test confirms Init creates the necessary collections while leaving debug logging disabled (debugging must be explicitly enabled in derived contexts).
        /// </summary>
        [TestMethod]
        public void InitInitializeCollections_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();

            Assert.AreEqual(true, ctx.WorldStateChangeStack != null);
            Assert.AreEqual(Enum.GetValues(typeof(MyWorldState)).Length, ctx.WorldStateChangeStack.Length);
            Assert.AreEqual(false, ctx.DebugMTR);
            Assert.AreEqual(false, ctx.LogDecomposition);
            Assert.AreEqual(true, ctx.MTRDebug == null);
            Assert.AreEqual(true, ctx.LastMTRDebug == null);
            Assert.AreEqual(true, ctx.DecompositionLog == null);
        }

        /// <summary>
        /// Verifies that Init initializes debug logging collections when the context has debug flags enabled.
        /// Derived contexts can override DebugMTR and LogDecomposition flags to enable detailed decomposition tracing.
        /// When debug is enabled, Init allocates MTRDebug (Method Traversal Record for selector choices), LastMTRDebug (previous MTR for comparison), and DecompositionLog (detailed decomposition trace).
        /// This test confirms that debug contexts properly initialize all logging infrastructure to support detailed plan analysis during development.
        /// </summary>
        [TestMethod]
        public void InitInitializeDebugCollections_ExpectedBehavior()
        {
            var ctx = new MyDebugContext();

            ctx.Init();

            Assert.AreEqual(true, ctx.DebugMTR);
            Assert.AreEqual(true, ctx.LogDecomposition);
            Assert.AreEqual(true, ctx.MTRDebug != null);
            Assert.AreEqual(true, ctx.LastMTRDebug != null);
            Assert.AreEqual(true, ctx.DecompositionLog != null);
        }

        /// <summary>
        /// Verifies that HasState correctly checks if a world state value is currently true based on the context's world state representation.
        /// HasState is a convenience method that checks the byte array representation of world state, treating non-zero values as true.
        /// The context tracks world state using enum-indexed byte arrays for type safety and performance, where each state can be either 0 (false) or 1 (true).
        /// This test demonstrates that HasState accurately reflects the current world state after modifications have been applied.
        /// </summary>
        [TestMethod]
        public void HasState_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(false, ctx.HasState(MyWorldState.HasA));
            Assert.AreEqual(true, ctx.HasState(MyWorldState.HasB));
        }

        /// <summary>
        /// Verifies that SetState in Planning context mode tracks state changes on the WorldStateChangeStack without modifying the actual WorldState array.
        /// During planning, effects are applied speculatively to explore decomposition paths; the change stack records these modifications so they can be rolled back.
        /// SetState pushes the effect and value onto the state's stack but leaves the WorldState byte array unchanged, allowing the planner to rewind changes.
        /// This test confirms the critical planning behavior: state changes are tracked for lookahead without modifying the actual state until execution.
        /// </summary>
        [TestMethod]
        public void SetStatePlanningContext_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(true, ctx.HasState(MyWorldState.HasB));
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Peek().Key == EffectType.Permanent);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Peek().Value == 1);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasB] == 0);
        }

        /// <summary>
        /// Verifies that SetState in Executing context mode immediately modifies the WorldState array without tracking changes on the stack.
        /// During execution, effects are applied directly to the world state because there is no need to track them for rollback.
        /// Executing context treats SetState as a direct state mutation, with changes immediately visible in the WorldState byte array.
        /// This test confirms that execution properly applies effects to the actual world state for normal game/application logic.
        /// </summary>
        [TestMethod]
        public void SetStateExecutingContext_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(true, ctx.HasState(MyWorldState.HasB));
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasB] == 1);
        }

        /// <summary>
        /// Verifies that GetState in Planning context returns the effect-modified value from the change stack, providing lookahead during decomposition.
        /// GetState checks if there are pending changes in the change stack and returns the modified value if found, otherwise returns the base state.
        /// This enables conditions to evaluate the speculative world state during planning, allowing the planner to make decisions based on what the world would be after planned effects.
        /// This test confirms that GetState properly implements the lookahead mechanism for informed task selection during decomposition.
        /// </summary>
        [TestMethod]
        public void GetStatePlanningContext_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(0, ctx.GetState(MyWorldState.HasA));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasB));
        }

        /// <summary>
        /// Verifies that GetState in Executing context returns the actual world state from the WorldState byte array.
        /// During execution, there is no change stack to consult—GetState simply returns the current world state values directly.
        /// This ensures tasks executing see the real, current world state, not a speculative state for planning purposes.
        /// This test confirms that GetState provides accurate state information during task execution for proper behavior control.
        /// </summary>
        [TestMethod]
        public void GetStateExecutingContext_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(0, ctx.GetState(MyWorldState.HasA));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasB));
        }

        /// <summary>
        /// Verifies that GetWorldStateChangeDepth correctly captures the current depth of the change stack for each world state.
        /// GetWorldStateChangeDepth creates a snapshot of the stack depths that can be used to restore the state to this point later via TrimToStackDepth.
        /// During executing, no changes are tracked so all depths remain zero; during planning, depths reflect the number of effects applied to each state.
        /// This test demonstrates the snapshot mechanism that enables the planner to backtrack and explore alternative decomposition paths.
        /// </summary>
        [TestMethod]
        public void GetWorldStateChangeDepth_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            var changeDepthExecuting = ctx.GetWorldStateChangeDepth(ctx.Factory);

            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            var changeDepthPlanning = ctx.GetWorldStateChangeDepth(ctx.Factory);

            Assert.AreEqual(ctx.WorldStateChangeStack.Length, changeDepthExecuting.Length);
            Assert.AreEqual(0, changeDepthExecuting[(int) MyWorldState.HasA]);
            Assert.AreEqual(0, changeDepthExecuting[(int) MyWorldState.HasB]);

            Assert.AreEqual(ctx.WorldStateChangeStack.Length, changeDepthPlanning.Length);
            Assert.AreEqual(0, changeDepthPlanning[(int) MyWorldState.HasA]);
            Assert.AreEqual(1, changeDepthPlanning[(int) MyWorldState.HasB]);
        }

        /// <summary>
        /// Verifies that TrimForExecution removes PlanOnly effects and applies selected effects to the world state, preparing the context for execution.
        /// TrimForExecution is called after a successful plan to clean up planning artifacts and apply the actual world state changes from the plan.
        /// PlanOnly effects are removed (they were only for planning lookahead), Permanent effects stay in the stack, and PlanAndExecute effects transition from stack to world state.
        /// This test demonstrates the critical transition from planning mode to execution mode, where speculative changes become actual world modifications.
        /// </summary>
        [TestMethod]
        public void TrimForExecution_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);
            ctx.TrimForExecution();

            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 0);
        }

        /// <summary>
        /// Verifies that TrimForExecution throws an exception when called in Executing context state rather than Planning.
        /// TrimForExecution is only meaningful during Planning mode when there is a change stack to process; calling it during Execution indicates a programming error.
        /// The exception prevents accidental state corruption by rejecting transitions that only make sense in Planning mode.
        /// This test ensures the context validates its state and fails fast rather than silently performing invalid operations.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TrimForExecutionThrowsExceptionIfWrongContextState_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            ctx.TrimForExecution();
        }

        /// <summary>
        /// Verifies that TrimToStackDepth correctly restores the change stack to a previously captured depth, enabling backtracking during decomposition.
        /// TrimToStackDepth is used by the planner when backtracking to explore alternative task decompositions after one path fails.
        /// It pops changes from the stack until each state's stack matches the provided depth array, effectively undoing speculative changes made during exploration.
        /// This test demonstrates the backtracking mechanism that enables the planner to explore multiple task decomposition paths in a single planning cycle.
        /// </summary>
        [TestMethod]
        public void TrimToStackDepth_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);
            var stackDepth = ctx.GetWorldStateChangeDepth(ctx.Factory);

            ctx.SetState(MyWorldState.HasA, false, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, false, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, false, EffectType.PlanOnly);
            ctx.TrimToStackDepth(stackDepth);

            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 1);
        }

        /// <summary>
        /// Verifies that TrimToStackDepth throws an exception when called in Executing context state rather than Planning.
        /// TrimToStackDepth is only meaningful during Planning mode when there is a change stack to manage; calling it during Execution is a programming error.
        /// The exception prevents accidental state corruption by rejecting backtracking operations that only make sense during plan exploration.
        /// This test ensures the context validates its state for all planning-specific operations and fails fast on invalid usage patterns.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TrimToStackDepthThrowsExceptionIfWrongContextState_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            var stackDepth = ctx.GetWorldStateChangeDepth(ctx.Factory);
            ctx.TrimToStackDepth(stackDepth);
        }
    }
}
