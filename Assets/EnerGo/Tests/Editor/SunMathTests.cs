using NUnit.Framework;

namespace EnerGo.Tests
{
    public class SunMathTests
    {
        [Test]
        public void TimeLimitIsFifteenToTwentyFiveSeconds()
        {
            Assert.AreEqual(15f, SunMath.SunTimeLimit(0f), 1e-4f);
            Assert.AreEqual(25f, SunMath.SunTimeLimit(1f), 1e-4f);
            Assert.AreEqual(20f, SunMath.SunTimeLimit(0.5f), 1e-4f);
        }

        [Test]
        public void Outcomes()
        {
            Assert.AreEqual(SunMath.Outcome.Captured, SunMath.SunOutcome(1f, 5f));    // full bar ends early
            Assert.AreEqual(SunMath.Outcome.None, SunMath.SunOutcome(0.7f, 3f));      // still running
            Assert.AreEqual(SunMath.Outcome.Captured, SunMath.SunOutcome(0.5f, 0f));  // time up with exactly half
            Assert.AreEqual(SunMath.Outcome.TooLittle, SunMath.SunOutcome(0.49f, 0f));
            Assert.AreEqual(SunMath.Outcome.TooLittle, SunMath.SunOutcome(0.2f, -1f));
        }

        [Test]
        public void PerformanceMatchesBonusTiers()
        {
            // EnerGoProgress.BonusFor: >= 0.75 = Hebat (+2), >= 0.4 = Bagus (+1).
            Assert.AreEqual(0f, SunMath.SunPerformance(0.5f), 1e-4f);
            Assert.AreEqual(0.4f, SunMath.SunPerformance(0.7f), 1e-4f);
            Assert.AreEqual(0.75f, SunMath.SunPerformance(0.875f), 1e-4f);
            Assert.AreEqual(1f, SunMath.SunPerformance(1f), 1e-4f);
        }
    }
}
