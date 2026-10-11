using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    // Android rotation vector: device -> world, world = East (x), North (y), Up (z).
    public class HeadingTests
    {
        static float Heading(Quaternion q) => GeoSpawner.HeadingFromRotation(q);

        [Test]
        public void FlatPhoneTopPointingNorthIsZero() => Assert.AreEqual(0f, Heading(Quaternion.identity), 1f);

        [Test]
        public void FlatPhoneTurnedLeftFacesWest()
        {
            // Turning counter-clockwise (seen from above) by 90°: the top of the phone points west.
            Assert.AreEqual(270f, Heading(Quaternion.AngleAxis(90f, Vector3.forward)), 1f);
        }

        [Test]
        public void UprightPhoneUsesTheBackCameraDirection()
        {
            // Tilt up 90° about the phone's x axis: back camera faces north; then turn clockwise to face east.
            var upright = Quaternion.AngleAxis(90f, Vector3.right);
            Assert.AreEqual(0f, Heading(upright), 1f);
            Assert.AreEqual(90f, Heading(Quaternion.AngleAxis(-90f, Vector3.forward) * upright), 1f);
        }

        [Test]
        public void HalfTiltedPhoneStillGivesTheSameHeading()
        {
            // 45° tilt (typical when looking at the radar) facing south-west: top and back camera agree.
            var q = Quaternion.AngleAxis(135f, Vector3.forward) * Quaternion.AngleAxis(45f, Vector3.right);
            Assert.AreEqual(225f, Heading(q), 1f);
        }
    }
}
