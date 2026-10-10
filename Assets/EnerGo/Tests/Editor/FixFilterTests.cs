using NUnit.Framework;

namespace EnerGo.Tests
{
    public class FixFilterTests
    {
        const double Lat = -6.2, Lon = 106.8;
        const double OneKmLat = 1000.0 / 111194.93; // ~1 km north in degrees

        [Test]
        public void FirstRealFixIsAccepted()
        {
            var f = new FixFilter();
            Assert.IsTrue(f.Offer(Lat, Lon, 100, 8));
            Assert.IsTrue(f.HasFix);
            Assert.AreEqual(Lat, f.Lat);
        }

        [Test]
        public void FixWithoutTimestampIsIgnored()
        {
            var f = new FixFilter();
            Assert.IsFalse(f.Offer(0, 0, 0, 0)); // legacy service's empty lastData
            Assert.IsFalse(f.HasFix);
        }

        [Test]
        public void WalkingIsAccepted()
        {
            var f = new FixFilter();
            f.Offer(Lat, Lon, 100, 8);
            Assert.IsTrue(f.Offer(Lat + 3.0 / 111194.93, Lon, 102, 8)); // 3 m in 2 s
        }

        [Test]
        public void SingleWildJumpIsRejected()
        {
            var f = new FixFilter();
            f.Offer(Lat, Lon, 100, 8);
            Assert.IsFalse(f.Offer(Lat + OneKmLat, Lon, 101, 8)); // 1 km in 1 s
            Assert.AreEqual(Lat, f.Lat);
        }

        [Test]
        public void StaleFirstFixDoesNotLockThePlayer()
        {
            // Cached last-known location 1 km away comes first, then the real GPS keeps reporting the real place.
            var f = new FixFilter();
            f.Offer(Lat + OneKmLat, Lon, 100, 30);
            Assert.IsFalse(f.Offer(Lat, Lon, 101, 8));                    // looks like a jump: held back once
            Assert.IsTrue(f.Offer(Lat + 1.0 / 111194.93, Lon, 102, 8));   // agrees with the previous fix: real move
            Assert.AreEqual(Lat + 1.0 / 111194.93, f.Lat, 1e-9);
        }

        [Test]
        public void SpeedIsMeasuredFromTheLastAcceptedFix()
        {
            // Rejected fixes must not reset the clock: 1 km after 60 s since the last accepted fix is plausible (16.7 m/s).
            var f = new FixFilter();
            f.Offer(Lat, Lon, 100, 8);
            f.Offer(Lat + 5 * OneKmLat, Lon, 101, 8);                     // glitch, rejected
            Assert.IsTrue(f.Offer(Lat + OneKmLat, Lon, 160, 8));
        }
    }
}
