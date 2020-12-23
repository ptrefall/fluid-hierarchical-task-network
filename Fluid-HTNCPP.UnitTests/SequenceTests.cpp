
#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "CoreIncludes/Domain.h"
#include "Planners/Planner.h"
#include "Tasks/CompoundTasks/Sequence.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/PausePlanTask.h"
#include "Effects/Effect.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

namespace Microsoft::VisualStudio::CppUnitTestFramework
{
template <>
std::wstring ToString<ITask>(ITask* eff)
{
    if (eff)
    {
        switch (eff->GetType())
        {
            case ITaskDerivedClassName::CompoundTask:
                return L"ITaskDerivedClassName::CompoundTask";
            case ITaskDerivedClassName::ITaskType:
                return L"ITaskDerivedClassName::ITaskType";
            case ITaskDerivedClassName::PausePlanTask:
                return L"ITaskDerivedClassName::PausePlanTask";
            case ITaskDerivedClassName::PrimitiveTask:
                return L"ITaskDerivedClassName::PrimitiveTask";
            case ITaskDerivedClassName::SelectorCompoundTask:
                return L"ITaskDerivedClassName::SelectorCompoundTask";
            case ITaskDerivedClassName::SequenceCompoundTask:
                return L"ITaskDerivedClassName::SequenceCompoundTask";
            case ITaskDerivedClassName::Slot:
                return L"ITaskDerivedClassName::Slot";
            case ITaskDerivedClassName::TaskRoot:
                return L"ITaskDerivedClassName::TaskRoot";
        }
    }
    return L"Unknown value";
}
} // namespace Microsoft::VisualStudio::CppUnitTestFramework
namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(SequenceTests)
    {
        TEST_METHOD(AddCondition_ExpectedBehavior)
        {
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("TestCondition"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            bool bRet = task->AddCondition(c);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Conditions().size() == 1);
        }

        TEST_METHOD(AddSubtask_ExpectedBehavior)
        {
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            bool bRet = task->AddSubTask(task2);

            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Subtasks().size() == 1);
        }

        TEST_METHOD(IsValidFailsWithoutSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);

            Assert::IsFalse(task->IsValid(ctx));
        }

        TEST_METHOD(IsValid_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task");
            task->AddSubTask(task2);

            Assert::IsTrue(task->IsValid(ctx));
        }

        TEST_METHOD(DecomposeRequiresContextInitFails_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            Assert::ExpectException<std::exception>([&]() {
                TaskQueueType plan;
                task->Decompose(ctx, 0, plan);
            });
        }

        TEST_METHOD(DecomposeWithNoSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
        }

        TEST_METHOD(DecomposeWithSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task2");

            task->AddSubTask(task2);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 2);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeNestedSubtasks_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Test3"s);

            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task4->AddCondition(c);
            task3->AddSubTask(task4);
            SharedPtr<PrimitiveTask> task5 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(task5);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> task6 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task2->AddSubTask(task6);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> task7 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(task7);

            auto status = task->Decompose(ctx, 0,plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 2);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task4"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeWithSubtasksOneFail_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task3->AddCondition(c);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
        }

        TEST_METHOD(DecomposeWithSubtasksCompoundSubtaskFails_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask> task2 = MakeSharedPtr<Selector>("Sub-task1");
            SharedPtr<PrimitiveTask> task3 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task->AddSubTask(task2);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
        }

        TEST_METHOD(DecomposeFailureReturnToPreviousWorldState_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetState(DomainTestState::HasA, true,true, EffectType::PlanAndExecute);
            ctx.SetState(DomainTestState::HasB, true,true, EffectType::Permanent);
            ctx.SetState(DomainTestState::HasC, true,true, EffectType::PlanOnly);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> task2 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<IEffect>       eff =
                MakeSharedPtr<ActionEffect>("TestEffect"s, EffectType::Permanent, [](IContext& context, EffectType ) {
                    static_cast<DomainTestContext&>(context).SetState(DomainTestState::HasA,
                                     false,
                                     true,
                                     EffectType::PlanOnly);
                });
            task2->AddEffect(eff) ;
            task->AddSubTask(task2);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Sub-task2"s);
            task->AddSubTask(task3);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasA].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int)DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int)DomainTestState::HasC].size() == 1);
            Assert::AreEqual(1,(int) ctx.GetStateDTS(DomainTestState::HasA));
            Assert::AreEqual(1, (int)ctx.GetStateDTS(DomainTestState::HasB));
            Assert::AreEqual(1, (int)ctx.GetStateDTS(DomainTestState::HasC));
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskLoseToMTR_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Test3"s);
            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task4->AddCondition(c);
            task3->AddSubTask(task4);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(subtask2);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task2->AddSubTask(subtask3);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(subtask4);

            ctx.LastMTR().Add(0);
            ctx.LastMTR().Add(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 2);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord()[1] == -1);
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskLoseToMTR2_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            TaskQueueType                 plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Test3"s);
            SharedPtr<PrimitiveTask> task4 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            task4->AddCondition(c);
            task3->AddSubTask(task4);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(subtask2);

            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            subtask3->AddCondition(c);
            task2->AddSubTask(subtask3);
            task2->AddSubTask(task3);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(subtask4);

            ctx.LastMTR().Add(1);
            ctx.LastMTR().Add(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 2);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[1] == -1);
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskEqualToMTR_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Test3"s);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            subtask2->AddCondition(c);
            task3->AddSubTask(subtask2);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task3->AddSubTask(subtask3);

            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            subtask1->AddCondition(c);

            task2->AddSubTask(subtask1);
            task2->AddSubTask(task3);

            task->AddSubTask(task2);

            SharedPtr<PrimitiveTask> task6 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(task6);

            ctx.LastMTR().Add(1);
            ctx.LastMTR().Add(1);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Succeeded);
            Assert::IsTrue(plan.size() == 2);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 1);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 1);
            Assert::AreEqual("Sub-task3"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task4"s, plan.front()->Name());
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskLoseToMTRReturnToPreviousWorldState_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetState(DomainTestState::HasA, true,true, EffectType::PlanAndExecute);
            ctx.SetState(DomainTestState::HasB, true,true, EffectType::Permanent);
            ctx.SetState(DomainTestState::HasC, true,true, EffectType::PlanOnly);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Selector>("Test3"s);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            subtask2->AddCondition(c);
            task3->AddSubTask(subtask2);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            SharedPtr<IEffect>       eff =
                MakeSharedPtr<ActionEffect>("TestEffect"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasA, false, true, EffectType::PlanOnly);
                });
            subtask3->AddEffect(eff);
            task3->AddSubTask(subtask3);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            SharedPtr<IEffect>       eff2 =
                MakeSharedPtr<ActionEffect>("TestEffect2"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasB, false, true, EffectType::PlanOnly);
                });
            subtask4->AddEffect(eff2);
            task2->AddSubTask(subtask4);

            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            subtask1->AddEffect(eff);
            task->AddSubTask(subtask1);
            task->AddSubTask(task2);

            SharedPtr<PrimitiveTask> subtask5 = MakeSharedPtr<PrimitiveTask>("Sub-task5");
            SharedPtr<IEffect>       eff3 =
                MakeSharedPtr<ActionEffect>("TestEffect3"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasC, false, true, EffectType::PlanOnly);
                });
            subtask5->AddEffect(eff3);
            task->AddSubTask(subtask5);

            ctx.LastMTR().Add(0);
            ctx.LastMTR().Add(0);
            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Rejected);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord().size() == 2);
            Assert::IsTrue(ctx.MethodTraversalRecord()[0] == 0);
            Assert::IsTrue(ctx.MethodTraversalRecord()[1] == -1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasA].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasC].size() == 1);
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasA));
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasB));
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasC));
        }

        TEST_METHOD(DecomposeNestedCompoundSubtaskFailReturnToPreviousWorldState_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetState(DomainTestState::HasA, true, true, EffectType::PlanAndExecute);
            ctx.SetState(DomainTestState::HasB, true, true, EffectType::Permanent);
            ctx.SetState(DomainTestState::HasC, true, true, EffectType::PlanOnly);

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Sequence>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Sequence>("Test3"s);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            SharedPtr<ICondition>    c = MakeSharedPtr<FuncCondition>("Done == true"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });
            subtask2->AddCondition(c);
            task3->AddSubTask(subtask2);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            SharedPtr<IEffect>       eff =
                MakeSharedPtr<ActionEffect>("TestEffect"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasA, false, true, EffectType::PlanOnly);
                });
            subtask3->AddEffect(eff);
            task3->AddSubTask(subtask3);
            task2->AddSubTask(task3);

            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            SharedPtr<IEffect>       eff2 =
                MakeSharedPtr<ActionEffect>("TestEffect2"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasB, false, true, EffectType::PlanOnly);
                });
            subtask4->AddEffect(eff2);

            task2->AddSubTask(subtask4);

            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            subtask1->AddEffect(eff);
            task->AddSubTask(subtask1);

            task->AddSubTask(task2);

            SharedPtr<PrimitiveTask> subtask5 = MakeSharedPtr<PrimitiveTask>("Sub-task5");
            SharedPtr<IEffect>       eff3 =
                MakeSharedPtr<ActionEffect>("TestEffect3"s, EffectType::Permanent, [](IContext& ctx, EffectType ) {
                    static_cast<DomainTestContext&>(ctx).SetState(DomainTestState::HasC, false, true, EffectType::PlanOnly);
                });
            subtask5->AddEffect(eff3);
            task->AddSubTask(subtask5);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Failed);
            Assert::IsTrue(plan.size() == 0);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int)DomainTestState::HasA].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int)DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int)DomainTestState::HasC].size() == 1);
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasA));
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasB));
            Assert::AreEqual(1, (int)ctx.GetState(DomainTestState::HasC));
        }

        TEST_METHOD(PausePlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task->AddSubTask(subtask1);
            SharedPtr<PausePlanTask> pause = MakeSharedPtr<PausePlanTask>();
            task->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task->AddSubTask(subtask2);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Partial);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
            Assert::IsTrue(ctx.HasPausedPartialPlan());
            Assert::IsTrue(ctx.PartialPlanQueue().size() == 1);
            ITask* tptr1 = static_cast<ITask*>(task.get());
            Assert::AreEqual(tptr1, ctx.PartialPlanQueue().front().Task.get());
            Assert::AreEqual(2, ctx.PartialPlanQueue().front().TaskIndex);
        }

        TEST_METHOD(ContinuePausedPlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task->AddSubTask(subtask1);
            SharedPtr<PausePlanTask> pause = MakeSharedPtr<PausePlanTask>();
            task->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task->AddSubTask(subtask2);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Partial);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
            Assert::IsTrue(ctx.HasPausedPartialPlan());
            Assert::IsTrue(ctx.PartialPlanQueue().size() == 1);
            ITask* tptr1 = static_cast<ITask*>(task.get());
            Assert::AreEqual(tptr1, ctx.PartialPlanQueue().front().Task.get());
            Assert::AreEqual(2, ctx.PartialPlanQueue().front().TaskIndex);

            ctx.HasPausedPartialPlan() = false;
            plan = TaskQueueType();
            while (ctx.PartialPlanQueue().size() > 0)
            {
                auto kvp = ctx.PartialPlanQueue().front();
                ctx.PartialPlanQueue().pop();
                TaskQueueType p;
                auto s = StaticCastPtr<CompoundTask>(kvp.Task)->Decompose(ctx, kvp.TaskIndex, p);
                if (s == DecompositionStatus::Succeeded || s == DecompositionStatus::Partial)
                {
                    while (p.size() > 0)
                    {
                        plan.push(p.front());
                        p.pop();
                    }
                }
            }
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
        }

        TEST_METHOD(NestedPausePlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Sequence>("Test3"s);
            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task3->AddSubTask(subtask1);
            SharedPtr<PausePlanTask> pause = MakeSharedPtr<PausePlanTask>();
            task3->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(subtask2);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task2->AddSubTask(subtask3);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(subtask4);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Partial);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
            Assert::IsTrue(ctx.HasPausedPartialPlan());
            Assert::IsTrue(ctx.PartialPlanQueue().size() == 2);
            PartialPlanQueueType queueCopy = ctx.PartialPlanQueue();
            ITask*               tptr1 = static_cast<ITask*>(task3.get());
            Assert::AreEqual(tptr1, queueCopy.front().Task.get());
            Assert::AreEqual(2, queueCopy.front().TaskIndex);
            queueCopy.pop();
            Assert::AreEqual(static_cast<ITask*>(task.get()), queueCopy.front().Task.get());
            Assert::AreEqual(1, queueCopy.front().TaskIndex);
        }

        TEST_METHOD(ContinueNestedPausePlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask>  task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask>  task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask>  task3 = MakeSharedPtr<Sequence>("Test3"s);
            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task3->AddSubTask(subtask1);
            SharedPtr<PausePlanTask> pause = MakeSharedPtr<PausePlanTask>();
            task3->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(subtask2);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task2->AddSubTask(subtask3);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(subtask4);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Partial);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
            Assert::IsTrue(ctx.HasPausedPartialPlan());
            Assert::IsTrue(ctx.PartialPlanQueue().size() == 2);
            PartialPlanQueueType queueCopy = ctx.PartialPlanQueue();
            ITask*               tptr1 = static_cast<ITask*>(task3.get());
            Assert::AreEqual(tptr1, queueCopy.front().Task.get());
            Assert::AreEqual(2, queueCopy.front().TaskIndex);
            queueCopy.pop();
            Assert::AreEqual(static_cast<ITask*>(task.get()), queueCopy.front().Task.get());
            Assert::AreEqual(1, queueCopy.front().TaskIndex);

            ctx.HasPausedPartialPlan() = false;
            plan = TaskQueueType();
            while (ctx.PartialPlanQueue().size() > 0)
            {
                auto kvp = ctx.PartialPlanQueue().front();
                ctx.PartialPlanQueue().pop();
                TaskQueueType p;
                auto          s = StaticCastPtr<CompoundTask>(kvp.Task)->Decompose(ctx, kvp.TaskIndex, p);

                if (s == DecompositionStatus::Succeeded || s == DecompositionStatus::Partial)
                {
                    while (p.size() > 0)
                    {
                        plan.push(p.front());
                        p.pop();
                    }
                }

                if (ctx.HasPausedPartialPlan())
                    break;
            }

            Assert::IsTrue(plan.size() == 2);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task4"s, plan.front()->Name());
        }

        TEST_METHOD(ContinueMultipleNestedPausePlan_ExpectedBehavior)
        {
            DomainTestContext ctx;
            TaskQueueType     plan;
            ctx.Init();

            SharedPtr<CompoundTask> task = MakeSharedPtr<Sequence>("Test"s);
            SharedPtr<CompoundTask> task2 = MakeSharedPtr<Selector>("Test2"s);
            SharedPtr<CompoundTask> task3 = MakeSharedPtr<Sequence>("Test3"s);
            SharedPtr<CompoundTask> task4 = MakeSharedPtr<Sequence>("Test3"s);

            SharedPtr<PrimitiveTask> subtask1 = MakeSharedPtr<PrimitiveTask>("Sub-task1");
            task3->AddSubTask(subtask1);
            SharedPtr<PausePlanTask> pause = MakeSharedPtr<PausePlanTask>();
            task3->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask2 = MakeSharedPtr<PrimitiveTask>("Sub-task2");
            task3->AddSubTask(subtask2);

            task2->AddSubTask(task3);
            SharedPtr<PrimitiveTask> subtask3 = MakeSharedPtr<PrimitiveTask>("Sub-task3");
            task2->AddSubTask(subtask3);

            SharedPtr<PrimitiveTask> subtask5 = MakeSharedPtr<PrimitiveTask>("Sub-task5");
            task4->AddSubTask(subtask5);
            task4->AddSubTask(pause);
            SharedPtr<PrimitiveTask> subtask6 = MakeSharedPtr<PrimitiveTask>("Sub-task6");
            task4->AddSubTask(subtask6);

            task->AddSubTask(task2);
            SharedPtr<PrimitiveTask> subtask4 = MakeSharedPtr<PrimitiveTask>("Sub-task4");
            task->AddSubTask(subtask4);
            task->AddSubTask(task4);
            SharedPtr<PrimitiveTask> subtask7 = MakeSharedPtr<PrimitiveTask>("Sub-task7");
            task->AddSubTask(subtask7);

            auto status = task->Decompose(ctx, 0, plan);

            Assert::IsTrue(status == DecompositionStatus::Partial);
            Assert::IsTrue(plan.size() == 1);
            Assert::AreEqual("Sub-task1"s, plan.front()->Name());
            Assert::IsTrue(ctx.HasPausedPartialPlan());
            Assert::IsTrue(ctx.PartialPlanQueue().size() == 2);
            PartialPlanQueueType queueCopy = ctx.PartialPlanQueue();
            ITask*               tptr1 = static_cast<ITask*>(task3.get());
            Assert::AreEqual(tptr1, queueCopy.front().Task.get());
            Assert::AreEqual(2, queueCopy.front().TaskIndex);
            queueCopy.pop();
            Assert::AreEqual(static_cast<ITask*>(task.get()), queueCopy.front().Task.get());
            Assert::AreEqual(1, queueCopy.front().TaskIndex);

            ctx.HasPausedPartialPlan() = false;
            plan = TaskQueueType();
            while (ctx.PartialPlanQueue().size() > 0)
            {
                auto kvp = ctx.PartialPlanQueue().front();
                ctx.PartialPlanQueue().pop();
                TaskQueueType p;
                auto          s = StaticCastPtr<CompoundTask>(kvp.Task)->Decompose(ctx, kvp.TaskIndex, p);

                if (s == DecompositionStatus::Succeeded || s == DecompositionStatus::Partial)
                {
                    while (p.size() > 0)
                    {
                        plan.push(p.front());
                        p.pop();
                    }
                }

                if (ctx.HasPausedPartialPlan())
                    break;
            }

            Assert::IsTrue(plan.size() == 3);
            Assert::AreEqual("Sub-task2"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task4"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task5"s, plan.front()->Name());

            ctx.HasPausedPartialPlan() = false;
            plan = TaskQueueType();
            while (ctx.PartialPlanQueue().size() > 0)
            {
                auto kvp = ctx.PartialPlanQueue().front();
                ctx.PartialPlanQueue().pop();
                TaskQueueType p;
                auto          s = StaticCastPtr<CompoundTask>(kvp.Task)->Decompose(ctx, kvp.TaskIndex, p);

                if (s == DecompositionStatus::Succeeded || s == DecompositionStatus::Partial)
                {
                    while (p.size() > 0)
                    {
                        plan.push(p.front());
                        p.pop();
                    }
                }

                if (ctx.HasPausedPartialPlan())
                    break;
            }

            Assert::IsTrue(plan.size() == 2);
            Assert::AreEqual("Sub-task6"s, plan.front()->Name());
            plan.pop();
            Assert::AreEqual("Sub-task7"s, plan.front()->Name());
        }
	};
}
