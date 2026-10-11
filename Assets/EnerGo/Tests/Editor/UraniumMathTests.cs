using NUnit.Framework;

namespace EnerGo.Tests
{
    public class UraniumMathTests
    {
        [Test]
        public void GeigerTicksFasterTheCloserTheAim()
        {
            Assert.AreEqual(1f / 20f, UraniumMath.GeigerInterval(0f), 1e-4f);
            Assert.AreEqual(1f / 2f, UraniumMath.GeigerInterval(150f), 1e-4f);
            Assert.AreEqual(1f / 2f, UraniumMath.GeigerInterval(180f), 1e-4f);
            float prev = 0f;
            for (float a = 0f; a <= 180f; a += 10f)
            {
                float gap = UraniumMath.GeigerInterval(a);
                Assert.GreaterOrEqual(gap, prev);
                prev = gap;
            }
        }

        [Test]
        public void MeterShowsHundredWhenAimedStraightAtTheOre()
        {
            Assert.AreEqual(100, UraniumMath.Meter(0f));
            Assert.AreEqual(50, UraniumMath.Meter(90f));
            Assert.AreEqual(0, UraniumMath.Meter(180f));
        }

        [Test]
        public void TrendNeedsThreePoints()
        {
            Assert.AreEqual(1, UraniumMath.Trend(40, 43));
            Assert.AreEqual(-1, UraniumMath.Trend(43, 40));
            Assert.AreEqual(0, UraniumMath.Trend(40, 42));
            Assert.AreEqual(0, UraniumMath.Trend(42, 40));
        }

        [Test]
        public void RevealIsForgiving()
        {
            Assert.AreEqual(20f, UraniumMath.RevealAngleDeg, 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.RevealHoldSec, 1e-4f);
        }

        [Test]
        public void FirstFragmentStaysWhereTheOreWasAndTheRestAreSpreadOut()
        {
            var rng = new System.Random(1);
            for (int run = 0; run < 200; run++)
            {
                float[] yaws = UraniumMath.FragmentYaws(() => (float)rng.NextDouble());
                Assert.AreEqual(UraniumMath.FragmentCount, yaws.Length);
                Assert.AreEqual(0f, yaws[0], 1e-4f);
                for (int i = 0; i < yaws.Length; i++)
                    for (int j = i + 1; j < yaws.Length; j++)
                        Assert.GreaterOrEqual(UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(yaws[i], yaws[j])), UraniumMath.FragmentMinSepDeg - 1e-3f);
            }
        }

        [Test]
        public void FragmentsFallBackToAnEvenSpread()
        {
            float[] yaws = UraniumMath.FragmentYaws(() => 0.5f); // every draw lands on the ore's own bearing
            Assert.AreEqual(0f, yaws[0], 1e-4f);
            Assert.AreEqual(120f, yaws[1], 1e-4f);
            Assert.AreEqual(240f, yaws[2], 1e-4f);
        }

        [Test]
        public void NearestSkipsTakenFragments()
        {
            float[] angles = { 10f, 90f, 40f };
            Assert.AreEqual(0, UraniumMath.Nearest(angles, new[] { false, false, false }));
            Assert.AreEqual(2, UraniumMath.Nearest(angles, new[] { true, false, false }));
            Assert.AreEqual(-1, UraniumMath.Nearest(angles, new[] { true, true, true }));
        }

        [Test]
        public void GripFillsInOneSecondWhileHeldAimedAndSteady()
        {
            float g = 0f;
            for (int i = 0; i < 10; i++) g = UraniumMath.StepGrip(g, true, 5f, 0f, 0.1f);
            Assert.AreEqual(1f, g, 1e-4f);
            Assert.AreEqual(1f, UraniumMath.StepGrip(1f, true, 5f, 0f, 0.1f), 1e-4f);
        }

        [Test]
        public void GripLetsGoOnReleaseMissOrShake()
        {
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, false, 5f, 0f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, true, UraniumMath.GripAngleDeg + 1f, 0f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, true, 5f, UraniumMath.GripShakeMax, 0.1f), 1e-4f);
            Assert.Greater(UraniumMath.StepGrip(0.7f, true, 5f, UraniumMath.GripShakeMax - 0.05f, 0.1f), 0.7f);
        }

        [Test]
        public void DoseFillsInFortySecondsWithEverythingOut()
        {
            Assert.AreEqual(1f / 40f, UraniumMath.StepDose(0f, 3, 1f), 1e-5f);
            float d = 0f;
            for (int i = 0; i < 400; i++) d = UraniumMath.StepDose(d, 3, 0.1f);
            Assert.AreEqual(1f, d, 1e-4f);
        }

        [Test]
        public void EachShieldedFragmentSlowsTheDoseButNeverStopsIt()
        {
            float d3 = UraniumMath.StepDose(0f, 3, 1f), d2 = UraniumMath.StepDose(0f, 2, 1f);
            float d1 = UraniumMath.StepDose(0f, 1, 1f), d0 = UraniumMath.StepDose(0f, 0, 1f);
            Assert.Greater(d3, d2);
            Assert.Greater(d2, d1);
            Assert.Greater(d1, d0);
            Assert.AreEqual(0.25f / 40f, d0, 1e-5f);
        }

        [Test]
        public void CaptureBeatsOverdoseOnTheSameFrame()
        {
            Assert.AreEqual(UraniumMath.Outcome.Captured, UraniumMath.Check(1f, 3));
            Assert.AreEqual(UraniumMath.Outcome.Overdose, UraniumMath.Check(1f, 2));
            Assert.AreEqual(UraniumMath.Outcome.None, UraniumMath.Check(0.9f, 2));
        }

        [Test]
        public void FastLowDoseCaptureScoresHigher()
        {
            float good = UraniumMath.Performance(UraniumMath.ParSec, 0.2f);
            float bad = UraniumMath.Performance(UraniumMath.ParSec * 3f, 0.9f);
            Assert.Greater(good, bad);
            Assert.That(good, Is.InRange(0f, 1f));
            Assert.That(bad, Is.InRange(0f, 1f));
        }
    }
}
