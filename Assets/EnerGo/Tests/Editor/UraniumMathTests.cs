using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class UraniumMathTests
    {
        [Test]
        public void GeigerClicksFasterTheCloserTheAim()
        {
            Assert.AreEqual(25f, UraniumMath.GeigerRate(0f), 0.01f);
            Assert.AreEqual(1f, UraniumMath.GeigerRate(150f), 0.01f);
            Assert.AreEqual(1f, UraniumMath.GeigerRate(180f), 0.01f);
            float prev = float.MaxValue;
            for (float a = 0f; a <= 180f; a += 10f)
            {
                float rate = UraniumMath.GeigerRate(a);
                Assert.LessOrEqual(rate, prev);
                prev = rate;
            }
        }

        [Test]
        public void ClickDelayIsExponentialAndNeverInfinite()
        {
            Assert.AreEqual(0.1f, UraniumMath.NextClickDelay(10f, Mathf.Exp(-1f)), 0.001f);
            Assert.IsFalse(float.IsInfinity(UraniumMath.NextClickDelay(10f, 0f)));
        }

        [Test]
        public void MeterShowsHundredWhenAimedStraightAtTheOre()
        {
            Assert.AreEqual(100, UraniumMath.Meter(0f));
            Assert.AreEqual(50, UraniumMath.Meter(90f));
            Assert.AreEqual(0, UraniumMath.Meter(180f));
        }

        [Test]
        public void TiltIsZeroUprightAndNinetyFlat()
        {
            Assert.AreEqual(0f, UraniumMath.TiltDeg(new Vector3(0f, -1f, 0f)), 0.1f);
            Assert.AreEqual(90f, UraniumMath.TiltDeg(new Vector3(0f, 0f, -1f)), 0.1f);
        }

        [Test]
        public void RodStartsHalfwayAndTiltingForwardPushesItIn()
        {
            Assert.AreEqual(0.5f, UraniumMath.RodFromTilt(20f, 20f), 1e-4f);
            Assert.Greater(UraniumMath.RodFromTilt(30f, 20f), 0.5f);
            Assert.AreEqual(1f, UraniumMath.RodFromTilt(200f, 20f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.RodFromTilt(-200f, 20f), 1e-4f);
        }

        [Test]
        public void HalfRodWithoutDisturbanceSettlesInTheGreenZone()
        {
            float r = 0.5f;
            for (int i = 0; i < 100; i++) r = UraniumMath.StepReactivity(r, 0.5f, 0f, 0f, 0f, 0.1f);
            Assert.IsTrue(UraniumMath.InGreen(r), "r = " + r);
        }

        [Test]
        public void FullRodCoolsAndPulledRodHeatsUp()
        {
            float cool = 0.8f, hot = 0.5f;
            for (int i = 0; i < 20; i++)
            {
                cool = UraniumMath.StepReactivity(cool, 1f, 0f, 0f, 0f, 0.1f);
                hot = UraniumMath.StepReactivity(hot, 0f, 0f, 0f, 0f, 0.1f);
            }
            Assert.Less(cool, 0.5f);
            Assert.Greater(hot, 0.7f);
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
        public void ProgressFillsOnlyInTheGreenZone()
        {
            Assert.AreEqual(1f / 8f, UraniumMath.StepProgress(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f - 1f / 16f, UraniumMath.StepProgress(0.5f, 0.9f, 1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepProgress(0f, 0f, 1f), 1e-4f);
        }

        [Test]
        public void DoseRisesTwiceAsFastWhenHot()
        {
            float normal = UraniumMath.StepDose(0f, 0.5f, 1f);
            float hot = UraniumMath.StepDose(0f, 0.9f, 1f);
            Assert.AreEqual(1f / 35f, normal, 1e-4f);
            Assert.AreEqual(2f * normal, hot, 1e-4f);
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
        public void SpikesComeEveryThreeToSixSeconds()
        {
            Assert.AreEqual(3f, UraniumMath.NextSpikeDelay(0f), 1e-4f);
            Assert.AreEqual(6f, UraniumMath.NextSpikeDelay(1f), 1e-4f);
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
