using System;
using System.Collections.Generic;
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
            var planner = new Planner<MyContext>();
            var plan = planner.GetPlan();

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void GetCurrentTaskReturnsNullAtStart_ExpectedBehavior()
        {
            var planner = new Planner<MyContext>();
            var task = planner.GetCurrentTask();

            Assert.IsTrue(task == null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void TickWithNullParametersThrowsNRE_ExpectedBehavior()
        {
            var planner = new Planner<MyContext>();
            planner.Tick(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithNullDomainThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var planner = new Planner<MyContext>();
            planner.Tick(null, ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithoutInitializedContextThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var planner = new Planner<MyContext>();
            planner.Tick(domain, ctx);
        }

        [TestMethod]
        public void TickWithEmptyDomain_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var planner = new Planner<MyContext>();
            planner.Tick(domain, ctx);
        }

        [TestMethod]
        public void TickWithPrimitiveTaskWithoutOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
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
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>(null));
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
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
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
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnNewPlan = (p) => { test = p.Count == 1; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnReplacePlan = (op, ct, p) => { test = op.Count == 0 && ct != null && p.Count == 1; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnNewTask = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnNewTaskConditionFailed = (t, c) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            // Note that one should not use AddEffect on types that's not part of WorldState unless you
            // know what you're doing. Outside of the WorldState, we don't get automatic trimming of 
            // state change. This method is used here only to invoke the desired callback, not because
            // its correct practice.
            task3.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.Done = true));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnStopCurrentTask = (t) => { test = t.Name == "Sub-task2"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnCurrentTaskCompletedSuccessfully = (t) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnApplyEffect = (e) => { test = e.Name == "TestEffect"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => !context.HasState(MyWorldState.HasA)));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            task3.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnCurrentTaskFailed = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Failure));
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
            var planner = new Planner<MyContext>();
            planner.OnCurrentTaskContinues = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
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
            var planner = new Planner<MyContext>();
            planner.OnCurrentTaskExecutingConditionFailed = (t, c) => { test = t.Name == "Sub-task" && c.Name == "TestCondition"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task2.AddExecutingCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void FindPlanIfConditionChangeAndOperatorIsContinuous_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var select = new Selector() { Name = "Test Select" };

            var actionA = new PrimitiveTask() { Name = "Test Action A" };
            actionA.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.Done == true));
            actionA.AddExecutingCondition(new FuncCondition<MyContext>("Can choose A", context => context.Done == true));
            actionA.SetOperator(new MyOperator());
            var actionB = new PrimitiveTask() { Name = "Test Action B" };
            actionB.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.Done == false));
            actionB.AddExecutingCondition(new FuncCondition<MyContext>("Can not choose A", context => context.Done == false));
            actionB.SetOperator(new MyOperator());

            domain.Add(domain.Root, select);
            domain.Add(select, actionA);
            domain.Add(select, actionB);

            Queue<ITask> plan;
            ITask currentTask;

            planner.Tick(domain, ctx, false);
            plan = planner.GetPlan();
            currentTask = planner.GetCurrentTask();
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action B");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.Done = true;

            planner.Tick(domain, ctx, true);
            plan = planner.GetPlan();
            currentTask = planner.GetCurrentTask();
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action A");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 0);
        }

        [TestMethod]
        public void FindPlanIfWorldStateChangeAndOperatorIsContinuous_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var select = new Selector() { Name = "Test Select" };

            var actionA = new PrimitiveTask() { Name = "Test Action A" };
            actionA.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.GetState(MyWorldState.HasA) == 1));
            actionA.SetOperator(new MyOperator());
            var actionB = new PrimitiveTask() { Name = "Test Action B" };
            actionB.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.GetState(MyWorldState.HasA) == 0));
            actionB.SetOperator(new MyOperator());

            domain.Add(domain.Root, select);
            domain.Add(select, actionA);
            domain.Add(select, actionB);

            Queue<ITask> plan;
            ITask currentTask;

            planner.Tick(domain, ctx, false);
            plan = planner.GetPlan();
            currentTask = planner.GetCurrentTask();
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action B");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.SetState(MyWorldState.HasA, true, EffectType.Permanent);

            planner.Tick(domain, ctx, true);
            plan = planner.GetPlan();
            currentTask = planner.GetCurrentTask();
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action A");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 0);
        }
    }
}
