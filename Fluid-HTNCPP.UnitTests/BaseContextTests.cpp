
#include "pch.h"
#include "CppUnitTest.h"
#include "Effects/EffectType.h"
#include "Contexts/BaseContext.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std::string_literals;

using namespace FluidHTN;

namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(BaseContextTests)
    {
        TEST_METHOD( DefaultContextStateIsExecuting_ExpectedBehavior)
        {
            DomainTestContext ctx;
            Assert::IsTrue(ctx.GetContextState() == ContextState::Executing);
        }
        TEST_METHOD(InitInitializeCollections_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();

            Assert::AreEqual(false, ctx.DebugMTR());
            Assert::AreEqual(false, ctx.LogDecomposition());
            Assert::AreEqual(true, ctx.MTRDebug().size() == 0);
            Assert::AreEqual(true, ctx.LastMTRDebug().size() == 0);
            Assert::AreEqual(true, ctx.DecompositionLog().size() == 0);
        }
        TEST_METHOD(InitInitializeDebugCollections_ExpectedBehavior)
        {
            MyDebugContext ctx;

            ctx.Init();

            Assert::AreEqual(true, ctx.DebugMTR());
            Assert::AreEqual(true, ctx.LogDecomposition());
        }
        TEST_METHOD(HasState_ExpectedBehavior)
        {
            DomainTestContext ctx;
            ctx.Init();
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);

            Assert::AreEqual(false, ctx.HasStateOneParam(DomainTestState::HasA));
            Assert::AreEqual(true, ctx.HasStateOneParam(DomainTestState::HasB));
        }
        TEST_METHOD(SetStatePlanningContext_ExpectedBehavior)
        {
            DomainTestContext ctx;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);

            Assert::AreEqual(true, (bool)ctx.GetStateDTS(DomainTestState::HasB));
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasA].size() == 0);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].top().First() == EffectType::Permanent);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].top().Second() == 1);
            Assert::IsTrue(ctx.GetWorldState().GetState(DomainTestState::HasB) == 0);
        }
        TEST_METHOD(SetStateExecutingContext_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Executing);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);

            Assert::AreEqual(true, ctx.HasStateOneParam(DomainTestState::HasB));
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].size() == 0);
            Assert::IsTrue(ctx.GetWorldState().GetState( DomainTestState::HasB) == 1);
        }

        TEST_METHOD(GetStatePlanningContext_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);

            Assert::AreEqual(0,(int) ctx.GetStateDTS(DomainTestState::HasA));
            Assert::AreEqual(1, (int)ctx.GetStateDTS(DomainTestState::HasB));
        }

        TEST_METHOD(GetStateExecutingContext_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Executing);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);

            Assert::AreEqual(0,(int) ctx.GetStateDTS(DomainTestState::HasA));
            Assert::AreEqual(1, (int)ctx.GetStateDTS(DomainTestState::HasB));
        }

        TEST_METHOD(GetWorldStateChangeDepth_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Executing);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);
            auto changeDepthExecuting = ctx.GetWorldStateChangeDepth();

            ctx.SetContextState(ContextState::Planning);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);
            auto changeDepthPlanning = ctx.GetWorldStateChangeDepth();

            Assert::AreEqual(ctx.GetWorldStateChangeStack().size(), changeDepthExecuting.size());
            Assert::AreEqual(0, changeDepthExecuting[(int) DomainTestState::HasA]);
            Assert::AreEqual(0, changeDepthExecuting[(int) DomainTestState::HasB]);

            Assert::AreEqual(ctx.GetWorldStateChangeStack().size(), changeDepthPlanning.size());
            Assert::AreEqual(0, changeDepthPlanning[(int) DomainTestState::HasA]);
            Assert::AreEqual(1, changeDepthPlanning[(int) DomainTestState::HasB]);
        }

        TEST_METHOD(TrimForExecution_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetStateDTS(DomainTestState::HasA, true,true, EffectType::PlanAndExecute);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);
            ctx.SetStateDTS(DomainTestState::HasC, true,true, EffectType::PlanOnly);
            ctx.TrimForExecution();

            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasA].size() == 0);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasC].size() == 0);
        }

        TEST_METHOD(TrimForExecutionThrowsExceptionIfWrongContextState_ExpectedBehavior)
        {
            DomainTestContext ctx;

            ctx.Init();
            ctx.SetContextState(ContextState::Executing);
            Assert::ExpectException<std::exception>([&]() { ctx.TrimForExecution(); });
        }
        TEST_METHOD(TrimToStackDepth_ExpectedBehavior)
        {
            DomainTestContext ctx;
            ctx.Init();
            ctx.SetContextState(ContextState::Planning);
            ctx.SetStateDTS(DomainTestState::HasA, true,true, EffectType::PlanAndExecute);
            ctx.SetStateDTS(DomainTestState::HasB, true,true, EffectType::Permanent);
            ctx.SetStateDTS(DomainTestState::HasC, true,true, EffectType::PlanOnly);
            auto stackDepth = ctx.GetWorldStateChangeDepth();

            ctx.SetStateDTS(DomainTestState::HasA, false, true, EffectType::PlanAndExecute);
            ctx.SetStateDTS(DomainTestState::HasB, false, true, EffectType::Permanent);
            ctx.SetStateDTS(DomainTestState::HasC, false, true, EffectType::PlanOnly);
            ctx.TrimToStackDepth(stackDepth);

            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasA].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasB].size() == 1);
            Assert::IsTrue(ctx.GetWorldStateChangeStack()[(int) DomainTestState::HasC].size() == 1);
        }

        TEST_METHOD(TrimToStackDepthThrowsExceptionIfWrongContextState_ExpectedBehavior)
        {
            DomainTestContext ctx;
            ctx.Init();
            ctx.SetContextState(ContextState::Executing);
            auto stackDepth = ctx.GetWorldStateChangeDepth();
            Assert::ExpectException<std::exception>([&]() { ctx.TrimToStackDepth(stackDepth); });
        }
        } ;
}
