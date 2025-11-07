using System;
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Effects;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class PlannerTests
    {
        /// <summary>
        /// Verifies that calling Planner.Tick() with null parameters throws a NullReferenceException.
        /// The planner requires both a valid domain and context to execute the planning cycle.
        /// Both parameters are essential: the domain contains the task hierarchy and the context holds world state and planner callbacks.
        /// This test ensures the planner fails fast with a clear exception when given invalid parameters rather than producing silent failures.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void TickWithNullParametersThrowsNRE_ExpectedBehavior()
        {
            var planner = new Planner<MyContext>();
            planner.Tick(null, null);
        }

        /// <summary>
        /// Verifies that calling Planner.Tick() with a null domain but valid context throws an exception.
        /// The domain is essential as it contains the task hierarchy that defines the planner's decomposition logic.
        /// Without a domain, the planner cannot find or execute any tasks, making it impossible to generate a valid plan.
        /// This test validates that the planner enforces the domain requirement through exception throwing.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithNullDomainThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var planner = new Planner<MyContext>();
            planner.Tick(null, ctx);
        }

        /// <summary>
        /// Verifies that calling Planner.Tick() with an uninitialized context throws an exception.
        /// The context must be initialized by calling Init() to set up the internal data structures required for planning and execution.
        /// Initialization creates the necessary collections for the plan queue, decomposition logging, and world state management.
        /// This test validates that the planner requires proper context initialization, preventing usage errors.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void TickWithoutInitializedContextThrowsException_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var domain = new Domain<MyContext>("Test");
            var planner = new Planner<MyContext>();
            planner.Tick(domain, ctx);
        }

        /// <summary>
        /// Verifies that the planner can handle a domain with no tasks without throwing an exception.
        /// A domain with only a root task and no subtasks is valid but results in an empty plan queue and no executable tasks.
        /// This test demonstrates that the planner gracefully handles empty domains, completing without error or plan generation.
        /// This capability is useful for testing and for dynamic domains that start empty and have tasks added at runtime.
        /// </summary>
        [TestMethod]
        public void TickWithEmptyDomain_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var domain = new Domain<MyContext>("Test");
            var planner = new Planner<MyContext>();
            planner.Tick(domain, ctx);
        }

        /// <summary>
        /// Verifies that when a primitive task is selected but has no operator assigned, the planner fails the task appropriately.
        /// Operators are required to execute primitive tasks during plan execution, so a missing operator is a configuration error.
        /// When an operator is missing, the task cannot be executed and the planner marks it as failed, failing the entire plan.
        /// This test demonstrates that the planner validates operator presence and handles missing operators gracefully.
        /// </summary>
        [TestMethod]
        public void TickWithPrimitiveTaskWithoutOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(ctx.PlannerState.CurrentTask == null);
            Assert.IsTrue(ctx.PlannerState.LastStatus == TaskStatus.Failure);
        }

        /// <summary>
        /// Verifies that a FuncOperator with a null function pointer results in task failure when executed.
        /// FuncOperator is a lambda-based operator implementation that wraps a user-provided function.
        /// If the function pointer is null, the operator cannot execute and the task fails.
        /// This test demonstrates that the planner handles null operator functions gracefully by failing the affected task.
        /// </summary>
        [TestMethod]
        public void TickWithFuncOperatorWithNullFunc_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>(null));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(ctx.PlannerState.CurrentTask == null);
            Assert.IsTrue(ctx.PlannerState.LastStatus == TaskStatus.Failure);
        }

        /// <summary>
        /// Verifies that a primitive task with an operator that returns Success completes without stack overflow or infinite loops.
        /// When a task's operator returns Success, the planner pops it from the plan queue and moves to the next task.
        /// This test ensures proper task completion handling prevents infinite loops or recursion issues.
        /// The test validates that successful task completion is handled efficiently without performance issues.
        /// </summary>
        [TestMethod]
        public void TickWithDefaultSuccessOperatorWontStackOverflows_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(ctx.PlannerState.CurrentTask == null);
            Assert.IsTrue(ctx.PlannerState.LastStatus == TaskStatus.Success);
        }

        /// <summary>
        /// Verifies that a primitive task with an operator that returns Continue remains active in the plan for the next tick.
        /// When a task's operator returns Continue, the task is not removed from the plan and remains the current task.
        /// Continue is used for long-running or multi-tick operations that need to maintain state across multiple planning cycles.
        /// This test validates that the planner properly maintains task state across ticks when tasks return Continue.
        /// </summary>
        [TestMethod]
        public void TickWithDefaultContinueOperator_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(ctx.PlannerState.CurrentTask != null);
            Assert.IsTrue(ctx.PlannerState.LastStatus == TaskStatus.Continue);
        }

        /// <summary>
        /// Verifies that the OnNewPlan callback is invoked when the planner generates a new plan during decomposition.
        /// Callbacks are the primary mechanism for applications to observe and react to planning events.
        /// OnNewPlan fires when the domain decomposition succeeds and produces a new plan queue containing tasks to execute.
        /// This test demonstrates that callbacks are properly invoked during the planning cycle, enabling external observation of plan generation.
        /// </summary>
        [TestMethod]
        public void OnNewPlan_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnNewPlan = (p) => { test = p.Count == 1; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnReplacePlan callback is invoked when the planner generates a new plan to replace the currently executing plan.
        /// OnReplacePlan is triggered during replanning when world state changes invalidate the current plan or a better plan becomes available.
        /// The callback receives the old plan, the current task being replaced, and the new plan, enabling applications to handle plan transitions.
        /// This test demonstrates that replanning callbacks work correctly when conditions change during execution.
        /// </summary>
        [TestMethod]
        public void OnReplacePlan_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnReplacePlan = (op, ct, p) => { test = op.Count == 0 && ct != null && p.Count == 1; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnNewTask callback is invoked when the planner pops a new task from the plan queue for execution.
        /// OnNewTask fires each time a task becomes the current executable task, providing hooks for task-level event handling.
        /// This callback allows applications to log, monitor, or trigger side effects whenever a new task begins execution.
        /// This test demonstrates that task-level callbacks are properly invoked during the execution phase of the planner tick.
        /// </summary>
        [TestMethod]
        public void OnNewTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnNewTask = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnNewTaskConditionFailed callback is invoked when a task's planning conditions fail during decomposition.
        /// During planning, the planner evaluates conditions to determine which tasks are valid decomposition paths.
        /// When a condition fails, the task is rejected and the planner explores alternative paths in the hierarchy.
        /// This test demonstrates that planning-level condition failure callbacks work correctly during domain decomposition.
        /// </summary>
        [TestMethod]
        public void OnNewTaskConditionFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnNewTaskConditionFailed = (t, c) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            // Note that one should not use AddEffect on types that's not part of WorldState unless you
            // know what you're doing. Outside of the WorldState, we don't get automatic trimming of 
            // state change. This method is used here only to invoke the desired callback, not because
            // its correct practice.
            task3.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.Done = true));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnCurrentTaskStarted callback is invoked when a primitive task's operator Start method is called.
        /// Operators have a Start method that is called once when a task becomes active, separate from the Update method called each tick.
        /// This callback allows applications to perform one-time initialization when a task begins execution.
        /// This test demonstrates that operator lifecycle callbacks work correctly during task execution startup.
        /// </summary>
        [TestMethod]
        public void OnStartNewTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskStarted = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue, start: (context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that a primitive task can complete successfully during its Start method, before the Update method is called.
        /// The Start method is not limited to initialization—operators can perform complete work and return Success immediately.
        /// This feature enables efficient single-tick operations and allows task completion to happen at startup.
        /// This test demonstrates that task lifecycle callbacks properly reflect successful completion from the Start phase.
        /// </summary>
        [TestMethod]
        public void StartNewTaskCanCompleteTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskCompletedSuccessfully = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue, start: (context) => TaskStatus.Success));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that a primitive task can fail during its Start method, causing immediate failure without Update calls.
        /// Operators can determine during initialization that they cannot proceed and return Failure to abort the task.
        /// This early failure detection enables quick rejection of invalid task executions.
        /// This test demonstrates that task lifecycle callbacks properly reflect failure initiated from the Start phase.
        /// </summary>
        [TestMethod]
        public void StartNewTaskCanFailTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskFailed = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue, start: (context) => TaskStatus.Failure));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnStopCurrentTask callback is invoked when a task is stopped due to replanning or plan changes.
        /// When the plan is replaced, the currently executing task must be stopped to clean up its state and resources.
        /// The Stop method on the operator is called to perform cleanup before the task is replaced.
        /// This test demonstrates that task stop callbacks work correctly during plan transitions.
        /// </summary>
        [TestMethod]
        public void OnStopCurrentTask_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnStopCurrentTask = (t) => { test = t.Name == "Sub-task2"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnCurrentTaskCompletedSuccessfully callback is invoked when a task's operator returns Success.
        /// This callback fires when a task completes its execution successfully, allowing applications to react to task completion.
        /// Successful task completion triggers effects and removes the task from the plan queue.
        /// This test demonstrates that successful task completion callbacks work correctly during plan execution.
        /// </summary>
        [TestMethod]
        public void OnCurrentTaskCompletedSuccessfully_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskCompletedSuccessfully = (t) => { test = t.Name == "Sub-task1"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done == false));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done = true;
            planner.Tick(domain, ctx);

            ctx.Done = false;
            ctx.IsDirty = true;
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnApplyEffect callback is invoked when effects are applied to the context during task completion.
        /// Effects modify world state when tasks complete, and the planner invokes callbacks for each effect application.
        /// This callback enables applications to monitor and react to state changes made by task effects.
        /// This test demonstrates that effect application callbacks work correctly when tasks complete successfully.
        /// </summary>
        [TestMethod]
        public void OnApplyEffect_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnApplyEffect = (e) => { test = e.Name == "TestEffect"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test1" };
            var task2 = new Selector() { Name = "Test2" };
            var task3 = (IPrimitiveTask) new PrimitiveTask() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext>("TestCondition", context => !context.HasState(MyWorldState.HasA)));
            var task4 = new PrimitiveTask() { Name = "Sub-task2" };
            task3.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Success));
            task3.AddEffect(new ActionEffect<MyContext>("TestEffect", EffectType.PlanAndExecute, (context, type) => context.SetState(MyWorldState.HasA, true, type)));
            task4.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(domain.Root, task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, ctx);

            ctx.ContextState = ContextState.Executing;
            ctx.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnCurrentTaskFailed callback is invoked when a task's operator returns Failure.
        /// Task failure triggers replanning because the current plan path is no longer viable.
        /// The callback allows applications to respond to task failures and monitor plan instability.
        /// This test demonstrates that task failure callbacks work correctly during plan execution.
        /// </summary>
        [TestMethod]
        public void OnCurrentTaskFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskFailed = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Failure));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnCurrentTaskContinues callback is invoked when a task's operator returns Continue.
        /// Continue indicates that a task needs more time and will remain the current task in the next planning cycle.
        /// The callback allows applications to monitor long-running task progress and multi-tick operations.
        /// This test demonstrates that task continuation callbacks work correctly during plan execution.
        /// </summary>
        [TestMethod]
        public void OnCurrentTaskContinues_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskContinues = (t) => { test = t.Name == "Sub-task"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the OnCurrentTaskExecutingConditionFailed callback is invoked when an executing condition fails at runtime.
        /// Executing conditions are checked before each task update and allow runtime task invalidation when conditions change.
        /// When an executing condition fails, the task is stopped and replanning is triggered.
        /// This test demonstrates that executing condition failure callbacks enable dynamic task invalidation during execution.
        /// </summary>
        [TestMethod]
        public void OnCurrentTaskExecutingConditionFailed_ExpectedBehavior()
        {
            bool test = false;
            var ctx = new MyContext();
            ctx.Init();
            var planner = new Planner<MyContext>();
            ctx.PlannerState.OnCurrentTaskExecutingConditionFailed = (t, c) => { test = t.Name == "Sub-task" && c.Name == "TestCondition"; };
            var domain = new Domain<MyContext>("Test");
            var task1 = new Selector() { Name = "Test" };
            var task2 = new PrimitiveTask() { Name = "Sub-task" };
            task2.SetOperator(new FuncOperator<MyContext>((context) => TaskStatus.Continue));
            task2.AddExecutingCondition(new FuncCondition<MyContext>("TestCondition", context => context.Done));
            domain.Add(domain.Root, task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert.IsTrue(test);
        }

        /// <summary>
        /// Verifies that the planner can find a better plan when planning conditions change and the current operator returns Continue.
        /// When world state changes affect planning conditions, the planner can trigger replanning while a task continues executing.
        /// The planner uses Method Traversal Record (MTR) comparison to decide whether new plans are better than existing ones.
        /// This test demonstrates that replanning works correctly when conditions improve during task execution.
        /// </summary>
        [TestMethod]
        public void FindPlanIfConditionChangeAndOperatorIsContinuous_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var select = new Selector() { Name = "Test Select" };

            var actionA = new PrimitiveTask() { Name = "Test Action A" };
            actionA.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.Done == true));
            actionA.AddExecutingCondition(new FuncCondition<MyContext>("Can choose A", context => context.Done == true));
            actionA.SetOperator(new MyOperator());
            var actionB = new PrimitiveTask() { Name = "Test Action B" };
            actionB.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.Done == false));
            actionB.AddExecutingCondition(new FuncCondition<MyContext>("Can not choose A", context => context.Done == false));
            actionB.SetOperator(new MyOperator());

            domain.Add(domain.Root, select);
            domain.Add(select, actionA);
            domain.Add(select, actionB);

            Queue<ITask> plan;
            ITask currentTask;

            planner.Tick(domain, ctx, false);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action B");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.Done = true;

            planner.Tick(domain, ctx, true);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action A");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 0);
        }

        /// <summary>
        /// Verifies that the planner finds a better plan when world state changes affect task preconditions during continuous execution.
        /// World state changes can make different tasks valid, triggering the planner to find alternative plans.
        /// The planner compares MTR values to ensure it only switches to genuinely better plans, not equivalent ones.
        /// This test demonstrates that replanning responds to world state changes while maintaining plan stability through MTR comparison.
        /// </summary>
        [TestMethod]
        public void FindPlanIfWorldStateChangeAndOperatorIsContinuous_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var select = new Selector() { Name = "Test Select" };

            var actionA = new PrimitiveTask() { Name = "Test Action A" };
            actionA.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.GetState(MyWorldState.HasA) == 1));
            actionA.SetOperator(new MyOperator());
            var actionB = new PrimitiveTask() { Name = "Test Action B" };
            actionB.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.GetState(MyWorldState.HasA) == 0));
            actionB.SetOperator(new MyOperator());

            domain.Add(domain.Root, select);
            domain.Add(select, actionA);
            domain.Add(select, actionB);

            Queue<ITask> plan;
            ITask currentTask;

            planner.Tick(domain, ctx, false);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action B");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);

            // When we change the condition to Done = true, we should now be able to find a better plan!
            ctx.SetState(MyWorldState.HasA, true, EffectType.Permanent);

            planner.Tick(domain, ctx, true);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action A");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 0);
        }

        /// <summary>
        /// Verifies that the planner correctly finds an alternative plan when world state changes make the current plan invalid.
        /// When the current plan becomes impossible (not just suboptimal), the planner must find any viable alternative.
        /// The planner triggers replanning and may switch to worse MTR plans if the current plan is completely invalid.
        /// This test demonstrates that replanning handles forced transitions to suboptimal but valid plans correctly.
        /// </summary>
        [TestMethod]
        public void FindPlanIfWorldStateChangeToWorseMRTAndOperatorIsContinuous_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var planner = new Planner<MyContext>();
            var domain = new Domain<MyContext>("Test");
            var select = new Selector() { Name = "Test Select" };

            var actionA = new PrimitiveTask() { Name = "Test Action A" };
            actionA.AddCondition(new FuncCondition<MyContext>("Can choose A", context => context.GetState(MyWorldState.HasA) == 0));
            actionA.AddExecutingCondition(new FuncCondition<MyContext>("Can choose A", context => context.GetState(MyWorldState.HasA) == 0));
            actionA.SetOperator(new MyOperator());
            var actionB = new PrimitiveTask() { Name = "Test Action B" };
            actionB.AddCondition(new FuncCondition<MyContext>("Can not choose A", context => context.GetState(MyWorldState.HasA) == 1));
            actionB.AddExecutingCondition(new FuncCondition<MyContext>("Can not choose A", context => context.GetState(MyWorldState.HasA) == 1));
            actionB.SetOperator(new MyOperator());

            domain.Add(domain.Root, select);
            domain.Add(select, actionA);
            domain.Add(select, actionB);

            Queue<ITask> plan;
            ITask currentTask;

            planner.Tick(domain, ctx, false);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action A");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 0);

            // When we change the condition to Done = true, the first plan should no longer be allowed, we should find the second plan instead!
            ctx.SetState(MyWorldState.HasA, true, EffectType.Permanent);

            planner.Tick(domain, ctx, true);
            plan = ctx.PlannerState.Plan;
            currentTask = ctx.PlannerState.CurrentTask;
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
            Assert.IsTrue(currentTask.Name == "Test Action B");
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == 1);
        }

        /// <summary>
        /// Verifies that toggling between plans using only planning conditions (without executing conditions) results in unstable plan switching.
        /// Planning conditions are evaluated once during decomposition, but world state changes during execution don't re-evaluate them.
        /// Therefore, a task with a failed planning condition can still remain the current task if conditions change after decomposition.
        /// This test demonstrates the limitation of relying only on planning conditions and the need for executing conditions.
        /// </summary>
        [TestMethod]
        public void ToggleBetweenTwoPlansWithOnlyPlannerConditionWontWork_ExpectedBehavior()
        {
            var c = new MyContext();
            c.Init();

            var planner = new Planner<MyContext>();
            var domain = new DomainBuilder<MyContext>("Test")
                .Action("A")
                    .Condition("Is True", ctx => ctx.HasState(MyWorldState.HasA))
                    .Do(ctx =>
                {
                    ctx.Done = true;
                    return TaskStatus.Continue;
                })
                .End()
                .Action("B")
                    .Condition("Is False", ctx => ctx.HasState(MyWorldState.HasA) == false)
                    .Do(ctx =>
                {
                    ctx.Done = false;
                    return TaskStatus.Continue;
                })
                .End()
                .Build();

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running Action A

            c.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // Our change triggered a replan, but B can't beat A due to MTR. So A won't get invalidated.
        }

        /// <summary>
        /// Verifies that using executing conditions enables smooth toggling between different plans as conditions change at runtime.
        /// Executing conditions are re-evaluated on each planner tick, allowing tasks to be invalidated when world state changes.
        /// When an executing condition fails, the planner triggers replanning and can switch to alternative tasks.
        /// This test demonstrates that executing conditions enable dynamic and responsive plan switching based on runtime conditions.
        /// </summary>
        [TestMethod]
        public void ToggleBetweenTwoPlansWithExecutingConditionWillWork_ExpectedBehavior()
        {
            var c = new MyContext();
            c.Init();

            var planner = new Planner<MyContext>();
            var domain = new DomainBuilder<MyContext>("Test")
                .Action("A")
                .Condition("Is True", ctx => ctx.HasState(MyWorldState.HasA))
                .ExecutingCondition("Is True", ctx => ctx.HasState(MyWorldState.HasA))
                .Do(ctx =>
                {
                    ctx.Done = true;
                    return TaskStatus.Continue;
                })
                .End()
                .Action("B")
                .Condition("Is False", ctx => ctx.HasState(MyWorldState.HasA) == false)
                .ExecutingCondition("Is False", ctx => ctx.HasState(MyWorldState.HasA) == false)
                .Do(ctx =>
                {
                    ctx.Done = false;
                    return TaskStatus.Continue;
                })
                .End()
                .Build();

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A

            c.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsFalse(c.Done); // Out executing condition will realize that A is no longer valid, and we find B instead.

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A
        }

        /// <summary>
        /// Verifies that operators can detect condition changes and return Success to enable task switching without executing conditions.
        /// Operators have access to the context and can check conditions manually during execution.
        /// If an operator detects that conditions no longer support the current task, it can return Success to complete the task and trigger replanning.
        /// This test demonstrates an alternative approach to plan switching where operators detect and respond to condition changes.
        /// </summary>
        [TestMethod]
        public void ToggleBetweenTwoPlansWithConditionSuccessInOperatorWillWork_ExpectedBehavior()
        {
            var c = new MyContext();
            c.Init();

            var planner = new Planner<MyContext>();
            var domain = new DomainBuilder<MyContext>("Test")
                .Action("A")
                .Condition("Is True", ctx => ctx.HasState(MyWorldState.HasA))
                .Do(ctx =>
                {
                    if (ctx.HasState(MyWorldState.HasA) == false)
                    {
                        return TaskStatus.Success;
                    }

                    ctx.Done = true;
                    return TaskStatus.Continue;
                })
                .End()
                .Action("B")
                .Condition("Is False", ctx => ctx.HasState(MyWorldState.HasA) == false)
                .Do(ctx =>
                {
                    if (ctx.HasState(MyWorldState.HasA))
                    {
                        return TaskStatus.Success;
                    }

                    ctx.Done = false;
                    return TaskStatus.Continue;
                })
                .End()
                .Build();

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A

            c.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsFalse(c.Done); // Out executing condition will realize that A is no longer valid, and we find B instead.

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A
        }

        /// <summary>
        /// Verifies that operators returning Failure does not directly enable plan switching in the same way as Success.
        /// While Failure does trigger replanning, the semantics are different from Success (task failure vs. task completion).
        /// This test demonstrates the distinction between task failure (which occurs due to errors) and task success (completion).
        /// Understanding these semantics is important for designing responsive replanning behaviors.
        /// </summary>
        [TestMethod]
        public void ToggleBetweenTwoPlansWithConditionFailureInOperatorWontWork_ExpectedBehavior()
        {
            var c = new MyContext();
            c.Init();

            var planner = new Planner<MyContext>();
            var domain = new DomainBuilder<MyContext>("Test")
                .Action("A")
                .Condition("Is True", ctx => ctx.HasState(MyWorldState.HasA))
                .Do(ctx =>
                {
                    if (ctx.HasState(MyWorldState.HasA) == false)
                    {
                        return TaskStatus.Failure;
                    }

                    ctx.Done = true;
                    return TaskStatus.Continue;
                })
                .End()
                .Action("B")
                .Condition("Is False", ctx => ctx.HasState(MyWorldState.HasA) == false)
                .Do(ctx =>
                {
                    if (ctx.HasState(MyWorldState.HasA))
                    {
                        return TaskStatus.Failure;
                    }

                    ctx.Done = false;
                    return TaskStatus.Continue;
                })
                .End()
                .Build();

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A

            c.SetState(MyWorldState.HasA, false, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsFalse(c.Done); // Out executing condition will realize that A is no longer valid, and we find B instead.

            c.SetState(MyWorldState.HasA, true, EffectType.Permanent);
            planner.Tick(domain, c);
            Assert.IsTrue(c.Done); // We're running A
        }
    }
}
