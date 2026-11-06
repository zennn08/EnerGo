using NUnit.Framework;

namespace EnerGo.Tests
{
    public class DesaTextTests
    {
        [Test]
        public void GoldIsNeverCalledAFuel()
        {
            StringAssert.DoesNotContain("bahan bakar", DesaText.SourceOf(ResourceIds.Gold).ToLowerInvariant());
            StringAssert.Contains("kabel", DesaText.SourceOf(ResourceIds.Gold).ToLowerInvariant());
        }

        [Test]
        public void FormatSisaNeverReadsZeroWhileStockIsLeft()
        {
            Assert.AreEqual("5 j 12 m", DesaText.FormatSisa(5 * 3600 + 12 * 60));
            Assert.AreEqual("12 m", DesaText.FormatSisa(12 * 60));
            Assert.AreEqual("1 m", DesaText.FormatSisa(20));
            Assert.AreEqual("0 m", DesaText.FormatSisa(0));
            Assert.AreEqual("24 j", DesaText.FormatSisa(86400));
        }

        [Test]
        public void BackgroundFollowsClockAndStock()
        {
            Assert.AreEqual("desa_siang", DesaText.BackgroundFor(DesaStatus.Padam, true));
            Assert.AreEqual("desa_siang", DesaText.BackgroundFor(DesaStatus.Menyala, true));
            Assert.AreEqual("desa_malam_padam", DesaText.BackgroundFor(DesaStatus.Padam, false));
            Assert.AreEqual("desa_malam_terang", DesaText.BackgroundFor(DesaStatus.Menipis, false));
            Assert.AreEqual("desa_malam_terang", DesaText.BackgroundFor(DesaStatus.Menyala, false));
        }

        [Test]
        public void PoseFilesMatchTheStatus()
        {
            Assert.AreEqual("pemandu_menunjuk", DesaText.PoseFor(DesaStatus.Padam, true, false)); // never filled: points at the plant
            Assert.AreEqual("pemandu_khawatir", DesaText.PoseFor(DesaStatus.Padam, true, true));
            Assert.AreEqual("pemandu_menunjuk", DesaText.PoseFor(DesaStatus.Menipis, true, true));
            Assert.AreEqual("pemandu_melambai", DesaText.PoseFor(DesaStatus.Menyala, true, true));
            Assert.AreEqual("pemandu_santai", DesaText.PoseFor(DesaStatus.Menyala, false, true));
        }

        [Test]
        public void PortraitFollowsTheStatus()
        {
            Assert.AreEqual("pemandu_wajah_netral", DesaText.WajahFor(DesaStatus.Padam, false));
            Assert.AreEqual("pemandu_wajah_khawatir", DesaText.WajahFor(DesaStatus.Padam, true));
            Assert.AreEqual("pemandu_wajah_berpikir", DesaText.WajahFor(DesaStatus.Menipis, true));
            Assert.AreEqual("pemandu_wajah_senang", DesaText.WajahFor(DesaStatus.Menyala, true));
        }

        [Test]
        public void PrologHasFivePanelsAndTheLastUsesTheVillageName()
        {
            Assert.AreEqual(5, DesaText.Prolog.Length);
            StringAssert.Contains("Sya", DesaText.Prolog[2]);
            StringAssert.Contains("{nama}", DesaText.Prolog[4]);
        }
    }
}
