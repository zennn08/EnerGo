using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class UraniumVisualTests
    {
        [Test]
        public void FragmentsOnlyPopInAfterTheOreHasBroken()
        {
            Assert.AreEqual(0f, UraniumVisual.FragmentPop(UraniumVisual.BreakSec - 0.01f), 1e-4f);
            Assert.AreEqual(1f, UraniumVisual.FragmentPop(UraniumVisual.BreakSec + 1f), 1e-4f);
        }

        [Test]
        public void OreShakesHarderUntilTheBreak()
        {
            Assert.That(UraniumVisual.OreShake(UraniumVisual.BreakSec * 0.9f), Is.GreaterThan(UraniumVisual.OreShake(UraniumVisual.BreakSec * 0.2f)));
        }

        [Test]
        public void ChunksFlyOutThenFallAndShrinkAway()
        {
            var v = new Vector3(0.5f, 0.4f, 0f);
            Assert.That(UraniumVisual.ChunkOffset(v, 0.2f).y, Is.GreaterThan(0f));
            Assert.That(UraniumVisual.ChunkOffset(v, 1.5f).y, Is.LessThan(UraniumVisual.ChunkOffset(v, 0.2f).y));
            Assert.AreEqual(1f, UraniumVisual.ChunkScale(0f), 1e-4f);
            Assert.AreEqual(0f, UraniumVisual.ChunkScale(UraniumVisual.ChunkLifeSec), 1e-4f);
        }

        [Test]
        public void LidDropsOntoTheRimAndStays()
        {
            Assert.That(UraniumVisual.LidY(0f), Is.GreaterThan(UraniumVisual.LidRestY + 0.2f));
            Assert.AreEqual(UraniumVisual.LidRestY, UraniumVisual.LidY(UraniumVisual.LidDropSec + 2f), 1e-3f);
        }

        [Test]
        public void FlaskBumpFadesAfterAFragmentLands()
        {
            Assert.AreEqual(0f, UraniumVisual.FlaskBump(float.NegativeInfinity), 1e-4f);
            Assert.AreEqual(0f, UraniumVisual.FlaskBump(5f), 1e-3f);
        }
    }
}
