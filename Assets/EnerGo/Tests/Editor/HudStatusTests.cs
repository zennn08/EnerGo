using NUnit.Framework;

namespace EnerGo.Tests
{
    public class HudStatusTests
    {
        [Test]
        public void TagFollowsWarning()
        {
            Assert.AreEqual("AMAN", HudStatus.Tags(false));
            Assert.AreEqual("AWAS", HudStatus.Tags(true));
        }

        [Test]
        public void PlainMessageIsInfoWithoutBar()
        {
            var s = HudStatus.Plain("BERALIH KE SENSOR 360");
            Assert.AreEqual("INFO", s.Tag);
            Assert.AreEqual("BERALIH KE SENSOR 360", s.Line);
            Assert.IsFalse(s.HasBar);
        }

        [Test]
        public void BarStatusCarriesItsValueAndLabel()
        {
            var s = HudStatus.WithBar("PECAHKAN", false, 0.25f, "1/4");
            Assert.AreEqual("AMAN", s.Tag);
            Assert.IsTrue(s.HasBar);
            Assert.AreEqual(0.25f, s.Bar, 1e-5f);
            Assert.AreEqual("1/4", s.Label);
        }

        [Test]
        public void TapsOnTheHudBandsDoNotReachTheGame()
        {
            Assert.IsTrue(HudTapZones.Blocks(40f, 800f));      // header
            Assert.IsTrue(HudTapZones.Blocks(127f, 800f));
            Assert.IsFalse(HudTapZones.Blocks(128f, 800f));    // play area
            Assert.IsFalse(HudTapZones.Blocks(400f, 800f));
            Assert.IsFalse(HudTapZones.Blocks(730f, 800f));
            Assert.IsTrue(HudTapZones.Blocks(731f, 800f));     // bottom button
        }
    }
}
