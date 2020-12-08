#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Domain.h"
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
            Assert::ExpectException<std::exception>([&]() { planner.Tick(domain, ctx); });
        }
        TEST_METHOD(TickWithEmptyDomain_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            planner.Tick(domain, ctx);
        }

        TEST_METHOD(TickWithPrimitiveTaskWithoutOperator_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Domain      domain("Test"s);
            Planner     planner;
            ctx.Init();
            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test"s);
            std::shared_ptr<ITask> task2 = std::make_shared<PrimitiveTask>("Sub-task");

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
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

            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>    f = std::make_shared<FuncOperator>(nullptr);

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
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
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f = std::make_shared<FuncOperator>([](IContext& ) { return TaskStatus::Success; });
            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
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
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f = std::make_shared<FuncOperator>([](IContext& ) { return TaskStatus::Continue; });

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
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
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnReplacePlan_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnReplacePlan = [&](TaskQueueType op, std::shared_ptr<ITask> ct, TaskQueueType p) {
                test = ((op.size() == 0) && (ct != nullptr) && (p.size() == 1));
            };
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);
            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");

            std::shared_ptr<IOperator> f1 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            std::shared_ptr<IOperator> f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task3->SetOperator(f1);
            task4->SetOperator(f2);
            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnNewTask_ExpectedBehavior)
        {
            bool test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnNewTask = [&](std::shared_ptr<ITask>&t) { test = (t->Name() == "Sub-task"); };
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator> f = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task2->SetOperator(f);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);
            Assert::IsTrue(test);
        }
        TEST_METHOD(OnNewTaskConditionFailed_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnNewTaskConditionFailed = [&](std::shared_ptr<ITask>& t, std::shared_ptr<ICondition>&) {
                test = (t->Name() == "Sub-task1"s);
            };
            std::shared_ptr<CompoundTask>  task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition>    c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");

            std::shared_ptr<IOperator> f = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f);
            // Note that one should not use AddEffect on types that's not part of WorldState unless you
            // know what you're doing. Outside of the WorldState, we don't get automatic trimming of
            // state change. This method is used here only to invoke the desired callback, not because
            // its correct practice.
            std::shared_ptr<IEffect> effect =
                std::make_shared<ActionEffect>("TestEffect"s, EffectType::PlanAndExecute, [](IContext& context, EffectType ) {
                    static_cast<DomainTestContext&>(context).Done() = true;
                });
            task3->AddEffect(effect);

            std::shared_ptr<IOperator> f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);
            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnStopCurrentTask_ExpectedBehavior)
        {
            bool        test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnStopCurrentTask = [&](std::shared_ptr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task2"); };

            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2"s);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition>    c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");

            std::shared_ptr<IOperator> f1 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task3->SetOperator(f1);
            std::shared_ptr<IOperator> f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }
        TEST_METHOD(OnCurrentTaskCompletedSuccessfully_ExpectedBehavior)
        {
            bool test = false;
            Domain      domain("Test"s);
            DomainTestContext ctx;
            Planner     planner;
            ctx.Init();
            planner.OnCurrentTaskCompletedSuccessfully = [&](std::shared_ptr<PrimitiveTask>& t) {
                test = (t->Name() == "Sub-task1"s);
            };
            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2"s);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition>    c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            task3->AddCondition(c);
            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");

            std::shared_ptr<IOperator> f1 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f1);
            std::shared_ptr<IOperator> f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.Done() = true;
            planner.Tick(domain, ctx);

            ctx.Done() = false;
            ctx.IsDirty() = true;
            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnApplyEffect_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnApplyEffect = [&](std::shared_ptr<IEffect>& e) { test = (e->Name() == "TestEffect"s); };

            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);
            std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2"s);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition>    c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& context) {
                WORLDSTATEPROPERTY_VALUE_TYPE trudat = 1;
                return static_cast<DomainTestContext&>(context).HasState(
                    static_cast<WORLDSTATEPROPERTY_ID_TYPE>(DomainTestState::HasA),
                    trudat);
            });

            task3->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");

            std::shared_ptr<IOperator> f1 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Success; });
            task3->SetOperator(f1);

            std::shared_ptr<IEffect> eff =
                std::make_shared<ActionEffect>("TestEffect"s, EffectType::PlanAndExecute, [](IContext& context, EffectType type) {
                    context.SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(DomainTestState::HasA), true, true, type);
                });

            task3->AddEffect(eff);

            std::shared_ptr<IOperator> f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task4->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(domain.Root(), task2);
            domain.Add(task1, task3);
            domain.Add(task2, task4);

            ctx.SetContextState(ContextState::Executing);
            ctx.SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(DomainTestState::HasA), true, true, EffectType::Permanent);
            planner.Tick(domain, ctx);

            ctx.SetContextState(ContextState::Executing);
            ctx.SetState(static_cast<WORLDSTATEPROPERTY_ID_TYPE>(DomainTestState::HasA), false, true, EffectType::Permanent);
            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskFailed_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskFailed = [&](std::shared_ptr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task"s); };
            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);

            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Failure; });

            task2->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskContinues_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskContinues = [&](std::shared_ptr<PrimitiveTask>& t) { test = (t->Name() == "Sub-task"s); };
            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);

            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });

            task2->SetOperator(f2);

            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }

        TEST_METHOD(OnCurrentTaskExecutingConditionFailed_ExpectedBehavior)
        {
            bool              test = false;
            Domain            domain("Test"s);
            DomainTestContext ctx;
            Planner           planner;
            ctx.Init();
            planner.OnCurrentTaskExecutingConditionFailed = [&](std::shared_ptr<PrimitiveTask>& t, std::shared_ptr<ICondition>& c) {
                test = ((t->Name() == "Sub-task"s) && (c->Name() == "TestCondition"s));
            };
            std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test1"s);

            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            std::shared_ptr<IOperator>     f2 = std::make_shared<FuncOperator>([](IContext&) { return TaskStatus::Continue; });
            task2->SetOperator(f2);

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& context) {
                return static_cast<DomainTestContext&>(context).Done();
            });

            task2->AddExecutingCondition(c);
            domain.Add(domain.Root(), task1);
            domain.Add(task1, task2);

            planner.Tick(domain, ctx);

            Assert::IsTrue(test);
        }
	};
}
