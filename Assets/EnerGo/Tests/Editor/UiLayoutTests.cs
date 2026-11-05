using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class UiLayoutTests
    {
        [Test]
        public void SafeAreaKeepsTheMinimumMargins()
        {
            Assert.AreEqual(20f, UiLayout.SafeBottom(0f, 2.25f));
            Assert.AreEqual(90f / 2.25f + 16f, UiLayout.SafeBottom(90f, 2.25f), 1e-3f);
        }

        [Test]
        public void TotalUnitsToleratesMissingAndNegative()
        {
            Assert.AreEqual(0, UiLayout.TotalUnits(null));
            Assert.AreEqual(17, UiLayout.TotalUnits(new[] { 12, 5, -3, 0 }));
        }

        [Test]
        public void ResultTitleAndStars()
        {
            Assert.AreEqual("SEMPURNA!", UiLayout.ResultTitle(3, 3));
            Assert.AreEqual("BAGUS!", UiLayout.ResultTitle(2, 3));
            Assert.AreEqual("COBA LAGI", UiLayout.ResultTitle(0, 3));
            Assert.AreEqual(3, UiLayout.Stars(5));
            Assert.AreEqual(0, UiLayout.Stars(-1));
        }

        [Test]
        public void BottomInsetIsTheUnsafeStripInPanelUnits()
        {
            // 1080 px phone with a 96 px gesture bar, panel 480 units wide.
            Assert.AreEqual(42.667f, UiLayout.PanelBottomInset(96f, 1080f, 480f), 0.001f);
            Assert.AreEqual(0f, UiLayout.PanelBottomInset(0f, 1080f, 480f));
            Assert.AreEqual(0f, UiLayout.PanelBottomInset(96f, 0f, 480f));
        }
    }
}
