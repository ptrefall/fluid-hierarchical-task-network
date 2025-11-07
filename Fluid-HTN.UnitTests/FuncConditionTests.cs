using System;
using FluidHTN.Conditions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class FuncConditionTests
    {
        /// <summary>
        /// Verifies that a FuncCondition correctly stores and exposes the name parameter provided during construction.
        /// In hierarchical task network planning, conditions are boolean validators that gate task decomposition and execution.
        /// Each condition requires a unique name for debugging and logging decomposition decisions.
        /// This test ensures the condition's Name property returns exactly what was passed to the constructor.
        /// </summary>
        [TestMethod]
        public void SetsName_ExpectedBehavior()
        {
            var c = new FuncCondition<MyContext>("Name", null);

            Assert.AreEqual("Name", c.Name);
        }

        /// <summary>
        /// Verifies that a FuncCondition returns false when no validation function is provided, representing an invalid state.
        /// FuncCondition is a lambda-based wrapper that encapsulates a boolean validation check to be evaluated during planning.
        /// When a condition has no function pointer, it cannot validate the world state, so returning false signals that the condition cannot be satisfied.
        /// This test ensures that null function conditions consistently report as invalid rather than throwing exceptions or causing undefined behavior.
        /// </summary>
        [TestMethod]
        public void IsValidFailsWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var c = new FuncCondition<MyContext>("Name", null);

            var result = c.IsValid(ctx);

            Assert.AreEqual(false, result);
        }

        /// <summary>
        /// Verifies that a FuncCondition throws an exception when IsValid is called with a null context parameter.
        /// The context object represents the planner's world state and is essential for conditions to evaluate their validation logic.
        /// Conditions depend on accessing the context to check state variables and make decomposition decisions during planning.
        /// This test ensures that the condition validates its input and fails fast with a clear exception when given invalid parameters rather than causing silent failures.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void IsValidThrowsIfBadContext_ExpectedBehavior()
        {
            var c = new FuncCondition<MyContext>("Name", null);

            c.IsValid(null);
        }

        /// <summary>
        /// Verifies that a FuncCondition correctly invokes the lambda function provided during construction when IsValid is called.
        /// FuncCondition wraps a user-defined boolean function that receives the context and evaluates whether a condition is satisfied in the current world state.
        /// The lambda is executed during decomposition to gate whether a task is valid for selection, enabling data-driven planning decisions.
        /// This test confirms that the condition mechanism properly executes the enclosed function and returns the boolean result that reflects the actual world state.
        /// </summary>
        [TestMethod]
        public void IsValidCallsInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var c = new FuncCondition<MyContext>("Done == false", (context) => context.Done == false);

            var result = c.IsValid(ctx);

            Assert.AreEqual(true, result);
        }
    }
}
