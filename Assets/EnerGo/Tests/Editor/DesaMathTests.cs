using System;
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class DesaMathTests
    {
        const long Now = 1000000;

        [Test]
        public void RemainingNeverNegativeAndNeverAboveCapacity()
        {
            Assert.AreEqual(0, DesaMath.Remaining(Now - 5, Now));
            Assert.AreEqual(3600, DesaMath.Remaining(Now + 3600, Now));
            Assert.AreEqual(86400, DesaMath.Remaining(Now + 30 * 3600, Now)); // clock moved back
        }

        [Test]
        public void StatusBoundaries()
        {
            Assert.AreEqual(DesaStatus.Padam, DesaMath.StatusOf(0));
            Assert.AreEqual(DesaStatus.Menipis, DesaMath.StatusOf(1));
            Assert.AreEqual(DesaStatus.Menipis, DesaMath.StatusOf(10799));
            Assert.AreEqual(DesaStatus.Menyala, DesaMath.StatusOf(10800));
        }

        [Test]
        public void SecondsPerUnitMatchesTheSpec()
        {
            Assert.AreEqual(1200, DesaMath.SecondsPerUnit(ResourceIds.Sun));
            Assert.AreEqual(1200, DesaMath.SecondsPerUnit(ResourceIds.Coal));
            Assert.AreEqual(1800, DesaMath.SecondsPerUnit(ResourceIds.Gold));
            Assert.AreEqual(2700, DesaMath.SecondsPerUnit(ResourceIds.Oil));
            Assert.AreEqual(10800, DesaMath.SecondsPerUnit(ResourceIds.Uranium));
            Assert.Throws<ArgumentException>(() => DesaMath.SecondsPerUnit("x"));
        }

        [Test]
        public void FillFromEmptyStartsNowAndFromStockExtends()
        {
            Assert.AreEqual(Now + 2400, DesaMath.EndsAfterFill(Now - 99, Now, 1200, 2));
            Assert.AreEqual(Now + 500 + 1200, DesaMath.EndsAfterFill(Now + 500, Now, 1200, 1));
        }

        [Test]
        public void UnitsThatDoNotFitAreRefused()
        {
            Assert.AreEqual(0, DesaMath.UnitsThatFit(22 * 3600, 10800, 5)); // uranium at 22 h
            Assert.AreEqual(1, DesaMath.UnitsThatFit(21 * 3600, 10800, 5)); // exactly reaches 24 h
            Assert.AreEqual(2, DesaMath.UnitsThatFit(0, 10800, 2));         // limited by what is owned
            Assert.AreEqual(0, DesaMath.UnitsThatFit(86400, 1200, 9));
            Assert.AreEqual(0, DesaMath.UnitsThatFit(0, 1200, 0));
        }

        [Test]
        public void SpendUsesNormalBeforePure()
        {
            int normal = 2, pure = 3;
            DesaMath.Spend(ref normal, ref pure, 3);
            Assert.AreEqual(0, normal);
            Assert.AreEqual(2, pure);
        }

        [Test]
        public void CleanNameTrimsLimitsAndFallsBack()
        {
            Assert.AreEqual("Desa Teknik", DesaMath.CleanName("   "));
            Assert.AreEqual("Desa Teknik", DesaMath.CleanName(null));
            Assert.AreEqual("Sukamaju", DesaMath.CleanName("  Sukamaju "));
            Assert.AreEqual(16, DesaMath.CleanName(new string('a', 40)).Length);
            Assert.AreEqual("a b", DesaMath.CleanName("a\nb"));
        }

        [Test]
        public void PracticeAlwaysPaysOneAndRadarUsesTheFormula()
        {
            Assert.AreEqual(1, DesaMath.CaptureReward(false, 3, 2));
            Assert.AreEqual(6, DesaMath.CaptureReward(true, 3, 2));
            Assert.AreEqual(1, DesaMath.CaptureReward(true, 0, 0));
        }

        [Test]
        public void PrologNeverBlocksAPlayerWhoseSaveNeedsAReset()
        {
            Assert.IsTrue(DesaMath.PrologNeeded(false, false));
            Assert.IsFalse(DesaMath.PrologNeeded(true, false));
            Assert.IsFalse(DesaMath.PrologNeeded(false, true)); // corrupt save: saving is locked until reset, which lives in the lobby
        }

        [Test]
        public void CoverPointCropsTheLongerSide()
        {
            // A 9:16 image in a taller 480 x 1067 box scales by height and crops the sides.
            var mid = DesaMath.CoverPoint(1080, 1920, 480, 1067, .5f, .5f);
            Assert.AreEqual(240f, mid.x, .5f);
            Assert.AreEqual(533.5f, mid.y, .5f);
            Assert.AreEqual(-60f, DesaMath.CoverPoint(1080, 1920, 480, 1067, 0f, 0f).x, 1f);
        }
    }
}
