using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class PompaPoseTests
    {
        const float HalfTurn = Mathf.PI;

        [Test]
        public void PitmanKeepsItsLengthAllTheWayRound()
        {
            for (int i = 0; i < 48; i++)
            {
                var p = PompaPose.At(i / 48f * 2f * Mathf.PI);
                Assert.That(Vector2.Distance(p.Pin, p.Equalizer), Is.EqualTo(PompaPose.PitmanLength).Within(0.001f));
            }
        }

        [Test]
        public void PinTopRaisesTheRearAndDropsTheHead()
        {
            Assert.That(PompaPose.At(HalfTurn * 0.5f).HeadDownDeg, Is.GreaterThan(10f));
            Assert.That(PompaPose.At(HalfTurn * 1.5f).HeadDownDeg, Is.LessThan(-10f));
        }

        [Test]
        public void RodFollowsTheHeadAndStaysInTheCasing()
        {
            var down = PompaPose.At(HalfTurn * 0.5f);
            var up = PompaPose.At(HalfTurn * 1.5f);
            Assert.That(down.CarrierY, Is.LessThan(up.CarrierY));
            for (int i = 0; i < 48; i++)
            {
                var p = PompaPose.At(i / 48f * 2f * Mathf.PI);
                Assert.That(p.CarrierY - PompaPose.RodLength, Is.GreaterThanOrEqualTo(0f));          // never below the ground
                Assert.That(p.CarrierY - PompaPose.RodLength, Is.LessThanOrEqualTo(PompaPose.StuffingBoxTop)); // never out of the well
                Assert.That(p.CableLength, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void EveryPressOrReleaseAdvancesHalfATurnWithTheHeadFollowingTheFinger()
        {
            float held = PompaPose.NextCrankTarget(0f, true);
            Assert.That(held, Is.GreaterThan(0f));
            Assert.That(PompaPose.At(held).HeadDownDeg, Is.GreaterThan(10f));     // pressed: head down
            float released = PompaPose.NextCrankTarget(held, false);
            Assert.That(released, Is.EqualTo(held + HalfTurn).Within(0.001f));    // always forward, never back
            Assert.That(PompaPose.At(released).HeadDownDeg, Is.LessThan(-10f));   // released: head up
        }
    }
}
