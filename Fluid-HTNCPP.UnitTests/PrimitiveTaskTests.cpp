#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Effects/Effect.h"
#include "Conditions/Condition.h"
#include "Operators/Operator.h"
#include "Tasks/PrimitiveTasks/PrimitiveTask.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;


class TestContext : public BaseContext
{
	bool _Done = false;
public:
	bool& Done() { return _Done; }
};

namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(PrimitiveTaskTests)
    {
        TEST_METHOD(AddCondition_ExpectedBehavior)
        {
            auto                        task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<TestContext&>(ctx).Done() == false);
            });
            bool bRet = task->AddCondition(c);

            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Conditions().size() == 1);
        }
        TEST_METHOD(AddExecutingCondition_ExpectedBehavior)
        {
            auto                        task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<TestContext&>(ctx).Done() == false);
            });

            bool bRet = task->AddExecutingCondition(c);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->ExecutingConditions().size() == 1);
        }

        TEST_METHOD(AddEffect_ExpectedBehavior)
        {
            auto                     task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<IEffect> e = std::make_shared<ActionEffect>("Name"s, EffectType::Permanent , [](IContext& ctx, EffectType eff) {
                (void)eff;
                static_cast<TestContext&>(ctx).Done() = true;
            });

            bool bRet = task->AddEffect(e);
            Assert::IsTrue(bRet);
            Assert::IsTrue(task->Effects().size() == 1);
        }
        TEST_METHOD(SetOperator_ExpectedBehavior)
        {
            auto                       task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<IOperator> o = std::make_shared<FuncOperator>(nullptr, nullptr);

            task->SetOperator(o);

            Assert::IsTrue(task->Operator() != nullptr);
        }

        TEST_METHOD(SetOperatorThrowsExceptionIfAlreadySet_ExpectedBehavior)
        {
            auto                       task = std::make_shared<PrimitiveTask>("Test"s);

            std::shared_ptr<IOperator> o = std::make_shared<FuncOperator>(nullptr, nullptr);

            task->SetOperator(o);

            Assert::ExpectException<std::exception>([=]() {
                std::shared_ptr<IOperator> o2 = std::make_shared<FuncOperator>(nullptr, nullptr);
                task->SetOperator(o2);
            });
        }

        TEST_METHOD(ApplyEffects_ExpectedBehavior)
        {
            TestContext              ctx;
            auto                     task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<IEffect> e = std::make_shared<ActionEffect>("Name"s, EffectType::Permanent, [](IContext& ctx, EffectType e) {
                (void)e;
                static_cast<TestContext&>(ctx).Done() = true;
            });

            task->AddEffect(e);
            task->ApplyEffects(ctx);

            Assert::AreEqual(true, ctx.Done());
        }
        TEST_METHOD(StopWithValidOperator_ExpectedBehavior)
        {
            TestContext                ctx;
            auto                       task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<IOperator> o =
                std::make_shared<FuncOperator>(nullptr, [](IContext& ctx) { static_cast<TestContext&>(ctx).Done() = true; });

            task->SetOperator(o);
            task->Stop(ctx);

            Assert::IsTrue(task->Operator() != nullptr);
            Assert::AreEqual(true, ctx.Done());
        }

        TEST_METHOD(StopWithNullOperator_ExpectedBehavior)
        {
            TestContext                ctx;
            auto                       task = std::make_shared<PrimitiveTask>("Test"s);
            task->Stop(ctx);
        }
        TEST_METHOD( IsValid_ExpectedBehavior)
        {
            TestContext                ctx;
            auto                       task = std::make_shared<PrimitiveTask>("Test"s);
            std::shared_ptr<ICondition> c = std::make_shared<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<TestContext&>(ctx).Done() == false);
            });
            std::shared_ptr<ICondition> c2 = std::make_shared<FuncCondition>("Name"s, [](IContext& ctx) {
                return (static_cast<TestContext&>(ctx).Done() == true);
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
