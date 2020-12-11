#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Operators/Operator.h"

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
TEST_CLASS(FuncOperatorTests)
{
    TEST_METHOD(UpdateDoesNothingWithoutFunctionPtr_ExpectedBehavior)
    {
        TestContext ctx;
		auto e = MakeSharedPtr<FuncOperator>(nullptr, nullptr);
		e->Update(ctx);
	} 

TEST_METHOD(StopDoesNothingWithoutFunctionPtr_ExpectedBehavior)
{
    TestContext ctx;
    auto        e = MakeSharedPtr<FuncOperator>(nullptr, nullptr);

    e->Stop(ctx);
}
TEST_METHOD(UpdateReturnsStatusInternalFunctionPtr_ExpectedBehavior)
{
    TestContext ctx;
    auto        e = MakeSharedPtr<FuncOperator>([=](IContext&) { return TaskStatus::Success; }, nullptr);

    auto status = e->Update(ctx);

    Assert::AreEqual((int)TaskStatus::Success, (int)status);
}

TEST_METHOD(StopCallsInternalFunctionPtr_ExpectedBehavior)
{
    TestContext ctx;
    auto e = MakeSharedPtr<FuncOperator>(nullptr, [](IContext&ctx ) { static_cast<TestContext&>(ctx).Done() = true; });

    e->Stop(ctx);

    Assert::AreEqual(true, ctx.Done());
}
}
;
} // namespace FluidHTNCPPUnitTests
