using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GuidePagerTests
    {
        [Test]
        public void StartsOnTheFirstStep()
        {
            var p = new GuidePager(3);
            Assert.AreEqual(0, p.Page);
            Assert.AreEqual("LANGKAH 1/3", p.StepLabel);
            Assert.AreEqual("LANJUT", p.PrimaryLabel);
            Assert.IsFalse(p.IsLast);
        }

        [Test]
        public void LastStepClosesInsteadOfAdvancing()
        {
            var p = new GuidePager(3);
            Assert.IsTrue(p.Next());
            Assert.IsTrue(p.Next());
            Assert.IsTrue(p.IsLast);
            Assert.AreEqual("TUTUP", p.PrimaryLabel);
            Assert.AreEqual("LANGKAH 3/3", p.StepLabel);
            Assert.IsFalse(p.Next());
            Assert.AreEqual(2, p.Page);
        }

        [Test]
        public void PrevStopsAtTheFirstStep()
        {
            var p = new GuidePager(2);
            p.Prev();
            Assert.AreEqual(0, p.Page);
            p.Next(); p.Prev();
            Assert.AreEqual(0, p.Page);
        }
    }
}
