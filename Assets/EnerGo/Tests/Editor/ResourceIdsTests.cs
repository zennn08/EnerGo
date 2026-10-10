using NUnit.Framework;

namespace EnerGo.Tests
{
    public class ResourceIdsTests
    {
        [Test]
        public void NewResourcesHaveNames()
        {
            Assert.AreEqual("Minyak Bumi", ResourceIds.Name(ResourceIds.Oil));
            Assert.AreEqual("Uranium", ResourceIds.Name(ResourceIds.Uranium));
            Assert.AreEqual("Matahari", ResourceIds.Name(ResourceIds.Sun));
        }

        [Test]
        public void OnlySunCoalGoldArePlayable()
        {
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Sun));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Coal));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Gold));
            Assert.IsFalse(ResourceIds.IsPlayable(ResourceIds.Oil));
            Assert.IsFalse(ResourceIds.IsPlayable(ResourceIds.Uranium));
            Assert.IsFalse(ResourceIds.IsPlayable("random"));
        }
    }
}
