
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
            var builder = new DomainBuilder<MyContext>("Test");

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
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            var domain = builder.Build();

            Assert.IsTrue(builder.Pointer == domain.Root);
        }

        [TestMethod]
        public void Selector_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Select("select test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Selector_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Select("select test");

            // Assert
            Assert.AreEqual(false, builder.Pointer is TaskRoot);
            Assert.AreEqual(true, builder.Pointer is Selector);
        }

        [TestMethod]
        public void Selector_CompoundTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.CompoundTask<Selector>("select test");

            // Assert
            Assert.AreEqual(false, builder.Pointer is TaskRoot);
            Assert.AreEqual(true, builder.Pointer is Selector);
        }

        [TestMethod]
        public void Sequence_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Sequence_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Sequence("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is Sequence);
        }

        [TestMethod]
        public void Sequence_CompoundTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.CompoundTask<Sequence>("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is Sequence);
        }

        [TestMethod]
        public void Action_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("sequence test");
            builder.End();

            // Assert
            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Action_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask);
        }

        [TestMethod]
        public void Action_PrimitiveTask()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.PrimitiveTask<PrimitiveTask>("sequence test");

            // Assert
            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void PausePlanThrowsWhenPointerIsNotDecomposeAll()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.PausePlan();
        }

        [TestMethod]
        public void PausePlan_ExpectedBehaviour()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.PausePlan();
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void PausePlan_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Sequence("sequence test");
            builder.PausePlan();

            Assert.AreEqual(true, builder.Pointer is Sequence);
        }

        [TestMethod]
        public void Condition_ExpectedBehaviour()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Condition("test", (ctx) => true);

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void ExecutingCondition_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.ExecutingCondition("test", (ctx) => true);
        }

        [TestMethod]
        public void ExecutingCondition_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.ExecutingCondition("test", (ctx) => true);
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void ExecutingCondition_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.ExecutingCondition("test", (ctx) => true);

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Do_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Do((ctx) => TaskStatus.Success);
        }

        [TestMethod]
        public void Do_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.Do((ctx) => TaskStatus.Success);
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Do_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.Do((ctx) => TaskStatus.Success);

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Effect_ThrowsIfNotPrimitiveTaskPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });
        }

        [TestMethod]
        public void Effect_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });
            builder.End();

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Effect_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            builder.Action("test");
            builder.Effect("test", EffectType.Permanent, (ctx, t) => { });

            Assert.AreEqual(true, builder.Pointer is IPrimitiveTask);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = false)]
        public void Splice_ThrowsIfNotCompoundPointer()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            var domain = new DomainBuilder<MyContext>("sub-domain").Build();
            builder.Action("test");
            builder.Splice(domain);
        }

        [TestMethod]
        public void Splice_ExpectedBehavior()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            var domain = new DomainBuilder<MyContext>("sub-domain").Build();
            builder.Splice(domain);

            Assert.AreEqual(true, builder.Pointer is TaskRoot);
        }

        [TestMethod]
        public void Splice_ForgotEnd()
        {
            // Arrange
            var builder = new DomainBuilder<MyContext>("Test");

            // Act
            var domain = new DomainBuilder<MyContext>("sub-domain").Build();
            builder.Select("test");
            builder.Splice(domain);

            Assert.AreEqual(true, builder.Pointer is Selector);
        }
    }
}
