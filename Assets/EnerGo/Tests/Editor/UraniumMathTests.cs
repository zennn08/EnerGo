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
        public void SliderTopPullsTheRodOutBottomPushesItIn()
        {
            Assert.AreEqual(0f, UraniumMath.RodFromSlider(0f), 1e-4f);
            Assert.AreEqual(1f, UraniumMath.RodFromSlider(1f), 1e-4f);
            Assert.AreEqual(0.25f, UraniumMath.RodFromSlider(0.25f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.RodFromSlider(-0.3f), 1e-4f);
            Assert.AreEqual(1f, UraniumMath.RodFromSlider(1.4f), 1e-4f);
        }

        [Test]
        public void HalfRodWithoutDisturbanceSettlesInTheGreenZone()
        {
            float r = 0.5f;
            for (int i = 0; i < 100; i++) r = UraniumMath.StepReactivity(r, 0.5f, 0f, 0f, 0f, 0.1f);
            Assert.IsTrue(UraniumMath.InGreen(r), "r = " + r);
        }

        [Test]
        public void FullRodCoolsWithinASecond()
        {
            float r = 0.8f;
            for (int i = 0; i < 10; i++) r = UraniumMath.StepReactivity(r, 1f, 0f, 0f, 0f, 0.1f);
            Assert.Less(r, UraniumMath.GreenMin, "rod response must be visible within a second");
        }

        [Test]
        public void PulledRodHeatsUp()
        {
            float r = 0.5f;
            for (int i = 0; i < 10; i++) r = UraniumMath.StepReactivity(r, 0f, 0f, 0f, 0f, 0.1f);
            Assert.Greater(r, UraniumMath.HotAbove);
        }

        [Test]
        public void SpikeAndShakingRaiseReactivity()
        {
            Assert.AreEqual(0.5f + UraniumMath.SpikeSize, UraniumMath.StepReactivity(0.5f, 0.5f, 0f, UraniumMath.SpikeSize, 0f, 0f), 1e-4f);
            float calm = UraniumMath.StepReactivity(0.5f, 0.5f, 0f, 0f, 0f, 0.1f);
            float shaken = UraniumMath.StepReactivity(0.5f, 0.5f, 0.6f, 0f, 0f, 0.1f);
            Assert.Greater(shaken, calm);
            Assert.AreEqual(1f, UraniumMath.StepReactivity(0.95f, 0f, 0f, 0.5f, 0f, 0f), 1e-4f);
        }

        [Test]
        public void TinySensorJitterDoesNotCountAsShaking()
        {
            Assert.AreEqual(UraniumMath.StepReactivity(0.5f, 0.5f, 0f, 0f, 0f, 0.1f),
                            UraniumMath.StepReactivity(0.5f, 0.5f, 0.05f, 0f, 0f, 0.1f), 1e-5f);
        }

        [Test]
        public void GreenZoneAndProgress()
        {
            Assert.IsTrue(UraniumMath.InGreen(0.40f));
            Assert.IsTrue(UraniumMath.InGreen(0.60f));
            Assert.IsFalse(UraniumMath.InGreen(0.39f));
            Assert.IsFalse(UraniumMath.InGreen(0.61f));
            Assert.AreEqual(1f / 8f, UraniumMath.StepProgress(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f - 1f / 8f, UraniumMath.StepProgress(0.5f, 0.9f, 1f), 1e-4f);
        }

        [Test]
        public void DriftMakesTheMiddleRodUnsafeSoTheRodMustFollowIt()
        {
            float drift = UraniumMath.Drift(0.8f); // fuel pulling hot
            float idle = 0.5f, chased = 0.5f;
            for (int i = 0; i < 100; i++)
            {
                idle = UraniumMath.StepReactivity(idle, 0.5f, 0f, 0f, 0f, 0.1f, drift);
                chased = UraniumMath.StepReactivity(chased, 0.5f + drift, 0f, 0f, 0f, 0.1f, drift);
            }
            Assert.IsFalse(UraniumMath.InGreen(idle), "parking the rod in the middle must not win: r = " + idle);
            Assert.IsTrue(UraniumMath.InGreen(chased), "moving the rod with the drift holds green: r = " + chased);
        }

        [Test]
        public void RodHasLagSoItMustBeLed()
        {
            float r = UraniumMath.StepReactivity(0.8f, 1f, 0f, 0f, 0f, 0.1f);
            Assert.Greater(r, UraniumMath.GreenMax, "one tenth of a second of full rod is not an instant fix");
        }

        [Test]
        public void DoseBudgetIsFortyFiveSecondsDoubledWhenHot()
        {
            Assert.AreEqual(1f / 45f, UraniumMath.StepDose(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(2f / 45f, UraniumMath.StepDose(0f, 0.9f, 1f), 1e-4f);
        }

        [Test]
        public void MeltdownBeatsOverdoseBeatsCapture()
        {
            Assert.AreEqual(UraniumMath.Outcome.Meltdown, UraniumMath.Check(1f, 1f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.Overdose, UraniumMath.Check(0.5f, 1f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.Captured, UraniumMath.Check(0.5f, 0.4f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.None, UraniumMath.Check(0.5f, 0.4f, 0.9f));
        }

        [Test]
        public void SpikesComeEveryFourToSevenSecondsWithLongerWarning()
        {
            Assert.AreEqual(4f, UraniumMath.NextSpikeDelay(0f), 1e-4f);
            Assert.AreEqual(7f, UraniumMath.NextSpikeDelay(1f), 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.SpikeWarnSec, 1e-4f);
        }

        [Test]
        public void RevealIsMoreForgiving()
        {
            Assert.AreEqual(20f, UraniumMath.RevealAngleDeg, 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.RevealHoldSec, 1e-4f);
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
