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
        [TestMethod]
        public void AddCondition_ExpectedBehavior()
        {
            var task = new Sequence<byte>() { Name = "Test" };
            var t = task.AddCondition(new FuncCondition<MyContext, byte>("TestCondition", context => context.Done == false));

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Conditions.Count == 1);
        }

        [TestMethod]
        public void AddSubtask_ExpectedBehavior()
        {
            var task = new Sequence<byte>() { Name = "Test" };
            var t = task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task" });

            Assert.IsTrue(t == task);
            Assert.IsTrue(task.Subtasks.Count == 1);
        }

        [TestMethod]
        public void IsValidFailsWithoutSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence<byte>() { Name = "Test" };

            Assert.IsFalse(task.IsValid(ctx));
        }

        [TestMethod]
        public void IsValid_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task" });

            Assert.IsTrue(task.IsValid(ctx));
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void DecomposeRequiresContextInitFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            var task = new Sequence<byte>() { Name = "Test" };
            var status = task.Decompose(ctx, 0, out var plan);
        }

        [TestMethod]
        public void DecomposeWithNoSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var task = new Sequence<byte>() { Name = "Test" };
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void DecomposeWithSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task1", plan.Peek().Name);
        }

        [TestMethod]
        public void DecomposeNestedSubtasks_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Selector<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.AreEqual("Sub-task2", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        [TestMethod]
        public void DecomposeWithSubtasksOneFail_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void DecomposeWithSubtasksCompoundSubtaskFails_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new Selector<byte>() { Name = "Sub-task1" });
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Failed);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 0);
        }

        [TestMethod]
        public void DecomposeFailureReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(new Selector<byte>() { Name = "Sub-task2" });
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

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Selector<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

            ctx.LastMTR.Add(0);
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 0);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == -1);
        }

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTR2_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Selector<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task2.AddSubtask(task3);

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(0);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Rejected);
            Assert.IsTrue(plan == null);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[1] == -1);
        }

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskEqualToMTR_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Selector<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task2.AddSubtask(task3);

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

            ctx.LastMTR.Add(1);
            ctx.LastMTR.Add(1);
            var status = task.Decompose(ctx, 0, out var plan);

            Assert.IsTrue(status == DecompositionStatus.Succeeded);
            Assert.IsTrue(plan != null);
            Assert.IsTrue(plan.Count == 2);
            Assert.IsTrue(ctx.MethodTraversalRecord.Count == 1);
            Assert.IsTrue(ctx.MethodTraversalRecord[0] == 1);
            Assert.AreEqual("Sub-task3", plan.Dequeue().Name);
            Assert.AreEqual("Sub-task4", plan.Dequeue().Name);
        }

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskLoseToMTRReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Selector<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasB, false, EffectType.PlanOnly))));

            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task5" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, false, EffectType.PlanOnly))));

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

        [TestMethod]
        public void DecomposeNestedCompoundSubtaskFailReturnToPreviousWorldState_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();
            ctx.ContextState = ContextState.Planning;
            ctx.SetState(MyWorldState.HasA, true, EffectType.PlanAndExecute);
            ctx.SetState(MyWorldState.HasB, true, EffectType.Permanent);
            ctx.SetState(MyWorldState.HasC, true, EffectType.PlanOnly);

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Sequence<byte>() { Name = "Test2" };
            var task3 = new Sequence<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" }.AddCondition(new FuncCondition<MyContext, byte>("Done == true", context => context.Done == true)));
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasB, false, EffectType.PlanOnly))));

            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" }
                .AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasA, false, EffectType.PlanOnly))));
            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task5" }.AddEffect(new ActionEffect<MyContext, byte>("TestEffect", EffectType.Permanent, (context, type) => context.SetState(MyWorldState.HasC, false, EffectType.PlanOnly))));

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

        [TestMethod]
        public void PausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task.AddSubtask(new PausePlanTask<byte>());
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

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

        [TestMethod]
        public void ContinuePausedPlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task.AddSubtask(new PausePlanTask<byte>());
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

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
            plan = new Queue<ITask<byte>>();
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

        [TestMethod]
        public void NestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Sequence<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask<byte>());
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

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

        [TestMethod]
        public void ContinueNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Sequence<byte>() { Name = "Test3" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask<byte>());
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });

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
            plan = new Queue<ITask<byte>>();
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

        [TestMethod]
        public void ContinueMultipleNestedPausePlan_ExpectedBehavior()
        {
            var ctx = new MyContext();
            ctx.Init();

            var task = new Sequence<byte>() { Name = "Test" };
            var task2 = new Selector<byte>() { Name = "Test2" };
            var task3 = new Sequence<byte>() { Name = "Test3" };
            var task4 = new Sequence<byte>() { Name = "Test4" };
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task1" });
            task3.AddSubtask(new PausePlanTask<byte>());
            task3.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task2" });

            task2.AddSubtask(task3);
            task2.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task3" });

            task4.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task5" });
            task4.AddSubtask(new PausePlanTask<byte>());
            task4.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task6" });

            task.AddSubtask(task2);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task4" });
            task.AddSubtask(task4);
            task.AddSubtask(new PrimitiveTask<byte>() { Name = "Sub-task7" });

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
            plan = new Queue<ITask<byte>>();
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
            plan = new Queue<ITask<byte>>();
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
