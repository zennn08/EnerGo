using NUnit.Framework;

namespace EnerGo.Tests
{
    public class LobbyTextTests
    {
        [Test]
        public void BadgeCapsAt99()
        {
            Assert.AreEqual("0", LobbyText.Badge(0));
            Assert.AreEqual("99", LobbyText.Badge(99));
            Assert.AreEqual("99+", LobbyText.Badge(100));
        }

        [Test]
        public void CatalogNumbersFollowTheSpecimenOrder()
        {
            Assert.AreEqual("No.01", LobbyText.CatalogNo(ResourceIds.Sun));
            Assert.AreEqual("No.02", LobbyText.CatalogNo(ResourceIds.Coal));
            Assert.AreEqual("No.03", LobbyText.CatalogNo(ResourceIds.Gold));
            Assert.AreEqual("No.04", LobbyText.CatalogNo(ResourceIds.Oil));
            Assert.AreEqual("No.05", LobbyText.CatalogNo(ResourceIds.Uranium));
            Assert.AreEqual("???", LobbyText.CatalogNo("random"));
        }
    }
}
