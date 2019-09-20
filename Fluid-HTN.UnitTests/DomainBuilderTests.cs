
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.PrimitiveTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainBuilderTests
    {
        [TestMethod]
        public void Build_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            var ptr = builder.Pointer;
            var domain = builder.Build();

            Assert.IsTrue(domain.Root != null);
            Assert.IsTrue(ptr == domain.Root);
            Assert.AreEqual("Test", domain.Root.Name);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes = false)]
        public void BuildInvalidatesPointer_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            var domain = builder.Build();

            Assert.IsTrue(builder.Pointer == domain.Root);
        }

        [TestMethod]
        public void Selector_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Select("select test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Selector_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Select("select test");

            // Assert
            Assert.AreEqual(false, builder.Pointer is TaskRoot<byte>);
            Assert.AreEqual(true, builder.Pointer is Selector<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void SelectorBuild_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Select("select test");
            var domain = builder.Build();
        }

        [TestMethod]
        public void Selector_CompoundTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.CompoundTask<Selector<byte>>("select test");

            // Assert
            Assert.AreEqual(false, builder.Pointer is TaskRoot<byte>);
            Assert.AreEqual(true, builder.Pointer is Selector<byte>);
        }

        [TestMethod]
        public void Sequence_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Sequence_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Sequence("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is Sequence<byte>);
        }

        [TestMethod]
        public void Sequence_CompoundTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.CompoundTask<Sequence<byte>>("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is Sequence<byte>);
        }

        [TestMethod]
        public void Action_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("sequence test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Action_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask<byte>);
        }

        [TestMethod]
        public void Action_PrimitiveTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.PrimitiveTask<PrimitiveTask<byte>>("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void PausePlanThrowsWhenPointerIsNotDecomposeAll()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.PausePlan();
        }

        [TestMethod]
        public void PausePlan_ExpectedBehaviour()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.PausePlan();
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void PausePlan_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.PausePlan();

            Assert.AreEqual(true, builder.Pointer is Sequence<byte>);
        }

        [TestMethod]
        public void Condition_ExpectedBehaviour()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Condition("test", (ctx) => true);

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void ExecutingCondition_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.ExecutingCondition("test", (ctx) => true);
        }

        [TestMethod]
        public void ExecutingCondition_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.ExecutingCondition("test", (ctx) => true);
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void ExecutingCondition_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.ExecutingCondition("test", (ctx) => true);

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Do_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Do((ctx) => TaskStatus.Success);
        }

        [TestMethod]
        public void Do_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.Do((ctx) => TaskStatus.Success);
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Do_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.Do((ctx) => TaskStatus.Success);

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Effect_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });
        }

        [TestMethod]
        public void Effect_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Effect_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Splice_ThrowsIfNotCompoundPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            var domain = new DomainBuilder<MyContext, byte>("sub-domain").Build();
            builder.Action("test");
            builder.Splice(domain);
        }

        [TestMethod]
        public void Splice_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            var domain = new DomainBuilder<MyContext, byte>("sub-domain").Build();
            builder.Splice(domain);

            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);
        }

        [TestMethod]
        public void Splice_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            var domain = new DomainBuilder<MyContext, byte>("sub-domain").Build();
            builder.Select("test");
            builder.Splice(domain);

            Assert.AreEqual(true, builder.Pointer is Selector<byte>);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Slot_ThrowsIfNotCompoundPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Action("test");
            builder.Slot(1);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Slot_ThrowsIfSlotIdAlreadyDefined()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Slot(1);
            builder.Slot(1);
        }

        [TestMethod]
        public void Slot_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Slot(1);
            Assert.AreEqual(true, builder.Pointer is TaskRoot<byte>);

            var domain = builder.Build();

            var subDomain = new DomainBuilder<MyContext, byte>("sub-domain").Build();
            Assert.IsTrue(domain.TrySetSlotDomain(1, subDomain)); // Its valid to add a sub-domain to a slot we have defined in our domain definition, and that is not currently occupied.
            Assert.IsTrue(domain.TrySetSlotDomain(1, subDomain) == false); // Need to clear slot before we can attach sub-domain to a currently occupied slot.
            Assert.IsTrue(domain.TrySetSlotDomain(99, subDomain) == false); // Need to define slotId in domain definition before we can attach sub-domain to that slot.

            Assert.IsTrue(domain.Root.Subtasks.Count == 1);
            Assert.IsTrue(domain.Root.Subtasks[0] is Slot<byte>);

            var slot = (Slot<byte>) domain.Root.Subtasks[0];
            Assert.IsTrue(slot.Subtask != null);
            Assert.IsTrue(slot.Subtask is TaskRoot<byte>);
            Assert.IsTrue(slot.Subtask.Name == "sub-domain");

            domain.ClearSlot(1);
            Assert.IsTrue(slot.Subtask == null);
        }

        [TestMethod]
        public void Slot_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext, byte>("Test");

            // Act
            builder.Select("test");
            builder.Slot(1);

            Assert.AreEqual(true, builder.Pointer is Selector<byte>);
        }
    }
}
