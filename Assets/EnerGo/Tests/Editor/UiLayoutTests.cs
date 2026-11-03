using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class UiLayoutTests
    {
        [Test]
        public void SafeAreaKeepsTheMinimumMargins()
        {
            Assert.AreEqual(16f, UiLayout.SafeTop(2340f, 2340f, 2.25f));
            Assert.AreEqual(20f, UiLayout.SafeBottom(0f, 2.25f));
            Assert.AreEqual(100f / 2.25f + 12f, UiLayout.SafeTop(2340f, 2240f, 2.25f), 1e-3f);
            Assert.AreEqual(90f / 2.25f + 16f, UiLayout.SafeBottom(90f, 2.25f), 1e-3f);
        }

        [Test]
        public void GridCentresTheShortLastRow()
        {
            var r = UiLayout.Grid(new Rect(0, 0, 300, 200), 5, 3, 12f, 80f);
            Assert.AreEqual(5, r.Length);
            Assert.AreEqual(r[1].center.x, (r[3].center.x + r[4].center.x) / 2f, 1e-3f); // row 2 centred under row 1
            Assert.AreEqual(r[0].width, r[3].width, 1e-3f);
            Assert.AreEqual(80f + 12f, r[3].y, 1e-3f);
        }

        [Test]
        public void TotalUnitsToleratesMissingAndNegative()
        {
            Assert.AreEqual(0, UiLayout.TotalUnits(null));
            Assert.AreEqual(17, UiLayout.TotalUnits(new[] { 12, 5, -3, 0 }));
        }

        [Test]
        public void FitFontSizeShrinksButNotBelowMin()
        {
            Assert.AreEqual(20, UiLayout.FitFontSize(100f, 20, 200f, 12));
            Assert.AreEqual(10, UiLayout.FitFontSize(400f, 20, 200f, 10));
            Assert.AreEqual(12, UiLayout.FitFontSize(4000f, 20, 200f, 12));
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
