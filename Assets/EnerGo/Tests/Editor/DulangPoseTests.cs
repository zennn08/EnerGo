using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class DulangPoseTests
    {
        // Pan-local axes: x = screen right, z = screen up, y = toward the viewer.
        static Vector3 PanUp(Vector2 tilt) => DulangPose.Tilt(tilt) * Vector3.up;

        [Test]
        public void LevelPhoneLeavesThePanFlat()
        {
            Assert.That(Quaternion.Angle(DulangPose.Tilt(Vector2.zero), Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void PanNormalTipsTowardTheLowSide()
        {
            Assert.That(PanUp(new Vector2(0.2f, 0f)).x, Is.GreaterThan(0.05f));   // low side right
            Assert.That(PanUp(new Vector2(0f, 0.2f)).z, Is.GreaterThan(0.05f));   // low side toward the screen top
        }

        [Test]
        public void TiltGrowsWithTheSensorAndStopsAtTheLimit()
        {
            float small = Quaternion.Angle(DulangPose.Tilt(new Vector2(0.05f, 0f)), Quaternion.identity);
            float big = Quaternion.Angle(DulangPose.Tilt(new Vector2(0.2f, 0f)), Quaternion.identity);
            float huge = Quaternion.Angle(DulangPose.Tilt(new Vector2(3f, 0f)), Quaternion.identity);
            Assert.That(big, Is.GreaterThan(small));
            Assert.That(huge, Is.EqualTo(DulangPose.MaxTiltDeg).Within(0.01f));
        }

        [Test]
        public void ScreenPointMapsToTheSameFootprint()
        {
            // A 160 px radius at 0.6 m on an 800 px tall, 60° camera: world radius = 160 * (2*0.6*tan30) / 800.
            float r = DulangPose.WorldRadius(160f, 0.6f, 60f, 800f);
            Assert.That(r, Is.EqualTo(160f * 2f * 0.6f * Mathf.Tan(30f * Mathf.Deg2Rad) / 800f).Within(1e-5f));
        }
    }
}
