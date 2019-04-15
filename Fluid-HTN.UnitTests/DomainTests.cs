using System;
using FluidHTN;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluid_HTN.UnitTests
{
    [TestClass]
    public class DomainTests
    {
        [TestMethod]
        public void DomainHasRootWithDomainName_ExpectedBehavior()
        {
            var domain = new Domain<MyContext>("Test");
            Assert.IsTrue(domain.Root != null);
            Assert.IsTrue(domain.Root.Name == "Test");
        }
    }
}
