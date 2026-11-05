using UnityEngine;

namespace EnerGo
{
    // Pump jack mechanism in the sda_pompa model's units: the crank drives a fixed-length pitman, which rocks the
    // beam, which lifts the rod. x = model X (horse head on -X, crank on +X), y = up. Kept free of scene objects.
    public static class PompaPose
    {
        public const float PitmanLength = 1.198f;
        public const float RodLength = 1.10f;       // sda_pompa_tali below the carrier bar
        public const float StuffingBoxTop = 0.72f;  // top of the wellhead the rod slides through

        static readonly Vector2 Pivot = new Vector2(0f, 2f);
        static readonly Vector2 Shaft = new Vector2(1.05f, 0.72f);
        static readonly Vector2 EqualizerOnBeam = new Vector2(1.05f, -0.14f);
        const float CrankRadius = 0.30f;
        const float HeadRadius = 1.155f;   // the arc the cables wrap around; centred on the pivot
        const float CarrierRestY = 1.50f;
        const float MaxBeamRad = 0.6f;

        public struct Pose
        {
            public Vector2 Pin, Equalizer;
            public float HeadDownDeg;   // beam: rotation about model Z (positive lifts the crank end, drops the head)
            public float CrankDeg;      // crank: rotation about model Z
            public float PitmanDeg;     // pitman: rotation about model Z, pivoting on Pin
            public float CarrierY;      // carrier bar and rod height
            public float CableLength;   // from the head's arc down to the carrier bar
        }

        public static Pose At(float crankRad)
        {
            var pin = Shaft + CrankRadius * new Vector2(-Mathf.Cos(crankRad), Mathf.Sin(crankRad));
            // Bisect the beam angle at which the equalizer sits one pitman length from the crank pin.
            float lo = -MaxBeamRad, hi = MaxBeamRad;
            for (int i = 0; i < 30; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Vector2.Distance(EqualizerAt(mid), pin) > PitmanLength) hi = mid; else lo = mid;
            }
            float beam = (lo + hi) * 0.5f;
            var eq = EqualizerAt(beam);
            float carrier = CarrierRestY - HeadRadius * beam; // the rod moves by the arc length the head turned through
            return new Pose
            {
                Pin = pin,
                Equalizer = eq,
                HeadDownDeg = beam * Mathf.Rad2Deg,
                CrankDeg = -crankRad * Mathf.Rad2Deg,
                PitmanDeg = Mathf.Atan2(-(eq.x - pin.x), eq.y - pin.y) * Mathf.Rad2Deg,
                CarrierY = carrier,
                CableLength = Pivot.y - carrier
            };
        }

        static Vector2 EqualizerAt(float beamRad)
        {
            float c = Mathf.Cos(beamRad), s = Mathf.Sin(beamRad);
            return Pivot + new Vector2(EqualizerOnBeam.x * c - EqualizerOnBeam.y * s, EqualizerOnBeam.x * s + EqualizerOnBeam.y * c);
        }

        // The crank only ever turns forward: a press takes it to the next head-down point, a release to the next head-up.
        public static float NextCrankTarget(float current, bool pressed)
        {
            float point = pressed ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;
            float turns = Mathf.Ceil((current - point + 0.0001f) / (2f * Mathf.PI));
            return point + turns * 2f * Mathf.PI;
        }
    }
}
