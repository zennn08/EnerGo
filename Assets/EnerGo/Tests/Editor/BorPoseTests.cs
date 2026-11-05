using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class BorPoseTests
    {
        static readonly float[] Bottoms = { 0.1f, 0.35f, 0.5f, 0.6f, 0.68f };
        const float Height = 1.28f;

        [Test]
        public void SlabsStackFromTheSurfaceToTheBottomWithoutGaps()
        {
            var s = BorPose.Slabs(Bottoms, Height);
            Assert.AreEqual(6, s.Length);
            Assert.AreEqual(0f, s[0].TopY, 1e-4f);
            Assert.AreEqual(-0.1f * Height, s[1].TopY, 1e-4f);
            Assert.AreEqual(-0.68f * Height, s[5].TopY, 1e-4f);
        }

        [Test]
        public void SlabScaleFitsItsNominalMeshToTheWellThickness()
        {
            var s = BorPose.Slabs(Bottoms, Height);
            for (int i = 0; i < 6; i++)
            {
                float bottom = i < Bottoms.Length ? Bottoms[i] : 1f;
                float top = i == 0 ? 0f : Bottoms[i - 1];
                Assert.AreEqual((bottom - top) * Height, s[i].ScaleY * BorPose.NominalThickness[i], 1e-4f);
            }
        }

        [Test]
        public void BitWaitsInTheDerrickThenFollowsTheDepth()
        {
            Assert.AreEqual(BorPose.StartAbove, BorPose.BitY(0f, Height, 0f), 1e-4f);    // not lowered yet: above ground
            Assert.AreEqual(0f, BorPose.BitY(0f, Height, 1f), 1e-4f);                     // fully lowered at depth 0: on the surface
            Assert.AreEqual(-0.5f * Height, BorPose.BitY(0.5f, Height, 1f), 1e-4f);       // depth maps onto the block
        }

        [Test]
        public void HoleOnlyExistsBelowTheSurfaceAndThePipeAlwaysReachesTheBit()
        {
            Assert.AreEqual(0f, BorPose.HoleLength(0.1f), 1e-4f);
            Assert.AreEqual(0.4f, BorPose.HoleLength(-0.4f), 1e-4f);
            Assert.AreEqual(BorPose.CrownY + 0.4f, BorPose.PipeLength(-0.4f), 1e-4f);
            Assert.AreEqual(BorPose.CrownY - BorPose.StartAbove, BorPose.PipeLength(BorPose.StartAbove), 1e-4f);
        }
    }
}
