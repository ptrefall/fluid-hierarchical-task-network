#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Domain.h"
#include "Tasks/Task.h"
#include "Tasks/CompoundTasks/CompoundTask.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "Tasks/CompoundTasks/PausePlanTask.h"
#include "Tasks/CompoundTasks/Selector.h"
#include "Tasks/CompoundTasks/Sequence.h"
#include "Tasks/CompoundTasks/DecompositionStatus.h"
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
TEST_CLASS(DomainTests)
{
    TEST_METHOD(DomainHasRootWithDomainName_ExpectedBehavior)
    {
        Domain domain("Test");
        Assert::IsTrue(domain.Root() != nullptr);
        Assert::IsTrue(domain.Root()->Name() == "Test"s);
    }
    TEST_METHOD(AddSubtaskToParent_ExpectedBehavior)
    {
        Domain                        domain("Test");
        std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test");
        std::shared_ptr<ITask> task2 = std::make_shared<Selector>("Test2");
        domain.Add(task1, task2);
        Assert::IsTrue(std::find(task1->Subtasks().begin(), task1->Subtasks().end(), task2) != task1->Subtasks().end());
        Assert::IsTrue(task2->Parent().get() == task1.get());
    }
    TEST_METHOD(FindPlanUninitializedContextThrowsException_ExpectedBehavior)
    {
        auto                      domain = std::make_shared<Domain>("Test");
        std::shared_ptr<IContext> ctx = std::make_shared<DomainTestContext>();

        Assert::ExpectException<std::exception>([=]() -> DecompositionStatus {
            TaskQueueType plan;
            return domain->FindPlan(*ctx, plan);
        });
    }
    TEST_METHOD(FindPlanNoTasksThenNullPlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        auto status = domain.FindPlan(*ctx, plan);
        Assert::IsTrue(status == DecompositionStatus::Rejected);
        Assert::IsTrue(plan.size() == 0);
    }
    TEST_METHOD(AfterFindPlanContextStateIsExecuting_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        domain.FindPlan(*ctx, plan);
        Assert::IsTrue(ctx->GetContextState() == ContextState::Executing);
    }
    TEST_METHOD(FindPlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;

        bctx->Init();

        std::shared_ptr<CompoundTask> task1 = std::make_shared<Selector>("Test");

        std::shared_ptr<ITask> task2 = std::make_shared<PrimitiveTask>("Sub-task");

        domain.Add(domain.Root(), task1);
        domain.Add(task1, task2);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Succeeded);
        Assert::IsTrue(plan.size() == 1);
        Assert::IsTrue(plan.front()->Name() == "Sub-task"s);
    }
    TEST_METHOD(FindPlanTrimsNonPermanentStateChange_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        std::shared_ptr<CompoundTask> task1 = std::make_shared<Sequence>("Test");

        std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<IEffect>       effect1 =
            std::make_shared<ActionEffect>("TestEffect1"s, EffectType::PlanOnly, [=](IContext& ctx, EffectType t) {
                ctx.SetState((int)DomainTestState::HasA, true, true, t);
            });
        task2->AddEffect(effect1);

        std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");
        std::shared_ptr<IEffect> effect2 = std::make_shared<ActionEffect>(
            "TestEffect2"s,
            EffectType::PlanAndExecute,
            [=](IContext& ctx, EffectType t) { ctx.SetState((int)DomainTestState::HasB, true, true, t); });
        task3->AddEffect(effect2);

        std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task3");
        std::shared_ptr<IEffect> effect3 =
            std::make_shared<ActionEffect>("TestEffect3"s, EffectType::Permanent, [=](IContext& ctx, EffectType t) {
                ctx.SetState((int)DomainTestState::HasC, true, true, t);
            });
        task4->AddEffect(effect3);
        domain.Add(domain.Root(), task1);
        domain.Add(task1, task2);
        domain.Add(task1, task3);
        domain.Add(task1, task4);
        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Succeeded);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasA].size() == 0);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasB].size() == 0);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasC].size() == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasA) == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasB) == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasC) == 1);
        Assert::IsTrue(plan.size() == 3);
    }

    TEST_METHOD(FindPlanClearsStateChangeWhenPlanIsNull_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        std::shared_ptr<CompoundTask> task1 = std::make_shared<Sequence>("Test");
        std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<IEffect> effect1 =
            std::make_shared<ActionEffect>("TestEffect1"s, EffectType::PlanOnly, [=](IContext& ctx, EffectType t) {
                ctx.SetState((int)DomainTestState::HasA, true, true, t);
            });
        task2->AddEffect(effect1);

        std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");
        std::shared_ptr<IEffect> effect2 = std::make_shared<ActionEffect>(
            "TestEffect2"s,
            EffectType::PlanAndExecute,
            [=](IContext& ctx, EffectType t) { ctx.SetState((int)DomainTestState::HasB, true, true, t); });
        task3->AddEffect(effect2);

        std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task3");
        std::shared_ptr<IEffect> effect3 =
            std::make_shared<ActionEffect>("TestEffect3"s, EffectType::Permanent, [=](IContext& ctx, EffectType t) {
                ctx.SetState((int)DomainTestState::HasC, true, true, t);
            });
        task4->AddEffect(effect3);

        std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task4");
        std::shared_ptr<ICondition> condition = std::make_shared<FuncCondition>("TestCondition"s, [=](IContext& ctx) {
            DomainTestContext& d = (DomainTestContext&)ctx;
            return (d.Done() == true);
        });
        task5->AddCondition(condition);

        domain.Add(domain.Root(), task1);
        domain.Add(task1, task2);
        domain.Add(task1, task3);
        domain.Add(task1, task4);
        domain.Add(task1, task5);
        auto status = domain.FindPlan(*ctx, plan);
        Assert::IsTrue(status == DecompositionStatus::Rejected);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasA].size() == 0);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasB].size() == 0);
        Assert::IsTrue(ctx->GetWorldStateChangeStack()[(int)DomainTestState::HasC].size() == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasA) == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasB) == 0);
        Assert::IsTrue(ctx->GetWorldState().GetState((int)DomainTestState::HasC) == 0);
        Assert::IsTrue(plan.size() == 0);
    }
    TEST_METHOD(FindPlanIfMTRsAreEqualThenReturnNullPlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        ctx->LastMTR().push_back(1);

        // Root is a Selector that branch off into task1 selector or task2 sequence.
        // MTR only tracks decomposition of compound tasks, so our MTR is only 1 layer deep here,
        // Since both compound tasks decompose into primitive tasks.
        std::shared_ptr<CompoundTask> task1 = std::make_shared<Sequence>("Test1");
        std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2");

        std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<ICondition> condition = std::make_shared<FuncCondition>("TestCondition"s, [=](IContext& ctx) {
            DomainTestContext& d = (DomainTestContext&)ctx;
            return (d.Done() == true);
        });
        task3->AddCondition(condition);

        std::shared_ptr<PrimitiveTask> task4 = std::make_shared<PrimitiveTask>("Sub-task1");

        std::shared_ptr<PrimitiveTask> task5 = std::make_shared<PrimitiveTask>("Sub-task2");
        std::shared_ptr<ICondition> condition2 = std::make_shared<FuncCondition>("TestCondition"s, [=](IContext& ctx) {
            DomainTestContext& d = (DomainTestContext&)ctx;
            return (d.Done() == true);
        });
        task5->AddCondition(condition);

        domain.Add(domain.Root(), task1);
        domain.Add(domain.Root(), task2);
        domain.Add(task1, task3);
        domain.Add(task2, task4);
        domain.Add(task2, task5);
        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Rejected);
        Assert::IsTrue(plan.size() == 0);
        Assert::IsTrue(ctx->MethodTraversalRecord().size() == 1);
        Assert::IsTrue(ctx->MethodTraversalRecord()[0] == ctx->LastMTR()[0]);
    }

    TEST_METHOD(PausePlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();

        std::shared_ptr<CompoundTask> task1 = std::make_shared<Sequence>("Test1");
        std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

        std::shared_ptr<ITask> task4 = std::make_shared<PausePlanTask>();

        domain.Add(domain.Root(), task1);
        domain.Add(task1, task2);
        domain.Add(task1, task4);
        domain.Add(task1, task3);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        Assert::IsTrue(ctx->HasPausedPartialPlan());
        Assert::IsTrue(ctx->PartialPlanQueue().size() == 1);
        auto   tx = std::static_pointer_cast<ITask>(task1);
        ITask* t1ptr = tx.get();
        ITask* t2ptr = ctx->PartialPlanQueue().front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(2, ctx->PartialPlanQueue().front().TaskIndex);
    }

    TEST_METHOD(ContinuePausedPlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();

        std::shared_ptr<CompoundTask> task1 = std::make_shared<Sequence>("Test1");
        std::shared_ptr<PrimitiveTask> task2 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<PrimitiveTask> task3 = std::make_shared<PrimitiveTask>("Sub-task2");

        std::shared_ptr<ITask> task4 = std::make_shared<PausePlanTask>();

        domain.Add(domain.Root(), task1);
        domain.Add(task1, task2);
        domain.Add(task1, task4);
        domain.Add(task1, task3);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        Assert::IsTrue(ctx->HasPausedPartialPlan());
        Assert::IsTrue(ctx->PartialPlanQueue().size() == 1);
        auto   tx = std::static_pointer_cast<ITask>(task1);
        ITask* t1ptr = tx.get();
        ITask* t2ptr = ctx->PartialPlanQueue().front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(2, ctx->PartialPlanQueue().front().TaskIndex);

        status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Succeeded);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task2"s, plan.front()->Name());
    }

    TEST_METHOD(NestedPausePlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();
        std::shared_ptr<CompoundTask> task = std::make_shared<Sequence>("Test1");
        std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2");
        std::shared_ptr<CompoundTask> task3 = std::make_shared<Sequence>("Test3");

        std::shared_ptr<PrimitiveTask> subtask4 = std::make_shared<PrimitiveTask>("Sub-task4");
        std::shared_ptr<PrimitiveTask> subtask3 = std::make_shared<PrimitiveTask>("Sub-task3");
        std::shared_ptr<PrimitiveTask> subtask2 = std::make_shared<PrimitiveTask>("Sub-task2");
        std::shared_ptr<PrimitiveTask> subtask1 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<ITask> pausePlan = std::make_shared<PausePlanTask>();

        domain.Add(domain.Root(), task);
        domain.Add(task, task2);
        domain.Add(task, subtask4);

        domain.Add(task2, task3);
        domain.Add(task2, subtask3);

        domain.Add(task3, subtask1);
        domain.Add(task3, pausePlan);
        domain.Add(task3, subtask2);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        Assert::IsTrue(ctx->HasPausedPartialPlan());
        Assert::IsTrue(ctx->PartialPlanQueue().size() == 2);

        auto   theQueue = ctx->PartialPlanQueue();

        ITask* t1ptr = task3.get();
        ITask* t2ptr = theQueue.front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(2, theQueue.front().TaskIndex);

        theQueue.pop();
        t1ptr = task.get();
        t2ptr = theQueue.front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(1, theQueue.front().TaskIndex);
    }
    TEST_METHOD(ContinueNestedPausePlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();

        std::shared_ptr<CompoundTask> task = std::make_shared<Sequence>("Test1");
        std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2");
        std::shared_ptr<CompoundTask> task3 = std::make_shared<Sequence>("Test3");

        std::shared_ptr<PrimitiveTask> subtask4 = std::make_shared<PrimitiveTask>("Sub-task4");
        std::shared_ptr<PrimitiveTask> subtask3 = std::make_shared<PrimitiveTask>("Sub-task3");
        std::shared_ptr<PrimitiveTask> subtask2 = std::make_shared<PrimitiveTask>("Sub-task2");
        std::shared_ptr<PrimitiveTask> subtask1 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<ITask>         pausePlan = std::make_shared<PausePlanTask>();

        domain.Add(domain.Root(), task);
        domain.Add(task, task2);
        domain.Add(task, subtask4);

        domain.Add(task2, task3);
        domain.Add(task2, subtask3);

        domain.Add(task3, subtask1);
        domain.Add(task3, pausePlan);
        domain.Add(task3, subtask2);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        Assert::IsTrue(ctx->HasPausedPartialPlan());
        Assert::IsTrue(ctx->PartialPlanQueue().size() == 2);

        PartialPlanQueueType queueCopy = ctx->PartialPlanQueue();
        ITask*               t1ptr = task3.get();
        ITask*               t2ptr = queueCopy.front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(2, queueCopy.front().TaskIndex);

        queueCopy.pop();
        t1ptr = task.get();
        t2ptr = queueCopy.front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(1, queueCopy.front().TaskIndex);

        status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Succeeded);
        Assert::IsTrue(plan.size() == 2);
        Assert::AreEqual("Sub-task2"s, plan.front()->Name());
        plan.pop();
        Assert::AreEqual("Sub-task4"s, plan.front()->Name());
    }
    TEST_METHOD(ContinueMultipleNestedPausePlan_ExpectedBehavior)
    {
        auto                      bctx = std::make_shared<DomainTestContext>();
        std::shared_ptr<IContext> ctx = std::static_pointer_cast<IContext>(bctx);
        Domain                    domain("Test");
        TaskQueueType             plan;
        bctx->Init();

        std::shared_ptr<CompoundTask> task = std::make_shared<Sequence>("Test1");
        std::shared_ptr<CompoundTask> task2 = std::make_shared<Selector>("Test2");
        std::shared_ptr<CompoundTask> task3 = std::make_shared<Sequence>("Test3");
        std::shared_ptr<CompoundTask> task4 = std::make_shared<Sequence>("Test4");

        domain.Add(domain.Root(), task);
        std::shared_ptr<PrimitiveTask> subtask1 = std::make_shared<PrimitiveTask>("Sub-task1");
        std::shared_ptr<ITask>         pausePlan1 = std::make_shared<PausePlanTask>();
        std::shared_ptr<PrimitiveTask> subtask2 = std::make_shared<PrimitiveTask>("Sub-task2");
        domain.Add(task3, subtask1);
        domain.Add(task3, pausePlan1);
        domain.Add(task3, subtask2);

        std::shared_ptr<PrimitiveTask> subtask3 = std::make_shared<PrimitiveTask>("Sub-task3");
        domain.Add(task2, task3);
        domain.Add(task2, subtask3);

        std::shared_ptr<PrimitiveTask> subtask5 = std::make_shared<PrimitiveTask>("Sub-task5");
        std::shared_ptr<ITask>         pausePlan2 = std::make_shared<PausePlanTask>();
        std::shared_ptr<PrimitiveTask> subtask6 = std::make_shared<PrimitiveTask>("Sub-task6");
        domain.Add(task4, subtask5);
        domain.Add(task4, pausePlan2);
        domain.Add(task4, subtask6);

        domain.Add(task, task2);
        std::shared_ptr<PrimitiveTask> subtask4 = std::make_shared<PrimitiveTask>("Sub-task4");
        domain.Add(task, subtask4);
        domain.Add(task, task4);
        std::shared_ptr<PrimitiveTask> subtask7 = std::make_shared<PrimitiveTask>("Sub-task7");
        domain.Add(task, subtask7);

        auto status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 1);
        Assert::AreEqual("Sub-task1"s, plan.front()->Name());
        Assert::IsTrue(ctx->HasPausedPartialPlan());
        Assert::IsTrue(ctx->PartialPlanQueue().size() == 2);

        PartialPlanQueueType queueCopy = ctx->PartialPlanQueue();

        ITask* t1ptr = task3.get();
        ITask* t2ptr = queueCopy.front().Task.get();
        Assert::AreEqual(t1ptr, t2ptr);
        Assert::AreEqual(2, queueCopy.front().TaskIndex);
        queueCopy.pop();
        t1ptr = task.get();
        Assert::AreEqual(t1ptr, queueCopy.front().Task.get());
        Assert::AreEqual(1, queueCopy.front().TaskIndex);

        status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Partial);
        Assert::IsTrue(plan.size() == 3);
        Assert::AreEqual("Sub-task2"s, plan.front()->Name());
        plan.pop();
        Assert::AreEqual("Sub-task4"s, plan.front()->Name());
        plan.pop();
        Assert::AreEqual("Sub-task5"s, plan.front()->Name());

        status = domain.FindPlan(*ctx, plan);

        Assert::IsTrue(status == DecompositionStatus::Succeeded);
        Assert::IsTrue(plan.size() == 2);
        Assert::AreEqual("Sub-task6"s, plan.front()->Name());
        plan.pop();
        Assert::AreEqual("Sub-task7"s, plan.front()->Name());
    }
    };
} // namespace FluidHTNCPPUnitTests