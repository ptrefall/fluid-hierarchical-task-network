#include "pch.h"
#include "CppUnitTest.h"
#include "Contexts/BaseContext.h"
#include "Effects/Effect.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

class TestContext : public BaseContext<DomainTestState, uint8_t,DomainTestWorldState>
{
	bool _Done = false;
public:
	bool& Done() { return _Done; }
};

namespace Microsoft::VisualStudio::CppUnitTestFramework
{
	template<>
	std::wstring ToString<EffectType>(const EffectType& eff)
	{
		switch(eff)
		{
		case EffectType::Permanent:
			return L"EffectType::Permanent";
		case EffectType::PlanOnly:
			return L"EffectType::PlanOnly";
		case EffectType::PlanAndExecute:
			return L"EffectType::PlanAndExecute";
		}
		return L"Unknown value";
	}
}
namespace FluidHTNCPPUnitTests
{
	TEST_CLASS(ActionEffectTests)
	{
	public:

		TEST_METHOD(SetsName_ExpectedBehavior)
		{
			ActionEffect a("Name", EffectType::PlanOnly, nullptr);

			Assert::AreEqual("Name"s, a.Name());
		}
		TEST_METHOD(SetsType_ExpectedBehavior)
		{
			 ActionEffect e("Name", EffectType::PlanOnly, nullptr);

			Assert::AreEqual(EffectType::PlanOnly, e.Type());
		}

		TEST_METHOD(ApplyDoesNothingWithoutFunctionPtr_ExpectedBehavior)
		{
			TestContext ctx;
			ActionEffect e("Name", EffectType::PlanOnly, nullptr);

			e.Apply(ctx);
		}

		TEST_METHOD(ApplyCallsInternalFunctionPtr_ExpectedBehavior)
		{
			TestContext ctx;
			ActionEffect e("Name", EffectType::PlanOnly, [=](IContext& c, EffectType ) {static_cast<TestContext&>(c).Done() = true; });

			e.Apply(ctx);

			Assert::AreEqual(true, ctx.Done());
		}
	};
}
