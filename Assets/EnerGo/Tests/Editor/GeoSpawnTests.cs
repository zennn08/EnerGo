using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GeoSpawnTests
    {
        const double Lat = -6.2, Lon = 106.8;
        const long Now = 1_791_600_000; // fixed instant

        [Test]
        public void SameInputSameSpawns()
        {
            var a = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            var b = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
                Assert.AreEqual(a[i].ResourceId, b[i].ResourceId);
                Assert.AreEqual(a[i].Lat, b[i].Lat);
                Assert.AreEqual(a[i].Lon, b[i].Lon);
            }
        }

        [Test]
        public void CountAndTypeRatesMatchTheTable()
        {
            var counts = new Dictionary<string, int>();
            int spawns = 0, cells = 0;
            for (long x = -158; x < 158; x++)
            for (long y = -158; y < 158; y++)
            {
                cells++;
                foreach (var s in GeoSpawner.SpawnsInCell(x, y, 42, 12))
                {
                    spawns++;
                    counts[s.ResourceId] = counts.TryGetValue(s.ResourceId, out int n) ? n + 1 : 1;
                }
            }
            Assert.AreEqual(0.5, spawns / (double)cells, 0.01, "mean spawns per cell");
            var expected = new Dictionary<string, double> { ["sun"] = .35, ["coal"] = .30, ["gold"] = .20, ["oil"] = .12, ["uranium"] = .03 };
            foreach (var kv in expected)
                Assert.AreEqual(kv.Value, counts[kv.Key] / (double)spawns, 0.01, kv.Key);
        }

        [Test]
        public void NoSunAtNightAndRestRenormalised()
        {
            var counts = new Dictionary<string, int>();
            int spawns = 0;
            for (long x = 0; x < 300; x++)
            for (long y = 0; y < 300; y++)
                foreach (var s in GeoSpawner.SpawnsInCell(x, y, 7, 20))
                {
                    spawns++;
                    counts[s.ResourceId] = counts.TryGetValue(s.ResourceId, out int n) ? n + 1 : 1;
                }
            Assert.IsFalse(counts.ContainsKey("sun"));
            Assert.AreEqual(0.30 / 0.65, counts["coal"] / (double)spawns, 0.015);
        }

        [Test]
        public void DaytimeBoundaries()
        {
            Assert.IsFalse(GeoSpawner.IsDaytime(5));
            Assert.IsTrue(GeoSpawner.IsDaytime(6));
            Assert.IsTrue(GeoSpawner.IsDaytime(17));
            Assert.IsFalse(GeoSpawner.IsDaytime(18));
        }

        [Test]
        public void DayAndNightShareCountsAndPositions()
        {
            // Only the type may differ between day and night, so a spawn never "jumps" at 18:00.
            var day = GeoSpawner.SpawnsInCell(12, -34, 99, 12);
            var night = GeoSpawner.SpawnsInCell(12, -34, 99, 20);
            Assert.AreEqual(day.Count, night.Count);
            for (int i = 0; i < day.Count; i++) { Assert.AreEqual(day[i].Lat, night[i].Lat); Assert.AreEqual(day[i].Lon, night[i].Lon); }
        }

        [Test]
        public void NearbyMatchesBruteForceAtNegativeLatitude()
        {
            // Points sprinkled around a cell corner: Nearby must see spawns in neighbouring cells too.
            long window = GeoSpawner.WindowOf(Now);
            for (int k = 0; k < 25; k++)
            {
                double lat = Lat + (k % 5) * 0.00011, lon = Lon + (k / 5) * 0.00013;
                var got = GeoSpawner.Nearby(lat, lon, 150, Now, 12, null).Select(s => s.Id).ToList();
                var want = new List<string>();
                long cx = GeoSpawner.CellOf(lon), cy = GeoSpawner.CellOf(lat);
                for (long x = cx - 6; x <= cx + 6; x++)
                for (long y = cy - 6; y <= cy + 6; y++)
                    foreach (var s in GeoSpawner.SpawnsInCell(x, y, window, 12))
                        if (GeoSpawner.DistanceM(lat, lon, s.Lat, s.Lon) <= 150) want.Add(s.Id);
                CollectionAssert.AreEquivalent(want, got, "point " + k);
            }
        }

        [Test]
        public void NearbyIsSortedAndSkipsCaptured()
        {
            var all = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            Assert.Greater(all.Count, 2, "fixture needs a few spawns");
            for (int i = 1; i < all.Count; i++)
                Assert.LessOrEqual(GeoSpawner.DistanceM(Lat, Lon, all[i - 1].Lat, all[i - 1].Lon),
                                   GeoSpawner.DistanceM(Lat, Lon, all[i].Lat, all[i].Lon));
            var captured = new HashSet<string> { all[0].Id };
            var rest = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, captured);
            Assert.AreEqual(all.Count - 1, rest.Count);
            Assert.IsFalse(rest.Any(s => s.Id == all[0].Id));
        }

        [Test]
        public void IdsEncodeCellWindowAndIndex()
        {
            var s = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null)[0];
            var parts = s.Id.Split(':');
            Assert.AreEqual(4, parts.Length);
            Assert.AreEqual(GeoSpawner.WindowOf(Now).ToString(), parts[2]);
        }

        [Test]
        public void WindowMath()
        {
            Assert.AreEqual(0, GeoSpawner.WindowOf(899));
            Assert.AreEqual(1, GeoSpawner.WindowOf(900));
            Assert.AreEqual(1, GeoSpawner.SecondsToNextWindow(899));
            Assert.AreEqual(900, GeoSpawner.SecondsToNextWindow(900));
        }

        [Test]
        public void PruneKeepsCurrentAndPreviousWindow()
        {
            string[] ids = { "1:2:100:0", "1:2:99:1", "3:4:98:0", "garbage" };
            CollectionAssert.AreEquivalent(new[] { "1:2:100:0", "1:2:99:1" }, GeoSpawner.PruneCaptured(ids, 100));
            CollectionAssert.IsEmpty(GeoSpawner.PruneCaptured(null, 100));
        }
    }
}
