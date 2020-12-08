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
	TEST_CLASS(SelectorTests)
	{
        TEST_METHOD(AddCondition_ExpectedBehavior)
        {
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            bool bRet = task->AddCondition(c);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Conditions().size() == 1);
        }

        TEST_METHOD(AddSubtask_ExpectedBehavior)
        {
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            bool bRet = task->AddSubTask(task2);

            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Subtasks().size() == 1);
        }

        TEST_METHOD(IsValidFailsWithoutSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);

            Assert::IsFalse(task->IsValid(ctx));
        }

        TEST_METHOD(IsValid_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task");
            task->AddSubTask(task2);

            Assert::IsTrue(task->IsValid(ctx));
        }

        TEST_METHOD(DecomposeWithNoSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
        }

        TEST_METHOD(DecomposeWithSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeWithSubtasks2_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Sub-task1"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeWithSubtasks3_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });

            task2->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeMTRFails_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task2->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);
            ctx.LastMTR().push_back(0);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::AreEqual(-1, ctx.MethodTraversalRecord()[0]);
        }
        TEST_METHOD( DecomposeDebugMTRFails_ExpectedBehavior)
        {
            MyDebugContext ctx;
            TaskQueueType  plan;
            ctx.Init();

            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task2->AddCondition(c);
            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");
            task->AddSubTask(task3);

            ctx.LastMTR().push_back(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MTRDebug().size() == 1);
            Assert::IsTrue(ctx.MTRDebug()[0].find("REPLAN FAIL"s) != std::string::npos);
            Assert::IsTrue(ctx.MTRDebug()[0].find("Sub-task2"s) != std::string::npos);
        }

        TEST_METHOD(DecomposeMTRSucceedsWhenEqual_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task2->AddCondition(c);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);
            ctx.LastMTR().push_back(1);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 0);
            Assert::IsTrue(plan.size() == 1);
        }

        TEST_METHOD(DecomposeCompoundSubtaskSucceeds_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");

            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);
            task2->AddSubTask(task3);
            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");
            task2->AddSubTask(task4);

            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task3");
            task->AddSubTask(task5);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 0);
        }

        TEST_METHOD(DecomposeCompoundSubtaskFails_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);

            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);
            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");
            std::shared_ptr<ICondition> c2 = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task4->AddCondition(c2);

            task2->AddSubTask(task3);
            task2->AddSubTask(task4);

            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task3");
            task->AddSubTask(task5);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task3"s, plan.front()->Name());
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 0);
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskFails_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<CompoundTask>  task3 = std::make_shared<Selector>("Test3"s);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task4->AddCondition(c);
            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task2");
            task5->AddCondition(c);

            task3->AddSubTask(task4);
            task3->AddSubTask(task5);

            task2->AddSubTask(task3);
            std::shared_ptr<PrimitiveTask> task6 = std::make_shared<PrimitiveTask>("Sub-task3");
            task6->AddCondition(c);
            task2->AddSubTask(task6);

            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task7 = std::make_shared<PrimitiveTask>("Sub-task4");
            task->AddSubTask(task7);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task4"s, plan.front()->Name());
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 0);
        }

        TEST_METHOD(DecomposeCompoundSubtaskBeatsLastMTR_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);

            task2->AddSubTask(task3);
            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");
            task2->AddSubTask(task4);

            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task3");
            task->AddSubTask(task5);

            ctx.LastMTR().push_back(1);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 0);
        }

        TEST_METHOD( DecomposeCompoundSubtaskEqualToLastMTR_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);
            task2->AddSubTask(task3);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");
            task2->AddSubTask(task4);

            task->AddSubTask(task2);
            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task3");
            task->AddSubTask(task5);

            ctx.LastMTR().push_back(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 0);
        }

        TEST_METHOD(DecomposeCompoundSubtaskLoseToLastMTR_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            std::shared_ptr<CompoundTask>  task = std::make_shared<Selector>("Test"s);
            std::shared_ptr<CompoundTask>  task2 = std::make_shared<Selector>("Test2"s);
            std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);
            task2->AddSubTask(task3);

            std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task2");
            task2->AddSubTask(task4);

            std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task3");
            task5->AddCondition(c);

            task->AddSubTask(task5);
            task->AddSubTask(task2);

            ctx.LastMTR().push_back(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == -1);
        }
	}; 
}