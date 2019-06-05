using System;
using FluidHTN;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class BaseContextTests
    {
        [TestMethod]
        public void DefaultContextStateIsExecuting_ExpectedBehavior()
        {
            var ctx = new MyContext();

            Assert.IsTrue(ctx.ContextState == ContextState.Executing);
        }

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

        [TestMethod]
        public void HasState_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);

            Assert.AreEqual(false, ctx.HasState(MyWorldState.HasA));
            Assert.AreEqual(true, ctx.HasState(MyWorldState.HasB));
        }

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

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TrimForExecutionThrowsExceptionIfWrongContextState_ExpectedBehavior()
        {
            var ctx = new MyContext();

            ctx.Init();
            ctx.ContextState = ContextState.Executing;
            ctx.TrimForExecution();
        }

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
