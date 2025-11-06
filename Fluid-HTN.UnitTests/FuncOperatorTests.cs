using System;
using FluidHTN;
using FluidHTN.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class FuncOperatorTests
    {
        [TestMethod]
        public void UpdateDoesNothingWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>(null);

            e.Update(ctx);
        }

        [TestMethod]
        public void StartDoesNothingWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>(null);

            e.Start(ctx);
        }

        [TestMethod]
        public void StopDoesNothingWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>(null);

            e.Stop(ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void UpdateThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new FuncOperator<MyContext>(null);

            e.Update(null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void StartThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new FuncOperator<MyContext>(null);

            e.Start(null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void StopThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new FuncOperator<MyContext>(null);

            e.Stop(null);
        }

        [TestMethod]
        public void UpdateReturnsStatusInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>((context) => TaskStatus.Success);

            var status = e.Update(ctx);

            Assert.AreEqual(TaskStatus.Success, status);
        }

        [TestMethod]
        public void StartReturnsStatusInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>(null, start: (context) => TaskStatus.Success);

            var status = e.Start(ctx);

            Assert.AreEqual(TaskStatus.Success, status);
        }

        [TestMethod]
        public void StopCallsInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext>(func: null, funcStop: (context) => context.Done = true);

            e.Stop(ctx);

            Assert.AreEqual(true, ctx.Done);
        }
    }
}
