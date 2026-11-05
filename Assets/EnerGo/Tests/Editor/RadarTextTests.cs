using NUnit.Framework;

namespace EnerGo.Tests
{
    public class RadarTextTests
    {
        [Test]
        public void CountdownIsMinutesAndSeconds() => Assert.AreEqual("SPAWN BARU 02:05", RadarText.Countdown(125));

        [Test]
        public void GpsLineWarnsOnlyWhenWeak()
        {
            Assert.AreEqual("GPS ±12 m", RadarText.GpsLine(12f, 30f));
            Assert.AreEqual("GPS ±45 m · Sinyal lemah, coba ke tempat terbuka", RadarText.GpsLine(45f, 30f));
        }

        [Test]
        public void CompassLineSaysWhenThereIsNoCompass()
        {
            Assert.AreEqual("Kompas tidak tersedia, utara di atas", RadarText.CompassLine(false, "", 0f));
            Assert.AreEqual("Kompas: Sensor · 90°", RadarText.CompassLine(true, "Sensor", 90.2f));
        }

        [Test]
        public void TargetLineHasDistanceAndDirection() => Assert.AreEqual("14 m · Timur Laut", RadarText.TargetLine(14.2, 45.0));

        [Test]
        public void CaptureButtonFollowsRangeAndLock()
        {
            Assert.AreEqual("TANGKAP", RadarText.CaptureButton(false, true, 10.0, out bool on));
            Assert.IsTrue(on);
            Assert.AreEqual("DEKATI DULU (14 m)", RadarText.CaptureButton(false, false, 14.2, out on));
            Assert.IsFalse(on);
            Assert.AreEqual("SEGERA HADIR", RadarText.CaptureButton(true, true, 3.0, out on));
            Assert.IsFalse(on);
        }

        [Test]
        public void EveryFailedFeedHasItsOwnTitleAndBody()
        {
            Assert.AreEqual("Izin lokasi ditolak", RadarText.FeedTitle(RadarText.Feed.Denied));
            Assert.AreEqual("Lokasi HP mati", RadarText.FeedTitle(RadarText.Feed.Disabled));
            Assert.AreEqual("GPS tidak merespons", RadarText.FeedTitle(RadarText.Feed.Failed));
            Assert.That(RadarText.FeedBody(RadarText.Feed.Disabled), Does.StartWith("Nyalakan Lokasi"));
        }
    }
}
