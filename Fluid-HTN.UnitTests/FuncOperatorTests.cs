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
            var e = new FuncOperator<MyContext, byte>(null, null);

            e.Update(ctx);
        }

        [TestMethod]
        public void StopDoesNothingWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext, byte>(null, null);

            e.Stop(ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void UpdateThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new FuncOperator<MyContext, byte>(null, null);

            e.Update(null);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void StopThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new FuncOperator<MyContext, byte>(null, null);

            e.Stop(null);
        }

        [TestMethod]
        public void UpdateReturnsStatusInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext, byte>((context) => TaskStatus.Success, null);

            var status = e.Update(ctx);

            Assert.AreEqual(TaskStatus.Success, status);
        }

        [TestMethod]
        public void StopCallsInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new FuncOperator<MyContext, byte>(null, (context) => context.Done = true);

            e.Stop(ctx);

            Assert.AreEqual(true, ctx.Done);
        }
    }
}
