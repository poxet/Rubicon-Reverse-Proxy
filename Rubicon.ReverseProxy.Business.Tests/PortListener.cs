using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rubicon.ReverseProxy.Business.Tests
{
    [TestClass]
    public class PortListener
    {
        [TestMethod]
        public void When_having_a_port_listeneer_with_one_rule_a_new_rule_with_same_public_port_can_be_added()
        {
            var rr = new Entities.RedirectRule(null, null, 3000, null, 3001);
            var pl = Business.PortListener.Create(rr);

            var rr2 = new Entities.RedirectRule(null, null, 3000, null, 3003);

            pl.Add(rr2);
            Assert.AreEqual(2, pl.RuleCount);
        }

        [TestMethod]
        public void When_having_a_port_listeneer_with_one_rule_a_new_rule_with_a_different_public_port_can_not_be_added()
        {
            var rr = new Entities.RedirectRule(null, null, 3000, null, 3001);
            var pl = Business.PortListener.Create(rr);

            var rr2 = new Entities.RedirectRule(null, null, 3002, null, 3003);

            try
            {
                pl.Add(rr2);
                Assert.Fail("Expected exception");
            }
            catch (Exception exception)
            {
                Assert.IsTrue(true);
            }
            Assert.AreEqual(1, pl.RuleCount);
        }
    }
}
