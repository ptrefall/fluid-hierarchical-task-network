using System;
using FluidHTN.Conditions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class FuncConditionTests
    {
        [TestMethod]
        public void SetsName_ExpectedBehavior()
        {
            var c = new FuncCondition<MyContext, byte>("Name", null);

            Assert.AreEqual("Name", c.Name);
        }

        [TestMethod]
        public void IsValidFailsWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var c = new FuncCondition<MyContext, byte>("Name", null);

            var result = c.IsValid(ctx);

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void IsValidThrowsIfBadContext_ExpectedBehavior()
        {
            var c = new FuncCondition<MyContext, byte>("Name", null);

            c.IsValid(null);
        }

        [TestMethod]
        public void IsValidCallsInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var c = new FuncCondition<MyContext, byte>("Done == false", (context) => context.Done == false);

            var result = c.IsValid(ctx);

            Assert.AreEqual(true, result);
        }
    }
}
