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
        /// <summary>
        /// Verifies that a primitive task correctly adds a condition to its conditions collection and returns the task itself for method chaining.
        /// Conditions are validators evaluated during task decomposition to determine whether a task is applicable in the current world state.
        /// Planning conditions gate whether a task can be selected and included in the plan, enabling data-driven task selection.
        /// This test ensures the fluent builder pattern works correctly by confirming the method returns the task instance and the condition is stored.
        /// </summary>
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new PrimitiveTask() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        /// <summary>
        /// Verifies that a primitive task correctly adds an executing condition to its executing conditions collection and returns the task itself for method chaining.
        /// Executing conditions are runtime validators checked before each task execution tick to ensure the task remains valid during execution.
        /// Unlike planning conditions which gate task selection, executing conditions can invalidate a task mid-execution if world state changes, triggering replanning.
        /// This test ensures executing conditions are properly stored and that the fluent API pattern returns the task for continued builder usage.
        /// </summary>
        [TestMethod]
        public void AddExecutingCondition_ExpectedBehavior()
        {
            var task = new PrimitiveTask() { Name = "Test" };
            var t = task.AddExecutingCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.ExecutingConditions.Count == 1);
        }

        /// <summary>
        /// Verifies that a primitive task correctly adds an effect to its effects collection and returns the task itself for method chaining.
        /// Effects are world state modifications applied when a task completes, representing the task's impact on the world.
        /// Effects can be PlanOnly (applied during planning for lookahead), PlanAndExecute (applied during both phases), or Permanent (persist across both phases).
        /// This test ensures effects are properly collected and that the fluent builder pattern allows chained configuration of effects.
        /// </summary>
        [TestMethod]
        public void AddEffect_ExpectedBehavior()
        {
            var task = new PrimitiveTask() { Name = "Test" };
            var t = task.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.Done = true));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Effects.Count == 1);
        }

        /// <summary>
        /// Verifies that a primitive task correctly stores an operator when SetOperator is called, making it available for execution.
        /// The operator is the execution mechanism that implements the actual work of the primitive task when it is selected for execution.
        /// Operators manage the task lifecycle (Start for initialization, Update for execution loop, Stop for cleanup) and return TaskStatus to indicate progress.
        /// This test ensures the task properly retains the operator for later invocation during plan execution.
        /// </summary>
        [TestMethod]
        public void SetOperator_ExpectedBehavior()
        {
            var task = new PrimitiveTask() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext>(null, null));

            Assert.IsTrue(task.Operator != null);
        }

        /// <summary>
        /// Verifies that a primitive task throws an exception if SetOperator is called more than once, preventing accidental operator replacement.
        /// Each primitive task should have exactly one operator for its execution mechanism, as multiple operators would create ambiguity about which should execute.
        /// Allowing operator replacement could silently introduce bugs where a task's implementation is accidentally overwritten during builder construction.
        /// This test ensures tasks enforce single-operator semantics by rejecting attempts to set a second operator with a clear exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void SetOperatorThrowsExceptionIfAlreadySet_ExpectedBehavior()
        {
            var task = new PrimitiveTask() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext>(null, null));
            task.SetOperator(new FuncOperator<MyContext>(null, null));
        }

        /// <summary>
        /// Verifies that a primitive task correctly applies all its effects to the context when ApplyEffects is called.
        /// Effects represent the consequences of a task completing and modify the world state to reflect what the task accomplished.
        /// ApplyEffects is called when a task completes successfully, allowing each effect to update the context based on its type and user-defined logic.
        /// This test confirms that the task properly iterates through its effects collection and applies each one to the provided context.
        /// </summary>
        [TestMethod]
        public void ApplyEffects_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask() { Name = "Test" };
            var t = task.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.Done = true));
            task.ApplyEffects(ctx);

            Assert.AreEqual(true, ctx.Done);
        }

        /// <summary>
        /// Verifies that a primitive task correctly calls its operator's Stop method when Stop is called, allowing the operator to perform cleanup.
        /// Stop is invoked when a task completes or is interrupted, giving the operator an opportunity to finalize state and perform resource cleanup.
        /// The operator's Stop function can modify world state to record final results or trigger side effects that persist after task completion.
        /// This test confirms that the task delegates to its operator's Stop method and that state changes made in Stop are preserved in the context.
        /// </summary>
        [TestMethod]
        public void StopWithValidOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask() { Name = "Test" };
            task.SetOperator(new FuncOperator<MyContext>(null, funcStop: context => context.Done = true));
            task.Stop(ctx);

            Assert.IsTrue(task.Operator != null);
            Assert.AreEqual(true, ctx.Done);
        }

        /// <summary>
        /// Verifies that a primitive task handles Stop gracefully when no operator is assigned, treating it as a valid no-op.
        /// Tasks may be created without operators for planning purposes or as intermediate task structures not meant for direct execution.
        /// Calling Stop on a taskless operator should not throw an exception but rather complete safely without executing any cleanup logic.
        /// This test ensures tasks are defensive about missing operators and do not fail catastrophically when lifecycle methods are called prematurely.
        /// </summary>
        [TestMethod]
        public void StopWithNullOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask() { Name = "Test" };
            task.Stop(ctx);
        }

        /// <summary>
        /// Verifies that a primitive task's IsValid method returns true only when all its planning conditions are satisfied by the current world state.
        /// IsValid is called during decomposition to determine whether a task can be selected and included in the plan.
        /// A task is valid only if every condition it has returns true when evaluated against the context; a single failing condition makes the task invalid.
        /// This test demonstrates the AND semantics of multiple conditions and shows how adding contradictory conditions (Done == true when Done is false) invalidates the task.
        /// </summary>
        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new PrimitiveTask() { Name = "Test" };
            task.AddCondition(new FuncCondition<MyContext>("Done == false", context => context.Done == false));
            var expectTrue = task.IsValid(ctx);
            task.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true));
            var expectFalse = task.IsValid(ctx);

            Assert.IsTrue(expectTrue);
            Assert.IsFalse(expectFalse);
        }
    }
}
