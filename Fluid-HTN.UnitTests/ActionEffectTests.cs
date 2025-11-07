using System;
using FluidHTN;
using FluidHTN.Effects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class ActionEffectTests
    {
        /// <summary>
        /// Verifies that an ActionEffect correctly stores and exposes the name parameter provided during construction.
        /// In hierarchical task network planning, effects are modifications to world state that occur when tasks complete.
        /// Each effect must have a unique name for identification and debugging purposes.
        /// This test ensures the effect's Name property returns exactly what was passed to the constructor.
        /// </summary>
        [TestMethod]
        public void SetsName_ExpectedBehavior()
        {
            var e = new ActionEffect<MyContext>("Name", EffectType.PlanOnly, null);

            Assert.AreEqual("Name", e.Name);
        }

        /// <summary>
        /// Verifies that an ActionEffect correctly stores and exposes the EffectType enumeration value provided during construction.
        /// Effects in HTN planning have different types that control when they are applied: PlanOnly effects modify world state during planning for lookahead,
        /// PlanAndExecute effects apply during both planning and execution, and Permanent effects persist across both phases.
        /// This test ensures the effect's Type property accurately reflects the effect type specified at creation time.
        /// </summary>
        [TestMethod]
        public void SetsType_ExpectedBehavior()
        {
            var e = new ActionEffect<MyContext>("Name", EffectType.PlanOnly, null);

            Assert.AreEqual(EffectType.PlanOnly, e.Type);
        }

        /// <summary>
        /// Verifies that an ActionEffect handles gracefully the case where no effect function is provided by not throwing an exception.
        /// ActionEffect is a wrapper around a lambda function that performs state modifications when effects are applied to the context.
        /// Not all effects need to perform actions—some may represent logical state changes without explicit code execution.
        /// This test ensures that calling Apply on an effect with a null function pointer is a valid no-op rather than an error condition.
        /// </summary>
        [TestMethod]
        public void ApplyDoesNothingWithoutFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new ActionEffect<MyContext>("Name", EffectType.PlanOnly, null);

            e.Apply(ctx);
        }

        /// <summary>
        /// Verifies that an ActionEffect throws an exception when Apply is called with a null context parameter.
        /// The context object is the planner's representation of world state and is essential for effects to modify state.
        /// Effects depend on the context to track state changes and apply modifications to the world representation during planning and execution.
        /// This test ensures that the effect validates its input and fails fast when given invalid parameters rather than silently failing or causing subtle bugs.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void ApplyThrowsIfBadContext_ExpectedBehavior()
        {
            var e = new ActionEffect<MyContext>("Name", EffectType.PlanOnly, null);

            e.Apply(null);
        }

        /// <summary>
        /// Verifies that an ActionEffect correctly invokes the lambda function provided during construction when Apply is called.
        /// ActionEffect wraps a user-defined function that receives the context and effect type and performs world state modifications.
        /// The lambda receives the context object and the effect type being applied, allowing the function to conditionally apply effects based on type.
        /// This test confirms that the effect mechanism properly executes the enclosed function and that state changes made within the lambda are reflected in the context.
        /// </summary>
        [TestMethod]
        public void ApplyCallsInternalFunctionPtr_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var e = new ActionEffect<MyContext>("Name", EffectType.PlanOnly, (c, et) => c.Done = true);

            e.Apply(ctx);

            Assert.AreEqual(true, ctx.Done);
        }
    }
}
