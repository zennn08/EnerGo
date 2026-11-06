using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class RadarWorldTests
    {
        [Test]
        public void TheSameCellAlwaysGrowsTheSameProps()
        {
            var a = RadarWorld.PropsInCell(12, -34);
            var b = RadarWorld.PropsInCell(12, -34);
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].X, b[i].X);
                Assert.AreEqual(a[i].Z, b[i].Z);
            }
        }

        [Test]
        public void NeighbouringCellsLookDifferent()
        {
            int same = 0;
            for (long c = 0; c < 20; c++)
            {
                var a = RadarWorld.PropsInCell(c, 5);
                var b = RadarWorld.PropsInCell(c + 1, 5);
                if (a.Length == b.Length && (a.Length == 0 || a[0].X == b[0].X)) same++;
            }
            Assert.Less(same, 3);
        }

        [Test]
        public void PropsStayInsideTheirCellWithNegativeCoordinatesToo()
        {
            for (long c = -30; c < 30; c++)
                foreach (var p in RadarWorld.PropsInCell(c, -c * 7))
                {
                    Assert.That(p.X, Is.InRange(-RadarWorld.CellM / 2f, RadarWorld.CellM / 2f));
                    Assert.That(p.Z, Is.InRange(-RadarWorld.CellM / 2f, RadarWorld.CellM / 2f));
                    Assert.Greater(p.Scale, 0f);
                }
        }

        [Test]
        public void TreesAreCommonAndHousesAreRare()
        {
            int trees = 0, houses = 0;
            for (long x = 0; x < 40; x++)
                for (long z = 0; z < 40; z++)
                    foreach (var p in RadarWorld.PropsInCell(x, z))
                    {
                        if (p.Kind == RadarWorld.PropKind.Pohon) trees++;
                        if (p.Kind == RadarWorld.PropKind.Rumah) houses++;
                    }
            Assert.Greater(houses, 0);
            Assert.Greater(trees, houses * 3);
        }

        [Test]
        public void MetresKeepRealDistancesAndCellsCoverNegativeLatitudes()
        {
            RadarWorld.ToMetres(-6.2, 106.8, out double e1, out double n1);
            RadarWorld.ToMetres(-6.2 + 100.0 / GeoSpawner.MetersPerDegree, 106.8, out double e2, out double n2);
            Assert.AreEqual(100.0, n2 - n1, 0.01);
            Assert.AreEqual(0.0, e2 - e1, 0.01);
            Assert.AreEqual(-1, RadarWorld.CellOf(-1.0));
            Assert.AreEqual(0, RadarWorld.CellOf(1.0));
        }

        [Test]
        public void SpawnBearingBecomesAPositionAroundThePlayer()
        {
            var north = RadarWorld.Around(0.0, 100.0);
            Assert.AreEqual(0f, north.x, 1e-3f);
            Assert.AreEqual(100f, north.y, 1e-3f);
            var east = RadarWorld.Around(90.0, 50.0);
            Assert.AreEqual(50f, east.x, 1e-3f);
            Assert.AreEqual(0f, east.y, 1e-3f);
        }

        [Test]
        public void TargetArrowPointsWhereTheSpawnIsSeenFromThePlayer()
        {
            Assert.AreEqual(0f, RadarWorld.ArrowRotation(90.0, 90f), 1e-3f);   // straight ahead: the icon already points up
            Assert.AreEqual(180f, RadarWorld.ArrowRotation(180.0, 0f), 1e-3f); // behind
            Assert.AreEqual(90f, RadarWorld.ArrowRotation(90.0, 0f), 1e-3f);   // to the right
        }

        [Test]
        public void RadarRingsFloatAboveTheTallestHill() =>
            Assert.Greater(RadarWorld.RingHeightM, RadarWorld.TallestHillM);

        [Test]
        public void ATapPicksTheNearestMarkerWithinReach()
        {
            var points = new[] { new Vector2(100, 100), new Vector2(200, 100) };
            Assert.AreEqual(1, RadarWorld.NearestWithin(new Vector2(180, 110), points, 2, 50f));
            Assert.AreEqual(-1, RadarWorld.NearestWithin(new Vector2(400, 400), points, 2, 50f));
            Assert.AreEqual(0, RadarWorld.NearestWithin(new Vector2(110, 100), points, 1, 50f));
            Assert.AreEqual(-1, RadarWorld.NearestWithin(new Vector2(200, 100), points, 1, 50f)); // only the first count points are live
        }
    }
}
