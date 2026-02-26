#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Conditions/Condition.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;


namespace FluidHTNCPPUnitTests
{
TEST_CLASS(FuncConditionTests)
{
    TEST_METHOD(SetsName_ExpectedBehavior)
    {
        auto c = MakeSharedPtr<FuncCondition>("Name"s, nullptr);

        Assert::AreEqual("Name"s, c->Name());
    }

    TEST_METHOD(IsValidFailsWithoutFunctionPtr_ExpectedBehavior)
    {
        auto ctx = MakeSharedPtr<DomainTestContext>();
        auto c = MakeSharedPtr<FuncCondition>("Name"s, nullptr);

        auto result = c->IsValid(*ctx);

        Assert::AreEqual(false, result);
    }

    TEST_METHOD(IsValidCallsInternalFunctionPtr_ExpectedBehavior)
    {
        DomainTestContext ctx;
        auto        c = MakeSharedPtr<FuncCondition>("Name"s,
                                                 [](IContext& ctx) { return (static_cast<DomainTestContext&>(ctx).Done() == false); });
        auto        result = c->IsValid(ctx);

        Assert::AreEqual(true, result);
    }
} ;
} // namespace FluidHTNCPPUnitTests
