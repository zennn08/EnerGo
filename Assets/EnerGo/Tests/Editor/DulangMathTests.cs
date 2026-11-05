using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class DulangMathTests
    {
        const float Dt = 1f / 60f;

        struct Run { public float Swirl, Speed, Progress; public int RoughFrames; }

        // Feeds a tilt path through the same steps the game uses, once a frame for `seconds`.
        static Run Play(System.Func<float, Vector2> path, float seconds)
        {
            var r = new Run();
            float last = float.NaN, roughFor = 0f;
            Vector2 before = path(0f);
            for (float t = Dt; t < seconds; t += Dt)
            {
                Vector2 now = path(t);
                r.Swirl = DulangMath.StepSwirl(r.Swirl, ref last, now, Dt);
                r.Speed = DulangMath.StepTiltSpeed(r.Speed, before, now, Dt);
                before = now;
                r.Progress = DulangMath.StepProgress(r.Progress, r.Swirl, r.Speed, now.magnitude, 0f, ref roughFor, Dt, out bool rough);
                if (rough) r.RoughFrames++;
            }
            return r;
        }

        static Vector2 Circle(float t, float radius, float degPerSec)
        {
            float a = t * degPerSec * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        [Test]
        public void SteadyCircleFillsInAboutTenSeconds()
        {
            var r = Play(t => Circle(t, 0.2f, 180f), 10.5f);
            Assert.AreEqual(1f, r.Progress, 0.02f);
            Assert.AreEqual(0, r.RoughFrames);
        }

        [Test]
        public void WobblePassingNearLevelIsNotTooFast()
        {
            // A slow circle whose centre is off to one side: the path goes close by level, where the
            // direction of the tilt swings very fast although the phone hardly moves. This used to spill.
            var r = Play(t => new Vector2(0.11f, 0f) + Circle(t, 0.1f, 120f), 8f);
            Assert.AreEqual(0, r.RoughFrames);
        }

        [Test]
        public void HoldingStillOrRockingDoesNotWashTheSand()
        {
            Assert.AreEqual(0f, Play(t => new Vector2(0.2f, 0f), 5f).Progress, 1e-3f);
            Assert.That(Play(t => new Vector2(0.25f * Mathf.Sin(t * 5f), 0f), 5f).Progress, Is.LessThan(0.15f));
        }

        [Test]
        public void ReallyFastCirclesAreRough()
        {
            Assert.That(Play(t => Circle(t, 0.3f, 900f), 2f).RoughFrames, Is.GreaterThan(30));
        }

        [Test]
        public void TippingTooFarIsRough()
        {
            Assert.That(Play(t => Circle(t, 0.7f, 90f), 2f).RoughFrames, Is.GreaterThan(30));
        }

        [Test]
        public void AShortJoltOnlyWarnsThenALongOneSpills()
        {
            float roughFor = 0f;
            DulangMath.StepProgress(0.5f, 180f, 0.5f, 0.2f, 0.9f, ref roughFor, 0.1f, out bool rough);
            Assert.IsFalse(rough);
            Assert.That(roughFor, Is.GreaterThan(0f));
            DulangMath.StepProgress(0.5f, 180f, 0.5f, 0.2f, 0.9f, ref roughFor, 0.2f, out rough);
            Assert.IsTrue(rough);
            DulangMath.StepProgress(0.5f, 180f, 0.5f, 0.2f, 0f, ref roughFor, 0.1f, out rough);
            Assert.IsFalse(rough);
            Assert.AreEqual(0f, roughFor, 1e-4f);
        }

        [Test]
        public void InnerSurfaceRisesFromTheFloorToTheRim()
        {
            Assert.AreEqual(0.012f, DulangMath.InnerHeight(0f), 1e-4f);
            Assert.AreEqual(0.078f, DulangMath.InnerHeight(DulangMath.InnerRim), 1e-4f);
            Assert.That(DulangMath.InnerHeight(0.15f), Is.GreaterThan(DulangMath.InnerHeight(0.05f)));
        }

        [Test]
        public void WaterReachesFurtherUpTheLowSide()
        {
            var slope = new Vector2(0.2f, 0f); // level water climbs toward +x, the low side of the pan
            float low = DulangMath.WaterEdge(0f, 0.045f, slope), high = DulangMath.WaterEdge(Mathf.PI, 0.045f, slope);
            Assert.That(low, Is.GreaterThan(high));
            Assert.That(low, Is.LessThanOrEqualTo(DulangMath.InnerRim));
            Assert.That(high, Is.GreaterThan(0f));
        }

        [Test]
        public void SandThinsToNothingAtItsEdgeAndAsItWashesOut()
        {
            Assert.That(DulangMath.SandThickness(0f, 0f), Is.GreaterThan(0.015f));
            Assert.AreEqual(0f, DulangMath.SandThickness(DulangMath.SandRadius(0f) + 0.01f, 0f), 1e-5f);
            Assert.That(DulangMath.SandThickness(0f, 0.8f), Is.LessThan(DulangMath.SandThickness(0f, 0f)));
            Assert.AreEqual(0f, DulangMath.SandThickness(0f, 1f), 1e-5f);
        }
    }
}
