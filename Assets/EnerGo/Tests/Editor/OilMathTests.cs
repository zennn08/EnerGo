using NUnit.Framework;

namespace EnerGo.Tests
{
    public class OilMathTests
    {
        static readonly float[] Fixed = { 0.1f, 0.3f, 0.45f, 0.55f, 0.62f };

        [Test]
        public void LayersAreDeterministicPerSeed()
        {
            CollectionAssert.AreEqual(OilMath.Layers(42), OilMath.Layers(42));
            CollectionAssert.AreNotEqual(OilMath.Layers(1), OilMath.Layers(2));
        }

        [Test]
        public void LayersAreOrderedWithAThinOilBandAndBrineBelow()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var b = OilMath.Layers(seed);
                Assert.AreEqual(5, b.Length);
                for (int i = 1; i < b.Length; i++) Assert.Greater(b[i], b[i - 1]);
                float oil = b[4] - b[3];
                Assert.That(oil, Is.InRange(0.06f - 1e-4f, 0.09f + 1e-4f), "seed " + seed);
                Assert.Less(b[4], 0.9f, "brine must remain below, seed " + seed);
            }
        }

        [Test]
        public void LayerAtUsesHalfOpenBands()
        {
            Assert.AreEqual(OilMath.Layer.Soil, OilMath.LayerAt(0f, Fixed));
            Assert.AreEqual(OilMath.Layer.Soil, OilMath.LayerAt(0.0999f, Fixed));
            Assert.AreEqual(OilMath.Layer.Rock, OilMath.LayerAt(0.1f, Fixed));
            Assert.AreEqual(OilMath.Layer.Gas, OilMath.LayerAt(0.5f, Fixed));
            Assert.AreEqual(OilMath.Layer.Oil, OilMath.LayerAt(0.6f, Fixed));
            Assert.AreEqual(OilMath.Layer.Brine, OilMath.LayerAt(0.62f, Fixed));
            Assert.AreEqual(OilMath.Layer.Brine, OilMath.LayerAt(1f, Fixed));
        }

        [Test]
        public void HardCapRockIsSlowerAndGasIsFaster()
        {
            Assert.Less(OilMath.SpeedMul(OilMath.Layer.CapRock), OilMath.SpeedMul(OilMath.Layer.Rock));
            Assert.Greater(OilMath.SpeedMul(OilMath.Layer.Gas), OilMath.SpeedMul(OilMath.Layer.Soil));
        }

        [Test]
        public void HeldDrillAcceleratesUpToTheLayerCap()
        {
            float depth = 0f, speed = 0f;
            for (int i = 0; i < 100; i++) OilMath.StepDrill(ref depth, ref speed, true, 0.35f, 0.1f);
            Assert.AreEqual(OilMath.MaxSpeed * 0.35f, speed, 1e-4f);
            Assert.Greater(depth, 0f);
        }

        [Test]
        public void ReleasedDrillKeepsMovingForAWhile()
        {
            float depth = 0.2f, speed = OilMath.MaxSpeed;
            OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.1f);
            Assert.Greater(speed, 0f);
            Assert.Greater(depth, 0.2f);
            for (int i = 0; i < 100; i++) OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.1f);
            Assert.AreEqual(0f, speed, 1e-6f);
        }

        [Test]
        public void CoastingFromGasSpeedOvershootsTheOilBand()
        {
            // Releasing right at the top of the oil band at full gas speed slides past even the thickest (9 %) band.
            float depth = 0f, speed = OilMath.MaxSpeed * OilMath.SpeedMul(OilMath.Layer.Gas);
            while (speed > 0f) OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.02f);
            Assert.Greater(depth, 0.09f);
        }

        [Test]
        public void FastDrillingInGasBuildsPressure()
        {
            Assert.AreEqual(0f, OilMath.StepGasPressure(0f, OilMath.SafeGasSpeed * 0.5f, true, 1f), 1e-5f);
            float p = 0f;
            for (int i = 0; i < 20; i++) p = OilMath.StepGasPressure(p, OilMath.MaxSpeed * 1.2f, true, 0.05f);
            Assert.AreEqual(1f, p, 1e-4f); // one second of full speed in gas = blowout
            Assert.Less(OilMath.StepGasPressure(0.5f, OilMath.MaxSpeed, false, 1f), 0.5f);
        }

        [Test]
        public void DrillOutcomes()
        {
            Assert.AreEqual(OilMath.DrillOutcome.Blowout, OilMath.CheckDrill(OilMath.Layer.Gas, 0.1f, true, 1f));
            Assert.AreEqual(OilMath.DrillOutcome.HitWater, OilMath.CheckDrill(OilMath.Layer.Brine, 0.1f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.FoundOil, OilMath.CheckDrill(OilMath.Layer.Oil, 0f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Oil, 0f, true, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Oil, 0.1f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Rock, 0f, false, 0f));
        }

        [Test]
        public void StrokeTiming()
        {
            float b = OilMath.Beat;
            Assert.AreEqual(OilMath.Stroke.Perfect, OilMath.JudgeStroke(b, b));
            Assert.AreEqual(OilMath.Stroke.Perfect, OilMath.JudgeStroke(b - 0.19f, b));
            Assert.AreEqual(OilMath.Stroke.Early, OilMath.JudgeStroke(b - 0.3f, b));
            Assert.AreEqual(OilMath.Stroke.Late, OilMath.JudgeStroke(b + 0.3f, b));
            Assert.AreEqual(OilMath.Stroke.Late, OilMath.JudgeStroke(float.PositiveInfinity, b)); // first stroke only starts the rhythm
        }

        [Test]
        public void TenPerfectStrokesFillTheDrumAndFourEarlyOnesBlowIt()
        {
            float drum = 0f, pressure = 0f;
            for (int i = 0; i < 10; i++) OilMath.ApplyStroke(OilMath.Stroke.Perfect, ref drum, ref pressure);
            Assert.AreEqual(1f, drum, 1e-6f);
            Assert.AreEqual(0f, pressure, 1e-6f);
            drum = 0f;
            for (int i = 0; i < 4; i++) OilMath.ApplyStroke(OilMath.Stroke.Early, ref drum, ref pressure);
            Assert.AreEqual(1f, pressure, 1e-6f);
            Assert.AreEqual(0f, drum, 1e-6f);
            float d = 0.3f, p = 0f;
            OilMath.ApplyStroke(OilMath.Stroke.Late, ref d, ref p);
            Assert.AreEqual(0.3f, d, 1e-6f);
        }

        [Test]
        public void IdlePumpStallsAfterTwoSecondsAndPressureRelaxes()
        {
            float drum = 0.5f, pressure = 0.5f;
            OilMath.StepPumpIdle(ref drum, ref pressure, 1f, 1f);
            Assert.AreEqual(0.5f, drum, 1e-6f);
            Assert.Less(pressure, 0.5f);
            OilMath.StepPumpIdle(ref drum, ref pressure, 3f, 1f);
            Assert.Less(drum, 0.5f);
        }

        [Test]
        public void TempoPicksUpNearlyFull()
        {
            Assert.AreEqual(OilMath.Beat, OilMath.BeatFor(0.5f), 1e-6f);
            Assert.AreEqual(OilMath.Beat * 0.85f, OilMath.BeatFor(0.8f), 1e-6f);
        }

        [Test]
        public void PerformanceRewardsSpeedAndRhythm()
        {
            Assert.Greater(OilMath.Performance(OilMath.ParSec, 10, 10), OilMath.Performance(OilMath.ParSec * 3f, 3, 10));
            Assert.That(OilMath.Performance(0f, 0, 0), Is.InRange(0f, 1f));
        }
    }
}
