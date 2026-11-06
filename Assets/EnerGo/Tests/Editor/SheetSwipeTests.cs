using NUnit.Framework;

namespace EnerGo.Tests
{
    public class SheetSwipeTests
    {
        [Test]
        public void TheSheetOnlyFollowsTheFingerDownwards()
        {
            Assert.AreEqual(0f, SheetSwipe.Offset(-40f));
            Assert.AreEqual(25f, SheetSwipe.Offset(25f));
        }

        [Test]
        public void AFarDragOrAQuickFlickCloses()
        {
            Assert.IsTrue(SheetSwipe.ShouldClose(160f, 500f, 0f));   // past a quarter of the sheet
            Assert.IsFalse(SheetSwipe.ShouldClose(100f, 500f, 0f));  // a short slow drag springs back
            Assert.IsTrue(SheetSwipe.ShouldClose(40f, 500f, 900f));  // a flick is enough
            Assert.IsFalse(SheetSwipe.ShouldClose(4f, 500f, 900f));  // a tap that twitched is not a flick
            Assert.IsFalse(SheetSwipe.ShouldClose(40f, 500f, -900f)); // flicking back up keeps it open
        }

        [Test]
        public void ADragOnlyStartsPastAShortDistanceSoTapsStayTaps()
        {
            Assert.IsFalse(SheetSwipe.StartsDrag(5f));
            Assert.IsTrue(SheetSwipe.StartsDrag(12f));
            Assert.IsFalse(SheetSwipe.StartsDrag(-30f));
        }
    }
}
