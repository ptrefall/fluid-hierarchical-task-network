
using FluidHTN;
using FluidHTN.Compounds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainBuilderTests
    {
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
    }
}
