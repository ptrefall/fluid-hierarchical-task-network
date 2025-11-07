using System;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainTests
    {
        /// <summary>
        /// Verifies that a Domain is created with a TaskRoot as its root task, initialized with the domain's name.
        /// The domain is the top-level container for the entire HTN task hierarchy, and TaskRoot is the starting point for decomposition.
        /// TaskRoot is a special compound task that serves as the root of the decomposition tree when the planner begins planning.
        /// This test confirms that domains properly initialize their root task with the provided domain name for identification.
        /// </summary>
        [TestMethod]
        public void DomainHasRootWithDomainName_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            Assert.IsTrue(domain.Root != null);
            Assert.IsTrue(domain.Root.Name == "Test");
        }

        /// <summary>
        /// Verifies that domain.Add correctly establishes parent-child relationships between tasks.
        /// Add registers a task as a subtask of a parent task and sets up the parent reference.
        /// This fluent API enables building task hierarchies where compound tasks contain subtasks that represent alternative or sequential decompositions.
        /// This test confirms the foundational mechanism for constructing HTN task trees.
        /// </summary>
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

        /// <summary>
        /// Verifies that FindPlan throws a NullReferenceException when passed a null context parameter.
        /// FindPlan requires a valid context to access world state and evaluate conditions during decomposition.
        /// Passing null is a programming error that indicates the planner was not properly initialized.
        /// This test ensures the domain fails fast with a clear exception rather than allowing silent failures.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void FindPlanNoCtxThrowsNRE_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(null, out var plan);
        }

        /// <summary>
        /// Verifies that FindPlan throws an exception when the context has not been initialized by calling Init.
        /// Init is required to set up the WorldStateChangeStack and other internal structures that FindPlan depends on.
        /// Calling FindPlan without initialization indicates a setup error and should fail fast.
        /// This test ensures the domain validates context state before attempting decomposition.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void FindPlanUninitializedContextThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        /// <summary>
        /// Verifies that FindPlan returns Rejected status and null plan when the domain has no tasks to decompose.
        /// An empty domain with only a TaskRoot and no subtasks cannot produce a valid plan since there is no work to be done.
        /// FindPlan returns Rejected to indicate that no viable plan could be constructed from the given domain structure.
        /// This test demonstrates graceful handling of empty or invalid domain configurations.
        /// </summary>
        [TestMethod]
        public void FindPlanNoTasksThenNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
        }

        /// <summary>
        /// Verifies that FindPlan throws an exception when the context's MethodTraversalRecord is null.
        /// MTR is used to track the path through selector decisions during decomposition and must be initialized before planning.
        /// Without a valid MTR, the planner cannot track decomposition choices or compare plans via MTR equality.
        /// This test ensures the domain validates critical planner state before attempting decomposition.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void MTRNullThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.MethodTraversalRecord = null;

            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
        }

        /// <summary>
        /// Verifies that FindPlan transitions the context state back to Executing after planning completes.
        /// FindPlan sets context state to Planning during decomposition, then restores it to Executing afterward.
        /// This ensures the context is in the correct state for the planner to begin executing the resulting plan.
        /// This test confirms the planning-to-execution state transition is properly managed by the domain.
        /// </summary>
        [TestMethod]
        public void AfterFindPlanContextStateIsExecuting_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(ctx.ContextState == ContextState.Executing);
        }

        /// <summary>
        /// Verifies that FindPlan successfully decomposes a simple domain hierarchy into an executable plan.
        /// FindPlan recursively decomposes compound tasks into primitive tasks, building a queue of primitive tasks ready for execution.
        /// The resulting plan queue can be popped to execute tasks in order until completion.
        /// This test demonstrates the fundamental planning operation where a domain specification becomes an executable task sequence.
        /// </summary>
        [TestMethod]
        public void FindPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.IsTrue(plan.Peek().Name == "Sub-task");
        }

        /// <summary>
        /// Verifies that FindPlan correctly trims non-Permanent effects and applies only Permanent effects to world state after planning.
        /// After successful planning, TrimForExecution removes PlanOnly effects (they're no longer needed) and transitions state changes to execution mode.
        /// Permanent effects remain and propagate to the actual world state, while PlanAndExecute effects are cleaned from the stack.
        /// This test demonstrates the effect handling during the transition from planning to execution phase.
        /// </summary>
        [TestMethod]
        public void FindPlanTrimsNonPermanentStateChange_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(new ActionEffect<MyContext>("TestEffect1", EffectType.PlanOnly, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(new ActionEffect<MyContext>("TestEffect2", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasB, true, type)));
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect3", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, true, type)));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasC] == 1);
            Assert.IsTrue(plan.Count == 3);
        }

        /// <summary>
        /// Verifies that when FindPlan fails to create a plan (Rejected status), all speculative state changes are cleared.
        /// If planning fails, the world state and change stack must be restored to their original state before planning began.
        /// This prevents failed planning attempts from corrupting the world state with partial effects.
        /// This test confirms the rollback mechanism that ensures planning failures don't leave the context in an invalid state.
        /// </summary>
        [TestMethod]
        public void FindPlanClearsStateChangeWhenPlanIsNull_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task1" }.AddEffect(new ActionEffect<MyContext>("TestEffect1", EffectType.PlanOnly, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            var task3 = new PrimitiveTask() { Name = "Sub-task2" }.AddEffect(new ActionEffect<MyContext>("TestEffect2", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasB, true, type)));
            var task4 = new PrimitiveTask() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext>("TestEffect3", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, true, type)));
            var task5 = new PrimitiveTask() { Name = "Sub-task4" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);
            domain.Add(task1, task4);
            domain.Add(task1, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasA].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasB].Count == 0);
            Assert.IsTrue(ctx.WorldStateChangeStack[(int) MyWorldState.HasC].Count == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasA] == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasB] == 0);
            Assert.IsTrue(ctx.WorldState[(int) MyWorldState.HasC] == 0);
            Assert.IsTrue(plan == null);
        }

        /// <summary>
        /// Verifies that FindPlan returns Rejected when the Method Traversal Record matches the previous plan's MTR.
        /// MTR equality indicates that the new decomposition follows the same selector choices as the last plan, making them equivalent.
        /// Returning the same plan repeatedly would create an infinite loop, so the planner must reject MTR-equal plans to force exploration of alternatives.
        /// This test demonstrates the MTR-based plan comparison mechanism that prevents repetitive planning cycles.
        /// </summary>
        [TestMethod]
        public void FindPlanIfMTRsAreEqualThenReturnNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // Root is a Selector that branch off into task1 selector or task2 sequence.
            // MTR tracks decomposition of compound tasks and priary tasks that are subtasks of selectors,
            // so our MTR is 2 layer deep.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            var task4 = new PrimitiveTask() { Name = "Sub-task1" };
            var task5 = new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));

            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);
            domain.Add(task2, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.AreEqual(ctx.MethodTraversalRecord[0], ctx.LastMTR[0]);
            Assert.AreEqual(ctx.MethodTraversalRecord[1], ctx.LastMTR[1]);
        }

        /// <summary>
        /// Verifies that FindPlan treats plans with equal MTRs as equivalent even if their actual task sequences differ.
        /// MTR equality is the primary metric for plan comparison; if MTRs are equal, the plans are considered equivalent from a planning perspective.
        /// This prevents the planner from cycling between different permutations of the same decomposition choices.
        /// This test confirms that MTR-based equivalence takes precedence over literal task sequence comparison.
        /// </summary>
        [TestMethod]
        public void FindPlanIfPlansAreDifferentButMTRsAreEqualThenReturnNullPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);

            // Root is a Selector that branch off into task1 selector or task2 sequence.
            // MTR tracks decomposition of compound tasks and priary tasks that are subtasks of selectors,
            // so our MTR is 2 layer deep.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Sequence() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));
            var task4 = new PrimitiveTask() { Name = "Sub-task1" };
            var task5 = new PrimitiveTask() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == true));

            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);
            domain.Add(task2, task5);
            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == ctx.LastMTR[1]);
        }

        /// <summary>
        /// Verifies that FindPlan can find a better plan (with different MTR) when world state changes make it possible.
        /// When the current MTR is equal to LastMTR, the plan is rejected. However, if world state changes cause a selector to make different choices,
        /// the new MTR will differ and the new plan will be accepted if valid.
        /// This test demonstrates the replanning mechanism: state changes can invalidate the last plan, requiring exploration of new decomposition paths.
        /// </summary>
        [TestMethod]
        public void FindPlanIfSelectorFindBetterPrimaryTaskMTRChangeSuccessfully_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(1);

            // Root is a Selector that branch off into two primary tasks.
            // We intend for task3 (Test Action B) to be selected in the first run,
            // but it will be a rejected plan because of LastMTR equality.
            // We then change the Done state to true before we do a replan,
            // and now we intend task 2 (Test Action A) to be selected, since its MTR beast LastMTR score.
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test Select" };
            var task2 = new PrimitiveTask() { Name = "Test Action A" }.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.Done == true));
            var task3 = new PrimitiveTask() { Name = "Test Action B" }.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.Done == false));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);
            domain.Add(task1, task3);

            // We expect this to first get rejected, because LastMTR holds [0, 1] which is what we'll get back from the planner.
            var status = domain.FindPlan(ctx, out var plan);
            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == ctx.LastMTR[1]);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.Done = true;
            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == ctx.LastMTR[0]);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] < ctx.LastMTR[1]);
        }

        /// <summary>
        /// Verifies that FindPlan returns Partial status when a PausePlanTask is encountered during decomposition.
        /// PausePlanTask is a special task that pauses planning, returning control to allow task execution before continuing.
        /// The context records the pause point with the task and subtask index, enabling continuation later.
        /// This test demonstrates partial planning where the plan is returned in incremental chunks between pause points.
        /// </summary>
        [TestMethod]
        public void PausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            domain.Add(domain.Root, task);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task, new PausePlanTask());
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

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
        /// Verifies that calling FindPlan again after a pause resumes decomposition from the pause point.
        /// The context's PartialPlanQueue tracks where decomposition paused, allowing FindPlan to resume and complete the remaining tasks.
        /// This enables a two-phase execution model: execute some tasks, then plan the remaining tasks based on execution outcomes.
        /// This test demonstrates continuation of partial plans and the completion of a paused decomposition.
        /// </summary>
        [TestMethod]
        public void ContinuePausedPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            domain.Add(domain.Root, task);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task, new PausePlanTask());
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task1", plan.Dequeue().Name);
            Assert.IsTrue(ctx.HasPausedPartialPlan);
            Assert.IsTrue(ctx.PartialPlanQueue.Count == 1);
            Assert.AreEqual(task, ctx.PartialPlanQueue.Peek().Task);
            Assert.AreEqual(2, ctx.PartialPlanQueue.Peek().TaskIndex);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 1);
            Assert.AreEqual("Sub-task2", plan.Peek().Name);
        }

        /// <summary>
        /// Verifies that pauses work correctly with nested compound tasks, maintaining a queue of pause points at multiple nesting levels.
        /// The PartialPlanQueue is a stack of pause points, each with the task and index where decomposition paused.
        /// Nested decomposition can pause at multiple levels, and the queue tracks all pause points for proper resumption.
        /// This test demonstrates partial planning with nested task hierarchies and multiple pause boundaries.
        /// </summary>
        [TestMethod]
        public void NestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var domain = new Domain<MyContext>("Test");
            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };

            domain.Add(domain.Root, task);
            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

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
        /// Verifies that resuming a nested paused plan correctly continues from all pause points in the queue.
        /// When continuing, the pause queue is processed in order, resuming each paused task and collecting the remaining tasks.
        /// This enables multi-level partial execution where different levels of the hierarchy can contribute tasks to the final plan.
        /// This test demonstrates the full lifecycle of nested partial planning: pause, execute, resume, and completion.
        /// </summary>
        [TestMethod]
        public void ContinueNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };

            domain.Add(domain.Root, task);
            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            var status = domain.FindPlan(ctx, out var plan);

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

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        /// <summary>
        /// Verifies that multiple pause points at different nesting levels are correctly queued and resumed in the proper order.
        /// Partial planning enables breaking decomposition into multiple planning phases via PausePlanTask, where paused decomposition points are stacked.
        /// When multiple compound tasks have pause points at different nesting levels, the context maintains a stack of pending partial plans that must be resumed in the correct order (innermost depth first, then backing up to outer levels).
        /// This test demonstrates that the planner correctly manages deep nesting scenarios with multiple pause points, resuming each paused decomposition from the correct task in the correct execution order, ultimately producing a complete plan when all pauses are resumed.
        /// </summary>
        [TestMethod]
        public void ContinueMultipleNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");

            var task = new Sequence() { Name = "Test" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = new Sequence() { Name = "Test3" };
            var task4 = new Sequence() { Name = "Test4" };

            domain.Add(domain.Root, task);

            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task1" });
            domain.Add(task3, new PausePlanTask());
            domain.Add(task3, new PrimitiveTask() { Name = "Sub-task2" });

            domain.Add(task2, task3);
            domain.Add(task2, new PrimitiveTask() { Name = "Sub-task3" });

            domain.Add(task4, new PrimitiveTask() { Name = "Sub-task5" });
            domain.Add(task4, new PausePlanTask());
            domain.Add(task4, new PrimitiveTask() { Name = "Sub-task6" });

            domain.Add(task, task2);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task4" });
            domain.Add(task, task4);
            domain.Add(task, new PrimitiveTask() { Name = "Sub-task7" });

            var status = domain.FindPlan(ctx, out var plan);

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

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Partial);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 3);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task5", plan.Dequeue().Name);

            status = domain.FindPlan(ctx, out plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task6", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task7", plan.Dequeue().Name);
        }
    }
}
