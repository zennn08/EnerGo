using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GuideBookTests
    {
        static readonly string[] BadGlyphs = { "☢", "←", "→", "✓", "✗", "⚠", "★" };

        [Test]
        public void EveryPlayableResourceHasTwoOrThreeCards()
        {
            foreach (var id in GuideBook.Ids)
            {
                Assert.IsTrue(ResourceIds.IsPlayable(id), id);
                var cards = GuideBook.For(id);
                Assert.That(cards.Length, Is.InRange(2, 3), id);
                foreach (var c in cards)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c.Title), id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c.Body), id);
                    foreach (var g in BadGlyphs) { StringAssert.DoesNotContain(g, c.Title); StringAssert.DoesNotContain(g, c.Body); }
                    foreach (char ch in c.Title + c.Body) Assert.IsFalse(char.IsSurrogate(ch), "emoji in " + id);
                }
            }
            Assert.AreEqual(5, GuideBook.Ids.Length);
        }

        [Test]
        public void UnknownIdHasNoCards() => Assert.AreEqual(0, GuideBook.For("random").Length);
    }
}
