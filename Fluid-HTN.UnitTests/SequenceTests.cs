using System;
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class SequenceTests
    {
        /// <summary>
        /// Verifies that a Sequence correctly adds a condition to its conditions collection and returns itself for method chaining.
        /// A Sequence is a compound task that decomposes by executing all subtasks in strict order, unlike a Selector which tries alternatives.
        /// Conditions gate whether a sequence is applicable before decomposition begins, evaluated against the current world state.
        /// This test ensures the fluent builder pattern works correctly for sequences by confirming conditions are stored and the method returns the task instance.
        /// </summary>
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new Sequence() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        /// <summary>
        /// Verifies that a Sequence correctly adds a subtask to its subtasks collection and returns itself for method chaining.
        /// Sequences maintain an ordered list of subtasks that must all decompose successfully in sequence (AND semantics).
        /// Unlike selectors which try alternatives until one succeeds, sequences must decompose every subtask in order.
        /// This test confirms the fluent builder pattern allows chaining subtask additions and that each subtask is properly stored.
        /// </summary>
        [TestMethod]
        public void AddSubtask_ExpectedBehavior()
        {
            var task = new Sequence() { Name = "Test" };
            var t = task.AddSubtask(new PrimitiveTask() { Name = "Sub-task" });

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Subtasks.Count == 1);
        }

        /// <summary>
        /// Verifies that a Sequence with no subtasks is considered invalid and cannot be decomposed.
        /// A sequence requires at least one subtask to execute in sequence; an empty sequence has nothing to accomplish.
        /// During decomposition validation, the planner checks if a sequence is valid before attempting decomposition, rejecting invalid sequences early.
        /// This test ensures sequences properly validate their structure and reject empty sequences that would cause decomposition failures.
        /// </summary>
        [TestMethod]
        public void IsValidFailsWithoutSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence() { Name = "Test" };

            Assert.IsFalse(task.IsValid(ctx));
        }

        /// <summary>
        /// Verifies that a Sequence with subtasks is considered valid and can proceed to decomposition.
        /// A sequence is valid when it has at least one subtask to execute in the sequence.
        /// The planner uses IsValid as a gating check before attempting decomposition, enabling early rejection of unsuitable tasks.
        /// This test confirms that sequences with subtasks properly report validity, allowing decomposition to proceed.
        /// </summary>
        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task" });

            Assert.IsTrue(task.IsValid(ctx));
        }

        /// <summary>
        /// Verifies that attempting to decompose without initializing the context causes a NullReferenceException.
        /// The context must be initialized to set up world state tracking structures (WorldStateChangeStack) required for decomposition.
        /// This test demonstrates the context initialization requirement and ensures the decomposition process properly validates context prerequisites.
        /// Without initialization, accessing context state structures fails, preventing decomposition from proceeding safely.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void DecomposeRequiresContextInitFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence() { Name = "Test" };
            var status = task.Decompose(ctx, 0, out var plan);
        }

        /// <summary>
        /// Verifies that attempting to decompose a Sequence with no subtasks returns Failed status and an empty plan.
        /// Decomposition breaks down compound tasks into executable primitives based on the current world state.
        /// When a sequence has no subtasks, there are no tasks to execute in sequence, so decomposition fails with an empty plan queue.
        /// This test confirms the sequence properly handles the edge case of an empty subtask list by returning Failed without crashing.
        /// </summary>
        [TestMethod]
        public void DecomposeWithNoSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var task = new Sequence() { Name = "Test" };
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        /// <summary>
        /// Verifies that a Sequence with valid subtasks successfully decomposes all of them into the plan in order.
        /// Sequences implement AND semantics, requiring all subtasks to decompose successfully and adding all results to the plan.
        /// Since both primitive tasks always decompose successfully, both are added to the plan in sequence order.
        /// This test demonstrates basic sequence decomposition behavior and confirms all subtasks are included in the plan queue.
        /// </summary>
        [TestMethod]
        public void DecomposeWithSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
        }

        /// <summary>
        /// Verifies that a Sequence can decompose nested compound tasks (selectors containing selectors) in sequence.
        /// Sequences recursively decompose all their subtasks in order, including decomposing nested compound tasks.
        /// The nested selector task2/task3 decomposes and produces Sub-task2, which is added to the plan along with Sub-task4 from the sequence.
        /// This test demonstrates that sequences handle complex nesting and properly collect results from all nested decompositions in order.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        /// <summary>
        /// Verifies that a Sequence fails decomposition when any subtask cannot be decomposed.
        /// Sequences require ALL subtasks to decompose successfully for the overall decomposition to succeed (AND semantics).
        /// When the second subtask fails its condition, the sequence cannot proceed and returns Failed with an empty plan.
        /// This test demonstrates the strict sequencing requirement: even if some subtasks decompose, if one fails, the entire sequence fails.
        /// </summary>
        [TestMethod]
        public void DecomposeWithSubtasksOneFail_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        /// <summary>
        /// Verifies that a Sequence fails decomposition when a nested compound task cannot decompose.
        /// Nested compound tasks (like empty selectors) may fail to decompose if they have no valid alternatives.
        /// When a nested compound subtask fails, the entire sequence decomposition fails, and no plan is returned.
        /// This test demonstrates that sequences properly propagate failures from nested decompositions, maintaining AND semantics across nesting levels.
        /// </summary>
        [TestMethod]
        public void DecomposeWithSubtasksCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new Selector() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        /// <summary>
        /// Verifies that when a Sequence fails decomposition, all applied effects are rolled back to restore the previous world state.
        /// During planning, effects applied during subtask decomposition are tracked on the WorldStateChangeStack for rollback capability.
        /// When a sequence fails (because a later subtask fails), the planner must undo all effects applied during the failed decomposition attempt.
        /// This test demonstrates rollback mechanism: despite effects being applied during the first subtask, they're rolled back when the sequence ultimately fails, maintaining planning consistency.
        /// </summary>
        [TestMethod]
        public void DecomposeFailureReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(new Selector() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 1);
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasA));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasB));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasC));
        }

        /// <summary>
        /// Verifies that a Sequence rejects decomposition when a nested selector's MTR choice fails.
        /// Sequences decompose all subtasks in order, tracking MTR choices for each nested selector encountered.
        /// The LastMTR indicates [0, 0] was previously used, but when attempting the same path, the nested selector fails at index 0 and records -1.
        /// This test demonstrates MTR-based rejection in nested sequences: if a previously successful decomposition path no longer works, the sequence rejects.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == -1);
        }

        /// <summary>
        /// Verifies that a Sequence rejects decomposition when a different nested selector's MTR choice fails.
        /// Each selector in the task hierarchy contributes to the MTR path, and sequences must track all of them.
        /// The LastMTR is [1, 0], but when decomposing, the second nested selector at index 0 fails and records -1.
        /// This test demonstrates that MTR rejection cascades through sequence decomposition: one nested selector's failure rejects the entire sequence.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTR2_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(task3);

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == -1);
        }

        /// <summary>
        /// Verifies that a Sequence succeeds decomposition when nested selector MTR choices match and decompose successfully.
        /// When the LastMTR indicates [1, 1] (second option at each selector level), and both nested selectors can decompose to those options, the sequence succeeds.
        /// The plan includes Sub-task3 from the first selector and Sub-task4 from the sequence, matching the MTR-indicated choices.
        /// This test demonstrates that sequences properly handle MTR-constrained decomposition when all nested selectors can satisfy the MTR path.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedCompoundSubtaskEqualToMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task2.AddSubtask(task3);

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(1);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);
            Assert.AreEqual("Sub-task3", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        /// <summary>
        /// Verifies that when a Sequence rejects decomposition due to MTR failure, all applied effects are rolled back to restore previous state.
        /// Effects are applied during decomposition and tracked on the change stack for potential rollback if decomposition fails later.
        /// When a nested selector rejects due to MTR conflict, all effects applied by previous subtasks must be undone to restore consistency.
        /// This test demonstrates combined mechanics: MTR-based rejection coupled with state rollback, ensuring planning maintains a consistent state despite decomposition failures.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTRReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Selector() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasB, false, EffectType.PlanOnly))));

            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task5" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, false, EffectType.PlanOnly))));

            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == -1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 1);
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasA));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasB));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasC));
        }

        /// <summary>
        /// Verifies that when a Sequence fails decomposition due to nested sequence failure, all applied effects are rolled back.
        /// Nested sequences themselves must decompose successfully for the outer sequence to continue.
        /// When a deeply nested sequence fails (because its subtasks have failing conditions), all effects applied during attempts must be undone.
        /// This test demonstrates comprehensive rollback: all effects applied during the entire failed decomposition attempt are rolled back, restoring the initial state exactly.
        /// </summary>
        [TestMethod]
        public void DecomposeNestedCompoundSubtaskFailReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence() { Name = "Test" };
            var task2 = new Sequence() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasB, false, EffectType.PlanOnly))));

            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task5" }.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, false, EffectType.PlanOnly))));

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 1);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 1);
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasA));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasB));
            Assert.AreEqual(1, ctx.GetState(MyWorldState.HasC));
        }

        /// <summary>
        /// Verifies that a Sequence stops decomposition when encountering a PausePlanTask and returns a partial plan.
        /// PausePlanTask is a special task that interrupts decomposition, allowing the planner to resume later at a specific point.
        /// The sequence decomposes up to the pause point (Sub-task1), returns that as a plan, and queues the remaining decomposition for later resumption.
        /// This test demonstrates partial planning: the planner can decompose incrementally, execute some tasks, and resume decomposition from a saved point.
        /// </summary>
        [TestMethod]
        public void PausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PausePlanTask());
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 1);
            Assert.AreEqual(task, ctx.PartialPlanQueue.Peek().Task);
            Assert.AreEqual(2, ctx.PartialPlanQueue.Peek().TaskIndex);
        }

        /// <summary>
        /// Verifies that resuming a paused sequence decomposition continues from the saved point and completes the plan.
        /// When a sequence is paused, the context maintains a queue of partially decomposed tasks with their resume indices.
        /// Resuming decomposition resumes the paused sequence from the saved index, completing the remaining subtasks.
        /// This test demonstrates the complete pause/resume cycle: pause to get Sub-task1, resume to decompose Sub-task2, combining for the full plan.
        /// </summary>
        [TestMethod]
        public void ContinuePausedPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task.AddSubtask(new PausePlanTask());
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 1);
            Assert.AreEqual(task, ctx.PartialPlanQueue.Peek().Task);
            Assert.AreEqual(2, ctx.PartialPlanQueue.Peek().TaskIndex);

            ctx.HasPausedPartialPlan = false;
            plan = new Queue<ITask>();
            while (ctx.PartialPlanQueue.Count > 0)
            {
                var kvp = ctx.PartialPlanQueue.Dequeue();
                var s = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var p);
                if (s == DecompositionStatus.Succeeded || s == DecompositionStatus.Partial)
                {
                    while (p.Count > 0)
                    {
                        plan.Enqueue(p.Dequeue());
                    }
                }
            }
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        /// <summary>
        /// Verifies that pause points in nested tasks are queued in the correct order for later resumption.
        /// When nested sequences encounter pause points, each pause level must maintain its own decomposition state.
        /// The pause queue tracks multiple paused tasks: the innermost paused sequence task3 and the outer sequence task, enabling proper resumption order.
        /// This test demonstrates pause point stacking: pauses at different nesting levels are queued and can be resumed in sequence to complete the entire plan.
        /// </summary>
        [TestMethod]
        public void NestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask());
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);
        }

        /// <summary>
        /// Verifies that resuming nested pause points decomposes in the correct order and produces the complete plan.
        /// When resuming paused nested sequences, each pause level is resumed in the proper order (innermost first).
        /// Resuming the innermost pause (task3 at index 2) produces Sub-task2, then continuing produces Sub-task4 from the outer sequence.
        /// This test demonstrates complete nested pause/resume execution: pauses at multiple levels are resumed in sequence to produce the full plan incrementally.
        /// </summary>
        [TestMethod]
        public void ContinueNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask());
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);

            ctx.HasPausedPartialPlan = false;
            plan = new Queue<ITask>();
            while (ctx.PartialPlanQueue.Count > 0)
            {
                var kvp = ctx.PartialPlanQueue.Dequeue();
                var s = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var p);

                if (s == DecompositionStatus.Succeeded || s == DecompositionStatus.Partial)
                {
                    while (p.Count > 0)
                    {
                        plan.Enqueue(p.Dequeue());
                    }
                }

                if (ctx.HasPausedPartialPlan)
                    break;
            }

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        /// <summary>
        /// Verifies that multiple pause points at different nesting levels are correctly queued and resumed in proper order.
        /// Partial planning with multiple pauses requires managing a stack of paused decomposition points at different levels.
        /// The sequence encounters pauses at nested levels (task3 and task4), queueing them and resuming them in sequence to progressively expand the plan.
        /// This test demonstrates multi-phase partial planning: pauses at different depths are resumed incrementally, eventually producing the complete plan across multiple decomposition phases.
        /// </summary>
        [TestMethod]
        public void ContinueMultipleNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            var task4 = new Sequence() { Name = "Test4" };
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask());
            task3.AddSubtask(new PrimitiveTask() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask() { Name = "Sub-task3" });

            task4.AddSubtask(new PrimitiveTask() { Name = "Sub-task5" });
            task4.AddSubtask(new PausePlanTask());
            task4.AddSubtask(new PrimitiveTask() { Name = "Sub-task6" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task4" });
            task.AddSubtask(task4);
            task.AddSubtask(new PrimitiveTask() { Name = "Sub-task7" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 2);
            var queueAsArray = ctx.PartialPlanQueue.ToArray();
            Assert.AreEqual(task3, queueAsArray[0].Task);
            Assert.AreEqual(2, queueAsArray[0].TaskIndex);
            Assert.AreEqual(task, queueAsArray[1].Task);
            Assert.AreEqual(1, queueAsArray[1].TaskIndex);

            ctx.HasPausedPartialPlan = false;
            plan = new Queue<ITask>();
            while (ctx.PartialPlanQueue.Count > 0)
            {
                var kvp = ctx.PartialPlanQueue.Dequeue();
                var s = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var p);

                if (s == DecompositionStatus.Succeeded || s == DecompositionStatus.Partial)
                {
                    while (p.Count > 0)
                    {
                        plan.Enqueue(p.Dequeue());
                    }
                }

                if (ctx.HasPausedPartialPlan)
                    break;
            }

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 3);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task5", plan.Dequeue().Name);

            ctx.HasPausedPartialPlan = false;
            plan = new Queue<ITask>();
            while (ctx.PartialPlanQueue.Count > 0)
            {
                var kvp = ctx.PartialPlanQueue.Dequeue();
                var s = kvp.Task.Decompose(ctx, kvp.TaskIndex, out var p);

                if (s == DecompositionStatus.Succeeded || s == DecompositionStatus.Partial)
                {
                    while (p.Count > 0)
                    {
                        plan.Enqueue(p.Dequeue());
                    }
                }

                if (ctx.HasPausedPartialPlan)
                    break;
            }

            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task6", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task7", plan.Dequeue().Name);
        }
    }
}
