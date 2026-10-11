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
        public void FullRodCoolsWithinHalfASecond()
        {
            float r = 0.8f;
            for (int i = 0; i < 5; i++) r = UraniumMath.StepReactivity(r, 1f, 0f, 0f, 0f, 0.1f);
            Assert.Less(r, UraniumMath.GreenMin, "rod response must be visible in under a second");
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
        public void WiderGreenZoneAndFasterProgress()
        {
            Assert.IsTrue(UraniumMath.InGreen(0.35f));
            Assert.IsTrue(UraniumMath.InGreen(0.65f));
            Assert.IsFalse(UraniumMath.InGreen(0.34f));
            Assert.AreEqual(1f / 6f, UraniumMath.StepProgress(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f - 1f / 12f, UraniumMath.StepProgress(0.5f, 0.9f, 1f), 1e-4f);
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
