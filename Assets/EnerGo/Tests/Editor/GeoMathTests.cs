using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GeoMathTests
    {
        const double Lat = -6.2, Lon = 106.8;

        [Test]
        public void DistanceAndBearingEast()
        {
            // 0.001° of longitude at -6.2°: 111194.93 * 0.001 * cos(6.2°) = 110.545 m, due east
            Assert.AreEqual(110.545, GeoSpawner.DistanceM(Lat, Lon, Lat, Lon + 0.001), 0.5);
            Assert.AreEqual(90.0, GeoSpawner.BearingDeg(Lat, Lon, Lat, Lon + 0.001), 1.0);
        }

        [Test]
        public void DistanceAndBearingNorthAndSouthWest()
        {
            Assert.AreEqual(111.195, GeoSpawner.DistanceM(Lat, Lon, Lat + 0.001, Lon), 0.5);
            Assert.AreEqual(0.0, GeoSpawner.BearingDeg(Lat, Lon, Lat + 0.001, Lon), 1.0);
            Assert.AreEqual(225.0, GeoSpawner.BearingDeg(Lat, Lon, Lat - 0.001, Lon - 0.001 / System.Math.Cos(6.2 * System.Math.PI / 180)), 1.0);
        }

        [TestCase(0, "Utara")]
        [TestCase(350, "Utara")]
        [TestCase(45, "Timur Laut")]
        [TestCase(90, "Timur")]
        [TestCase(200, "Selatan")]
        [TestCase(225, "Barat Daya")]
        [TestCase(300, "Barat Laut")]
        public void Direction8Names(double bearing, string expected) => Assert.AreEqual(expected, GeoSpawner.Direction8(bearing));

        [Test]
        public void CaptureRuleCapsTheAccuracyBonus()
        {
            Assert.IsTrue(GeoSpawner.CanCapture(18, 3));   // plainly in range
            Assert.IsTrue(GeoSpawner.CanCapture(30, 12));  // 30 - 12 = 18
            Assert.IsFalse(GeoSpawner.CanCapture(36, 40)); // bonus capped at 15 -> 21
            Assert.IsFalse(GeoSpawner.CanCapture(25, 0));
        }

        [Test]
        public void ImplausibleJumpsAreRejected()
        {
            Assert.IsTrue(GeoSpawner.PlausibleMove(40, 1));
            Assert.IsFalse(GeoSpawner.PlausibleMove(60, 1));
            Assert.IsTrue(GeoSpawner.PlausibleMove(300, 10));
            Assert.IsFalse(GeoSpawner.PlausibleMove(80, 0)); // same timestamp, big jump
        }

        [Test]
        public void HeadingSmoothingTakesTheShortWayAcrossNorth()
        {
            float h = GeoSpawner.SmoothHeading(350f, 10f, 0.05f);
            // moved towards 360/0, not backwards towards 180
            Assert.IsTrue(h > 350f || h < 10f, "got " + h);
            Assert.That(h, Is.InRange(0f, 360f));
        }
    }
}
