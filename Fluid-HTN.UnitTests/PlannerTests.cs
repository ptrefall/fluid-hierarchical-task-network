using System;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class PlannerTests
    {
        [TestMethod]
        public void GetPlanReturnsClearInstanceAtStart_ExpectedBehavior()
        {
            var planner = new Planner<MyContext, byte>();
            var plan = planner.GetPlan();

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void GetCurrentTaskReturnsNullAtStart_ExpectedBehavior()
        {
            var planner = new Planner<MyContext, byte>();
            var task = planner.GetCurrentTask();

            Assert.IsTrue(task == null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void TickWithNullParametersThrowsNRE_ExpectedBehavior()
        {
            var planner = new Planner<MyContext, byte>();
            planner.Tick(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithNullDomainThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var planner = new Planner<MyContext, byte>();
            planner.Tick(null, ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithoutInitializedContextThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext, byte>("Test");
            var planner = new Planner<MyContext, byte>();
            planner.Tick(domain, ctx);
        }

        [TestMethod]
        public void TickWithEmptyDomain_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext, byte>("Test");
            var planner = new Planner<MyContext, byte>();
            planner.Tick(domain, ctx);
        }

        [TestMethod]
        public void TickWithPrimitiveTaskWithoutOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
            var currentTask = planner.GetCurrentTask();

            Assert.IsTrue(currentTask == null);
            Assert.IsTrue(planner.LastStatus == TaskStatus.Failure);
        }

        [TestMethod]
        public void TickWithFuncOperatorWithNullFunc_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>(null));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
            var currentTask = planner.GetCurrentTask();

            Assert.IsTrue(currentTask == null);
            Assert.IsTrue(planner.LastStatus == TaskStatus.Failure);
        }

        [TestMethod]
        public void TickWithDefaultSuccessOperatorWontStackOverflows_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Success));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
            var currentTask = planner.GetCurrentTask();

            Assert.IsTrue(currentTask == null);
            Assert.IsTrue(planner.LastStatus == TaskStatus.Success);
        }

        [TestMethod]
        public void TickWithDefaultContinueOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
            var currentTask = planner.GetCurrentTask();

            Assert.IsTrue(currentTask != null);
            Assert.IsTrue(planner.LastStatus == TaskStatus.Continue);
        }

        [TestMethod]
        public void OnNewPlan_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnNewPlan = (p) => { test = p.Count == 1; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnReplacePlan_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnReplacePlan = (op, ct, p) => { test = op.Count == 0 && ct != null && p.Count == 1; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test1" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = (IPrimitiveTask<byte>) new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask<byte>() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnNewTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnNewTask = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnNewTaskConditionFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnNewTaskConditionFailed = (t, c) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test1" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = (IPrimitiveTask<byte>) new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask<byte>() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Success));
            // Note that one should not use AddEffect on types that's not part of WorldState unless you
            // know what you're doing. Outside of the WorldState, we don't get automatic trimming of 
            // state change. This method is used here only to invoke the desired callback, not because
            // its correct practice.
            task3.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.Done = true));
            task4.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnStopCurrentTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnStopCurrentTask = (t) => { test = t.Name == "Sub-task2"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test1" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = (IPrimitiveTask<byte>) new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask<byte>() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnCurrentTaskCompletedSuccessfully_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnCurrentTaskCompletedSuccessfully = (t) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test1" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = (IPrimitiveTask<byte>) new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask<byte>() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Success));
            task4.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnApplyEffect_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnApplyEffect = (e) => { test = e.Name == "TestEffect"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test1" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = (IPrimitiveTask<byte>) new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => !context.HasState(MyWorldState.HasA)));
            var task4 = new PrimitiveTask<byte>() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Success));
            task3.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            task4.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, ctx);

            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnCurrentTaskFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnCurrentTaskFailed = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Failure));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnCurrentTaskContinues_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnCurrentTaskContinues = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void OnCurrentTaskExecutingConditionFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext, byte>();
            planner.OnCurrentTaskExecutingConditionFailed = (t, c) => { test = t.Name == "Sub-task" && c.Name == "TestCondition"; };
            var domain = new Domain<MyContext, byte>("Test");
            var task1 = new Selector<byte>() { Name = "Test" };
            var task2 = new PrimitiveTask<byte>() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext, byte>((context) => TaskStatus.Continue));
            task2.AddExecutingCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }
    }
}
