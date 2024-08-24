#include "pch.h"
#include "CppUnitTest.h"
#include "CoreIncludes/BaseDomainBuilder.h"
#include "DomainTestContext.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

using namespace FluidHTN;

class DomainBuilder final : public BaseDomainBuilder 
    {
public:
    DomainBuilder(StringType n): BaseDomainBuilder(n){}
};
namespace FluidHTNCPPUnitTests
{
    TEST_CLASS(DomainBuilderTests)
    {
        TEST_METHOD(Build_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            auto ptr = builder.Pointer();
            auto domain = *(builder.Build());

            Assert::IsTrue(domain.Root() != nullptr);
            Assert::IsTrue(ptr == domain.Root());
            Assert::AreEqual("Test"s, domain.Root()->Name());
        }

        TEST_METHOD(BuildInvalidatesPointer_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            auto domain = *builder.Build();

            Assert::ExpectException<std::exception>([&]() {
                bool bRet = (builder.Pointer() == domain.Root());
                bRet;
            });
        }

        TEST_METHOD(Selector_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSelector("select test");
            builder.End();

            // Assert
            Assert::AreEqual(true,  builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Selector_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSelector("select test");

            // Assert
            Assert::AreEqual(false, builder.Pointer()->IsTypeOf( ITaskDerivedClassName::TaskRoot));
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SelectorCompoundTask));
        }

        TEST_METHOD(SelectorBuild_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSelector("select test");
            Assert::ExpectException<std::exception>([&]() { auto domain = builder.Build(); });
        }
        TEST_METHOD(Selector_CompoundTask)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            SharedPtr<CompoundTask> ctask = MakeSharedPtr<Selector>("compound task");
            builder.AddCompoundTask("compound task",ctask);

            // Assert
            Assert::AreEqual(false, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot)); 
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SelectorCompoundTask));
        }
        TEST_METHOD(Sequence_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSequence("Sequence test");
            builder.End();

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Sequence_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSequence("Sequence test");

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SequenceCompoundTask));
        }

        TEST_METHOD(Sequence_CompoundTask)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            SharedPtr<CompoundTask> ctask = MakeSharedPtr<Sequence>("sequence task");
            builder.AddCompoundTask("compound task",ctask);

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SequenceCompoundTask));
        }

        TEST_METHOD(Action_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("sequence test");
            builder.End();

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Action_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("sequence test");

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask));
        }
        TEST_METHOD(Action_PrimitiveTask)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddPrimitiveTask("sequence test");

            // Assert
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask));
        }

        TEST_METHOD(PausePlanThrowsWhenPointerIsNotDecomposeAll)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            Assert::ExpectException<std::exception>([&]() { builder.PausePlan(); });
        }

        TEST_METHOD(PausePlan_ExpectedBehaviour)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSequence("sequence test");
            builder.PausePlan();
            builder.End();

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }
        TEST_METHOD(PausePlan_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSequence("sequence test");
            builder.PausePlan();

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SequenceCompoundTask));
        }

        TEST_METHOD(Condition_ExpectedBehaviour)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddCondition("test", [](IContext&) { return true; });

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(ExecutingCondition_ThrowsIfNotPrimitiveTaskPointer)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            Assert::ExpectException<std::exception>(
                [&]() { builder.AddExecutingCondition("test", [](IContext&) { return true; }); });
        }
        TEST_METHOD(ExecutingCondition_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddExecutingCondition("test",  [](IContext&) { return true; });
            builder.End();

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(ExecutingCondition_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddExecutingCondition("test",  [](IContext&) { return true; });

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask));
        }

        TEST_METHOD(Do_ThrowsIfNotPrimitiveTaskPointer)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            Assert::ExpectException<std::exception>(
                [&]() {
                    builder.AddOperator([](IContext&) -> TaskStatus { return TaskStatus::Success; }); 
                });
        }

        TEST_METHOD(Do_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddOperator([](IContext&) -> TaskStatus { return TaskStatus::Success; }); 
            builder.End();

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Do_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddOperator([](IContext&) -> TaskStatus { return TaskStatus::Success; }); 

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask));
        }

        TEST_METHOD(Effect_ThrowsIfNotPrimitiveTaskPointer)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            Assert::ExpectException<std::exception>([&]() { builder.AddEffect("test", EffectType::Permanent, [](IContext&, EffectType){}); });
        }

        TEST_METHOD( Effect_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddEffect("test", EffectType::Permanent, [](IContext&, EffectType){});
            builder.End();

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Effect_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            builder.AddEffect("test", EffectType::Permanent, [](IContext&, EffectType){});

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::PrimitiveTask));
        }
        TEST_METHOD(Splice_ThrowsIfNotCompoundPointer)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            auto domain = *DomainBuilder("sub-domain").Build();
            builder.AddAction("test");
            Assert::ExpectException<std::exception>([&]() { builder.Splice(domain); });
        }

        TEST_METHOD(Splice_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            auto domain = *DomainBuilder("sub-domain").Build();
            builder.Splice(domain);

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
        }

        TEST_METHOD(Splice_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            auto domain = *DomainBuilder("sub-domain").Build();
            builder.AddSelector("test");
            builder.Splice(domain);

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SelectorCompoundTask));
        }

        TEST_METHOD(Slot_ThrowsIfNotCompoundPointer)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddAction("test");
            Assert::ExpectException<std::exception>([&]() { builder.AddSlot(1); });
        }

        TEST_METHOD(Slot_ThrowsIfSlotIdAlreadyDefined)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSlot(1);
            Assert::ExpectException<std::exception>([&]() { builder.AddSlot(1); });
        }

        TEST_METHOD(Slot_ExpectedBehavior)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSlot(1);
            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::TaskRoot));

            auto domain = *builder.Build();

            auto subDomain = *DomainBuilder("sub-domain").Build();
            Assert::IsTrue(domain.TrySetSlotDomain(1, subDomain)); // Its valid to add a sub-domain to a slot we have defined in our domain definition, and that is not currently occupied.
            Assert::IsTrue(domain.TrySetSlotDomain(1, subDomain) == false); // Need to clear slot before we can attach sub-domain to a currently occupied slot.
            Assert::IsTrue(domain.TrySetSlotDomain(99, subDomain) == false); // Need to define slotId in domain definition before we can attach sub-domain to that slot.

            Assert::IsTrue(domain.Root()->Subtasks().size() == 1);
            Assert::IsTrue(domain.Root()->Subtasks()[0]->IsTypeOf(ITaskDerivedClassName::Slot));

            auto slot = StaticCastPtr<Slot>(domain.Root()->Subtasks()[0]);
            Assert::IsTrue(slot->Subtask() != nullptr);
            Assert::IsTrue(slot->Subtask()->IsTypeOf(ITaskDerivedClassName::TaskRoot));
            Assert::IsTrue(slot->Subtask()->Name() == "sub-domain"s);

            domain.ClearSlot(1);
            Assert::IsTrue(slot->Subtask() == nullptr);
        }

        TEST_METHOD(Slot_ForgotEnd)
        {
            // Arrange
            DomainBuilder builder("Test"s);

            // Act
            builder.AddSelector("test");
            builder.AddSlot(1);

            Assert::AreEqual(true, builder.Pointer()->IsTypeOf(ITaskDerivedClassName::SelectorCompoundTask));
        }
    };
}