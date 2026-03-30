#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Effects/Effect.h"
#include "Conditions/Condition.h"
#include "Operators/Operator.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(PrimitiveTaskTests)
    {
        TEST_METHOD(AddCondition_ExpectedBehavior)
        {
            auto                        task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            bool bRet = task->AddCondition(c);

            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Conditions().size() == 1);
        }
        TEST_METHOD(AddExecutingCondition_ExpectedBehavior)
        {
            auto                        task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });

            bool bRet = task->AddExecutingCondition(c);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->ExecutingConditions().size() == 1);
        }

        TEST_METHOD(AddEffect_ExpectedBehavior)
        {
            auto                     task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<IEffect> e = MakeSharedPtr<ActionEffect>("Name"s, EffectType::Permanent , [](IContext& ctx, EffectType eff) {
                (void)eff;
                static_cast<DomainTestContext&>(ctx).Done() = true;
            });

            bool bRet = task->AddEffect(e);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Effects().size() == 1);
        }
        TEST_METHOD(SetOperator_ExpectedBehavior)
        {
            auto                       task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<IOperator> o = MakeSharedPtr<FuncOperator>(nullptr, nullptr);

            task->SetOperator(o);

            Assert::IsTrue(task->Operator() != nullptr);
        }

        TEST_METHOD(SetOperatorThrowsExceptionIfAlreadySet_ExpectedBehavior)
        {
            auto                       task = MakeSharedPtr<PrimitiveTask>("Test"s);

            SharedPtr<IOperator> o = MakeSharedPtr<FuncOperator>(nullptr, nullptr);

            task->SetOperator(o);

            Assert::ExpectException<std::exception>([=]() {
                SharedPtr<IOperator> o2 = MakeSharedPtr<FuncOperator>(nullptr, nullptr);
                task->SetOperator(o2);
            });
        }

        TEST_METHOD(ApplyEffects_ExpectedBehavior)
        {
            DomainTestContext              ctx;
            auto                     task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<IEffect> e = MakeSharedPtr<ActionEffect>("Name"s, EffectType::Permanent, [](IContext& ctx, EffectType e) {
                (void)e;
                static_cast<DomainTestContext&>(ctx).Done() = true;
            });

            task->AddEffect(e);
            task->ApplyEffects(ctx);

            Assert::AreEqual(true, ctx.Done());
        }
        TEST_METHOD(StopWithValidOperator_ExpectedBehavior)
        {
            DomainTestContext                ctx;
            auto                       task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<IOperator> o =
                MakeSharedPtr<FuncOperator>(nullptr, [](IContext& ctx) { static_cast<DomainTestContext&>(ctx).Done() = true; });

            task->SetOperator(o);
            task->Stop(ctx);

            Assert::IsTrue(task->Operator() != nullptr);
            Assert::AreEqual(true, ctx.Done());
        }

        TEST_METHOD(StopWithNullOperator_ExpectedBehavior)
        {
            DomainTestContext                ctx;
            auto                       task = MakeSharedPtr<PrimitiveTask>("Test"s);
            task->Stop(ctx);
        }
        TEST_METHOD( IsValid_ExpectedBehavior)
        {
            DomainTestContext                ctx;
            auto                       task = MakeSharedPtr<PrimitiveTask>("Test"s);
            SharedPtr<ICondition> c = MakeSharedPtr<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == false);
            });
            SharedPtr<ICondition> c2 = MakeSharedPtr<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<DomainTestContext&>(ctx).Done() == true);
            });

            task->AddCondition(c);
            bool expectTrue = task->IsValid(ctx);

            task->AddCondition(c2);
            bool expectFalse = task->IsValid(ctx);

            Assert::IsTrue(expectTrue);
            Assert::IsFalse(expectFalse);
        }
    };
}
