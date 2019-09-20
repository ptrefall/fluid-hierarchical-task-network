using System;
using FluidHTN;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class PrimitiveTaskTests
    {
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        [TestMethod]
        public void AddExecutingCondition_ExpectedBehavior()
        {
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            var t = task.AddExecutingCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.ExecutingConditions.Count == 1);
        }

        [TestMethod]
        public void AddEffect_ExpectedBehavior()
        {
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            var t = task.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.Done = true));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Effects.Count == 1);
        }

        [TestMethod]
        public void SetOperator_ExpectedBehavior()
        {
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext, byte>(null, null));

            Assert.IsTrue(task.Operator != null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void SetOperatorThrowsExceptionIfAlreadySet_ExpectedBehavior()
        {
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext, byte>(null, null));
            task.SetOperator(new FuncOperator<MyContext, byte>(null, null));
        }

        [TestMethod]
        public void ApplyEffects_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            var t = task.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.Done = true));
            task.ApplyEffects(ctx);

            Assert.AreEqual(true, ctx.Done);
        }

        [TestMethod]
        public void StopWithValidOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext, byte>(null, context => context.Done = true));
            task.Stop(ctx);

            Assert.IsTrue(task.Operator != null);
            Assert.AreEqual(true, ctx.Done);
        }

        [TestMethod]
        public void StopWithNullOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            task.Stop(ctx);
        }

        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask<byte>() { Name = "Test" };
            task.AddCondition(new FuncCondition<MyContext, byte>("Done == false", context => context.Done == false));
            var expectTrue = task.IsValid(ctx);
            task.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true));
            var expectFalse = task.IsValid(ctx);

            Assert.IsTrue(expectTrue);
            Assert.IsFalse(expectFalse);
        }
    }
}
