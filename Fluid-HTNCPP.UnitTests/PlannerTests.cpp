#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "CoreIncludes/Domain.h"
#include "Planners/Planner.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(PlannerTests)
    {
        TEST_METHOD(GetPlanReturnsClearInstanceAtStart_ExpectedBehavior)
        {
            Planner planner;
            auto plan = planner.GetPlan();

            Assert::IsTrue(plan.size() == 0);
        }
        TEST_METHOD(GetCurrentTaskReturnsNullAtStart_ExpectedBehavior)
        {
            Planner planner;
            auto task = planner.GetCurrentTask();

            Assert::IsTrue(task == nullptr);
        }
        TEST_METHOD(TickWithoutInitializedContextThrowsException_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            Assert::ExpectException<std::exception>([&]() { planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx); });
        }
        TEST_METHOD(TickWithEmptyDomain_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
        }

        TEST_METHOD(TickWithPrimitiveTaskWithoutOperator_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test"s);
            SharedPtr<ITask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
            auto currentTask = planner.GetCurrentTask();

            Assert::IsTrue(currentTask == nullptr);
            Assert::IsTrue(planner.LastStatus() == TaskStatus::Failure);
        }

        TEST_METHOD(TickWithFuncOperatorWithNullFunc_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();

            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>    f = MakeSharedPtr<FuncOperator>(nullptr);

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
            auto currentTask = planner.GetCurrentTask();

            Assert::IsTrue(currentTask == nullptr);
            Assert::IsTrue(planner.LastStatus() == TaskStatus::Failure);
        }
        TEST_METHOD(TickWithDefaultSuccessOperatorWontStackOverflows_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f = MakeSharedPtr<FuncOperator>([](IContext& ) { return TaskStatus::Success; });
            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
            auto currentTask = planner.GetCurrentTask();

            Assert::IsTrue(currentTask == nullptr);
            Assert::IsTrue(planner.LastStatus() == TaskStatus::Success);
        }

        TEST_METHOD(TickWithDefaultContinueOperator_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f = MakeSharedPtr<FuncOperator>([](IContext& ) { return TaskStatus::Continue; });

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
            auto currentTask = planner.GetCurrentTask();

            Assert::IsTrue(currentTask != nullptr);
            Assert::IsTrue(planner.LastStatus() == TaskStatus::Continue);
        }

        TEST_METHOD(OnNewPlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            bool        test = false;
            ctx.Init();
            planner.OnNewPlan = [&](TaskQueueType p) { test = (p.size() == 1); };
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnReplacePlan_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnReplacePlan = [&](TaskQueueType op, SharedPtr<ITask> ct, TaskQueueType p) {
                test = ((op.size() == 0) && (ct != nullptr) && (p.size() == 1));
            };
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task1");

            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);
            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            SharedPtr<IOperator> f1 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            SharedPtr<IOperator> f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task3->SetOperator(f1);
            task4->SetOperator(f2);
            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnNewTask_ExpectedBehavior)
        {
            bool test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnNewTask = [&](SharedPtr<ITask>&t) { test = (t->Name() == "Sub-task"); };
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator> f = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);
            Assert::IsTrue(test);
        }
        TEST_METHOD(OnNewTaskConditionFailed_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnNewTaskConditionFailed = [&](SharedPtr<ITask>& t, SharedPtr<ICondition>&) {
                test = (t->Name() == "Sub-task1"s);
            };
            SharedPtr<CompoundTask>  task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);

            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            SharedPtr<IOperator> f = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f);
            // Note that one should not use AddEffect on types that's not part of WorldState unless you
            // know what you're doing. Outside of the WorldState, we don't get automatic trimming of
            // state change. This method is used here only to invoke the desired callback, not because
            // its correct practice.
            SharedPtr<IEffect> effect =
                MakeSharedPtr<ActionEffect>("TestEffect"s, EffectType::PlanAndExecute, [](IContext& context, EffectType ) {
                    static_cast<DomainTestContext&>(context).Done() = true;
                });
            task3->AddEffect(effect);

            SharedPtr<IOperator> f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);
            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnStopCurrentTask_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnStopCurrentTask = [&](SharedPtr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task2"); };

            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<CompoundTask> task2 = MakeSharedPtr<Selector>("Test2"s);

            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);

            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            SharedPtr<IOperator> f1 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task3->SetOperator(f1);
            SharedPtr<IOperator> f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnCurrentTaskCompletedSuccessfully_ExpectedBehavior)
        {
            bool test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnCurrentTaskCompletedSuccessfully = [&](SharedPtr<PrimitiveTask>& t) {
                test = (t->Name() == "Sub-task1"s);
            };
            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<CompoundTask> task2 = MakeSharedPtr<Selector>("Test2"s);

            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);
            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            SharedPtr<IOperator> f1 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f1);
            SharedPtr<IOperator> f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnApplyEffect_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnApplyEffect = [&](SharedPtr<IEffect>& e) { test = (e->Name() == "TestEffect"s); };

            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);
            SharedPtr<CompoundTask> task2 = MakeSharedPtr<Selector>("Test2"s);

            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& context) {
                uint8_t trudat = 1;
                return static_cast<DomainTestContext&>(context).HasState(
                    DomainTestState::HasA,
                    trudat);
            });

            task3->AddCondition(c);

            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            SharedPtr<IOperator> f1 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f1);

            SharedPtr<IEffect> eff =
                MakeSharedPtr<ActionEffect>("TestEffect"s, EffectType::PlanAndExecute, [](IContext& context, EffectType type) {
                    static_cast<DomainTestContext&>(context).SetState(DomainTestState::HasA, true, true, type);
                });

            task3->AddEffect(eff);

            SharedPtr<IOperator> f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.SetContextState(ContextState::Executing);
            ctx.SetState(DomainTestState::HasA, true, true, EffectType::Permanent);
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            ctx.SetContextState(ContextState::Executing);
            ctx.SetState(DomainTestState::HasA, false, true, EffectType::Permanent);
            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskFailed_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskFailed = [&](SharedPtr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task"s); };
            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);

            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Failure; });

            task2->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskContinues_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskContinues = [&](SharedPtr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task"s); };
            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);

            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task2->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskExecutingConditionFailed_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskExecutingConditionFailed = [&](SharedPtr<PrimitiveTask>& t, SharedPtr<ICondition>& c) {
                test = ((t->Name() == "Sub-task"s) && (c->Name() == "TestCondition"s));
            };
            SharedPtr<CompoundTask> task1 = MakeSharedPtr<Selector>("Test1"s);

            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            SharedPtr<IOperator>     f2 = MakeSharedPtr<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task2->SetOperator(f2);

            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& context) {
                return static_cast<DomainTestContext&>(context).Done();
            });

            task2->AddExecutingCondition(c);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick<DomainTestState,uint8_t,DomainTestWorldState>(domain, ctx);

            Assert::IsTrue(test);
        }
	};
}
