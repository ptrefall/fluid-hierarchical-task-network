using System;
using FluidHTN;
using FluidHTN.Compounds;
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
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void TickWithNullDomainThrowsNRE_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var planner = new Planner<MyContext>();
            planner.Tick(null, ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void TickWithoutInitializedContextThrowsNRE_ExpectedBehavior()
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
    }
}
