using System;
using FluidHTN;
using FluidHTN.Compounds;
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
        public void FindPlanMustInitContext_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void FindPlanNoTasksThenNoPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var plan = domain.FindPlan(ctx);
            Assert.IsTrue(plan == null);
        }
    }
}
