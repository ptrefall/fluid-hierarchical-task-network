using System;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainTests
    {
        [TestMethod]
        public void DomainHasRootWithDomainName_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            Assert.IsTrue(domain.Root != null);
            Assert.IsTrue(domain.Root.Name == "Test");
        }

        [TestMethod]
        public void AddSubtaskToParent_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Test" };
            domain.Add(task1, task2);
            Assert.IsTrue(task1.Subtasks.Contains(task2));
            Assert.IsTrue(task2.Parent == task1);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void FindPlanNoCtxThrowsNRE_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void FindPlanUninitializedContextThrowsNRE_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void FindPlanNoTasksThenNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
            Assert.IsTrue(plan == null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void MTRNullThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.MethodTraversalRecord = null;

            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
        }

        [TestMethod]
        public void AfterFindPlanContextStateIsExecuting_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
            Assert.IsTrue(ctx.ContextState == ContextState.Executing);
        }

        [TestMethod]
        public void FindPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            var plan = domain.FindPlan(ctx);

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.IsTrue(plan.Peek().Name == "Sub-task");
        }

        [TestMethod]
        public void FindPlanTrimsNonPermanentStateChange_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(new ActionEffect<MyContext>("TestEffect1", EffectType.PlanOnly, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(new ActionEffect<MyContext>("TestEffect2", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasB, true, type)));
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect3", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, true, type)));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            var plan = domain.FindPlan(ctx);

            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasC] == 1);
            Assert.IsTrue(plan.Count == 3);
        }

        [TestMethod]
        public void FindPlanClearsStateChangeWhenPlanIsNull_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(new ActionEffect<MyContext>("TestEffect1", EffectType.PlanOnly, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(new ActionEffect<MyContext>("TestEffect2", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasB, true, type)));
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect3", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, true, type)));
            var task5 = new PrimitiveTask() { Name = "Sub-task4" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            domain.Add(task1, task5);
            var plan = domain.FindPlan(ctx);

            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int)MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int)MyWorldState.HasC] == 0);
            Assert.IsTrue(plan == null);
        }

        [TestMethod]
        public void FindPlanIfMTRsAreEqualThenReturnNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(1);

            // Root is a Selector that branch off into task1 selector or task2 sequence.
            // MTR only tracks decomposition of compound tasks, so our MTR is only 1 layer deep here,
            // Since both compound tasks decompose into primitive tasks.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            var task4 = new PrimitiveTask() { Name = "Sub-task1" };
            var task5 = new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            var task6 = new PrimitiveTask() { Name = "Sub-task3" };
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);
            domain.Add(task2, task5);
            var plan = domain.FindPlan(ctx);

            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
        }
    }
}
