#include "pch.h"
#include "CppUnitTest.h"
#include "Tasks/CompoundTasks/RandomSelector.h"
#include "CoreIncludes/BaseDomainBuilder.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

namespace FluidHTNCPPUnitTests
{
TEST_CLASS(RandomSelectorTest)
{

    TEST_METHOD(RandomSelect_ExpectedBehavior)
    {
        BaseDomainBuilder builder("tests");
		DomainTestContext ctx;
		ctx.Init();

		builder.AddRandomSelector("random");

		builder.AddAction("get a");
		builder.AddCondition("has not A", [](IContext& ctx) {
			return (static_cast<DomainTestContext&>(ctx).HasStateOneParam(DomainTestState::HasA) == false);
		});
		builder.AddOperator([](IContext&) { return TaskStatus::Success; });
		builder.End();
		builder.AddAction("get b");
		builder.AddCondition("has not B", [](IContext& ctx) {
			return (static_cast<DomainTestContext&>(ctx).HasStateOneParam(DomainTestState::HasB) == false);
		});
		builder.AddOperator([](IContext&) { return TaskStatus::Success; });
		builder.End();
		builder.AddAction("get c");
		builder.AddCondition("has not C", [](IContext& ctx) {
			return (static_cast<DomainTestContext&>(ctx).HasStateOneParam(DomainTestState::HasC) == false);
		});
		builder.AddOperator([](IContext&) { return TaskStatus::Success; });
		builder.End();
		builder.End();
		auto domain = builder.Build();

		int aCount = 0;
		int bCount = 0;
		int cCount = 0;
		for (int i = 0; i < 1000; i++)
		{
			TaskQueueType plan;
			auto          status = domain->FindPlan(ctx, plan);
			Assert::IsTrue(status == DecompositionStatus::Succeeded);
			Assert::IsTrue(plan.size() == 1);

			auto name = plan.front()->Name();
			if (name == "get a"s)
				aCount++;
			if (name == "get b"s)
				bCount++;
			if (name == "get c"s)
				cCount++;

			Assert::IsTrue(name == "get a" || name == "get b" || name == "get c");
            plan = TaskQueueType();
		}

		// With 1000 iterations, the chance of any of these counts being 0 is suuuper slim.
		Assert::IsTrue(aCount > 0 && bCount > 0 && cCount > 0);
	}
};
} // namespace FluidHTNCPPUnitTests
