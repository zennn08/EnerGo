using NUnit.Framework;

namespace EnerGo.Tests
{
    public class CaptureScreensTests
    {
        [Test]
        public void ScreenFollowsTheSavedCapture()
        {
            Assert.AreEqual(CaptureScreen.Play, CaptureScreens.For(false, null));
            Assert.AreEqual(CaptureScreen.Quiz, CaptureScreens.For(true, "awaiting-analysis"));
            Assert.AreEqual(CaptureScreen.Feedback, CaptureScreens.For(true, "feedback"));
            Assert.AreEqual(CaptureScreen.Result, CaptureScreens.For(true, "result"));
        }

        [Test]
        public void HudFrameOnlyShowsWhilePlaying()
        {
            Assert.IsTrue(CaptureScreens.ShowsHudFrame(CaptureScreen.Play));
            Assert.IsFalse(CaptureScreens.ShowsHudFrame(CaptureScreen.Quiz));
            Assert.IsFalse(CaptureScreens.ShowsHudFrame(CaptureScreen.Result));
        }

        [Test]
        public void CardTiltStopsAt18Degrees()
        {
            Assert.AreEqual(18f, CaptureScreens.CardRotation(1000f), 1e-4f);
            Assert.AreEqual(-18f, CaptureScreens.CardRotation(-1000f), 1e-4f);
            Assert.AreEqual(8f, CaptureScreens.CardRotation(100f), 1e-4f);
        }

        [Test]
        public void SwipeThresholdIs90Units()
        {
            Assert.IsTrue(CaptureScreens.PastThreshold(90f));
            Assert.IsTrue(CaptureScreens.PastThreshold(-90f));
            Assert.IsFalse(CaptureScreens.PastThreshold(89f));
        }

        [Test]
        public void WrongAnswersHoldTheButtonForTwoSeconds()
        {
            Assert.AreEqual(1f, CaptureScreens.ReadHold(true, 0f), 1e-4f);
            Assert.AreEqual(0.5f, CaptureScreens.ReadHold(false, 1f), 1e-4f);
            Assert.AreEqual(1f, CaptureScreens.ReadHold(false, 3f), 1e-4f);
        }

        [Test]
        public void ResultMessageFollowsTheScore()
        {
            Assert.AreEqual("Sempurna! Semua jawaban tepat.", CaptureScreens.ResultMessage(3, 3));
            Assert.AreEqual("Belum ada yang tepat. Coba lagi di tangkapan berikutnya.", CaptureScreens.ResultMessage(0, 3));
            Assert.AreEqual("Bagus! Jawab semua dengan tepat untuk hasil Murni.", CaptureScreens.ResultMessage(1, 3));
        }

        [Test]
        public void AnswerChipUsesOnlyLettersTheFontHas()
        {
            Assert.AreEqual("Pernyataan ini BENAR", CaptureScreens.AnswerChip(true));
            Assert.AreEqual("Pernyataan ini SALAH", CaptureScreens.AnswerChip(false));
        }

        [Test]
        public void PumpRingShrinksFromLastStrokeToTheTarget()
        {
            Assert.AreEqual(2.6f, CaptureScreens.PumpRingScale(0f, 1.2f), 0.001f);
            Assert.AreEqual(1f, CaptureScreens.PumpRingScale(1.2f, 1.2f), 0.001f);
            Assert.AreEqual(1f, CaptureScreens.PumpRingScale(5f, 1.2f), 0.001f);
        }

        [Test]
        public void GaugeWarnsOnlyAboveItsLimit()
        {
            Assert.IsTrue(CaptureScreens.GaugeWarn(0.61f, 0.6f));
            Assert.IsFalse(CaptureScreens.GaugeWarn(0.6f, 0.6f));
        }

        [Test]
        public void UraniumHeatIsTheMeterAsAFraction()
        {
            Assert.AreEqual(0f, CaptureScreens.UraniumHeat(0), 0.001f);
            Assert.AreEqual(0.5f, CaptureScreens.UraniumHeat(50), 0.001f);
            Assert.AreEqual(1f, CaptureScreens.UraniumHeat(140), 0.001f);
        }
    }
}
