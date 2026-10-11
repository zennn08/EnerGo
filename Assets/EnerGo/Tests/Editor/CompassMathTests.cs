using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class CompassMathTests
    {
        [Test]
        public void HeadingAndOffsetWrapAroundNorth()
        {
            Assert.AreEqual(10f, CompassMath.CameraHeading(350f, 20f), 1e-3f);
            Assert.AreEqual(20f, CompassMath.OffsetTarget(10f, 350f), 1e-3f);
            Assert.AreEqual(340f, CompassMath.OffsetTarget(330f, -10f), 1e-3f);
        }

        [Test]
        public void CalibratesOnlyWhenNotLookingStraightUpOrDown()
        {
            Assert.IsTrue(CompassMath.CanCalibrate(0f));
            Assert.IsTrue(CompassMath.CanCalibrate(60f));
            Assert.IsTrue(CompassMath.CanCalibrate(-60f));
            Assert.IsFalse(CompassMath.CanCalibrate(61f));
            Assert.IsFalse(CompassMath.CanCalibrate(-80f));
        }

        [Test]
        public void SmoothOffsetTakesTheShortWay()
        {
            float step = CompassMath.SmoothOffset(350f, 10f, 0.1f);
            float moved = Mathf.DeltaAngle(350f, step);
            Assert.Greater(moved, 0f);
            Assert.Less(moved, 20f);
            Assert.AreEqual(10f, Mathf.Repeat(CompassMath.SmoothOffset(350f, 10f, 100f), 360f), 0.01f);
        }

        [Test]
        public void StripCentreEdgesAndOutside()
        {
            Assert.AreEqual(0f, CompassMath.StripX(0f, 0f, 60f), 1e-4f);
            Assert.AreEqual(1f, CompassMath.StripX(60f, 0f, 60f), 1e-4f);
            Assert.AreEqual(-1f, CompassMath.StripX(300f, 0f, 60f), 1e-4f);
            Assert.Greater(CompassMath.StripX(90f, 0f, 60f), 1f);
        }

        [Test]
        public void StripWrapsAcrossNorth()
        {
            Assert.AreEqual(20f / 60f, CompassMath.StripX(10f, 350f, 60f), 1e-4f);  // just right of centre
            Assert.AreEqual(-20f / 60f, CompassMath.StripX(350f, 10f, 60f), 1e-4f); // just left
        }

        [Test]
        public void WorldYawIsBearingMinusOffset()
        {
            Assert.AreEqual(70f, CompassMath.WorldYaw(90f, 20f), 1e-4f);
            Assert.AreEqual(-10f, CompassMath.WorldYaw(10f, 20f), 1e-4f);
        }

        [Test]
        public void BackCameraHeadingUprightNorthAndEast()
        {
            Quaternion north = Quaternion.AngleAxis(90f, Vector3.right);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, CompassMath.BackCameraHeading(north)), 1e-3f);
            Quaternion east = Quaternion.AngleAxis(-90f, Vector3.forward) * north;
            Assert.AreEqual(90f, CompassMath.BackCameraHeading(east), 1e-3f);
        }

        [Test]
        public void BackCameraHeadingDoesNotFlipWhenPitchedUp()
        {
            Quaternion up50 = Quaternion.AngleAxis(140f, Vector3.right); // facing north, 50° up
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, CompassMath.BackCameraHeading(up50)), 1e-3f);
            Assert.AreEqual(180f, GeoSpawner.HeadingFromRotation(up50), 1e-3f); // why the radar function can't be used here
        }

        [Test]
        public void WorldYawAndCameraHeadingRoundTrip()
        {
            foreach (float b in new[] { 0f, 90f, 200f, 359f })
                foreach (float off in new[] { 0f, 45f, 300f })
                    Assert.AreEqual(0f, Mathf.DeltaAngle(b, CompassMath.CameraHeading(CompassMath.WorldYaw(b, off), off)), 1e-3f);
        }

        [Test]
        public void DirectionLabels()
        {
            string[] expect = { "U", "TL", "T", "TG", "S", "BD", "B", "BL", "U" };
            for (int i = 0; i < expect.Length; i++) Assert.AreEqual(expect[i], CompassMath.DirectionLabel(i * 45));
        }
    }
}
