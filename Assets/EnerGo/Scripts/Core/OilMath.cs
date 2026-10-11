using UnityEngine;

namespace EnerGo
{
    // Pure rules of the oil "Bor & Pompa" mini-game. Depth runs 0 (surface) .. 1 (bottom of the cross-section).
    public static class OilMath
    {
        public enum Layer { Soil, Rock, CapRock, Gas, Oil, Brine }
        public enum Stroke { Perfect, Early, Late }
        public enum DrillOutcome { None, FoundOil, Blowout, HitWater }

        // Drill
        public const float MaxSpeed = 0.22f;          // depth per second at x1 layer speed
        public const float Accel = 0.35f;             // speed gained per second while held
        public const float Brake = 0.30f;             // speed lost per second once released (momentum)
        public const float StopSpeed = 0.004f;        // slower than this counts as stopped
        public const float SafeGasSpeed = 0.08f;      // gas pressure only builds above this speed
        public const float GasPressureGain = 9f;      // pressure per second per unit of speed above safe
        public const float GasPressureRelief = 0.25f; // per second when slow or outside the gas
        public const float DisplayDepthM = 2000f;     // depth 1.0 shown as metres
        // Pump
        public const float Beat = 1.2f;               // seconds per stroke
        public const float FastBeatAbove = 0.7f, FastBeatMul = 0.85f;
        public const float PerfectWindow = 0.2f;
        public const float DrumPerPerfect = 0.1f;
        public const float EarlyPressure = 0.25f;
        public const float StallAfterSec = 2f, StallDrainPerSec = 1f / 20f;
        public const float PumpPressureRelief = 0.1f;
        public const float ParSec = 25f;              // drill + pump, for the speed score
        public const float FailShowSec = 1.5f;

        // Bottoms of Soil, Rock, CapRock, Gas, Oil; everything deeper is brine.
        public static float[] Layers(int seed)
        {
            var rng = new System.Random(seed);
            float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);
            float soil = R(0.08f, 0.14f);
            float rock = soil + R(0.18f, 0.28f);
            float cap = rock + R(0.10f, 0.16f);
            float gas = cap + R(0.08f, 0.14f);
            float oil = gas + R(0.06f, 0.09f);
            return new[] { soil, rock, cap, gas, oil };
        }

        public static Layer LayerAt(float depth, float[] bottoms)
        {
            for (int i = 0; i < bottoms.Length; i++)
                if (depth < bottoms[i]) return (Layer)i;
            return Layer.Brine;
        }

        public static float SpeedMul(Layer layer)
        {
            switch (layer)
            {
                case Layer.Rock: return 0.7f;
                case Layer.CapRock: return 0.35f;
                case Layer.Gas: return 1.2f;
                case Layer.Oil: return 0.8f;
                default: return 1f;
            }
        }

        public static void StepDrill(ref float depth, ref float speed, bool held, float layerMul, float dt)
        {
            speed = held ? Mathf.MoveTowards(speed, MaxSpeed * layerMul, Accel * dt) : Mathf.MoveTowards(speed, 0f, Brake * dt);
            depth = Mathf.Clamp01(depth + speed * dt);
        }

        public static float StepGasPressure(float pressure, float speed, bool inGas, float dt)
        {
            float rate = inGas && speed > SafeGasSpeed ? (speed - SafeGasSpeed) * GasPressureGain : -GasPressureRelief;
            return Mathf.Clamp01(pressure + rate * dt);
        }

        public static DrillOutcome CheckDrill(Layer layer, float speed, bool held, float gasPressure)
        {
            if (gasPressure >= 1f) return DrillOutcome.Blowout;
            if (layer == Layer.Brine) return DrillOutcome.HitWater;
            if (layer == Layer.Oil && !held && speed <= StopSpeed) return DrillOutcome.FoundOil;
            return DrillOutcome.None;
        }

        public static float BeatFor(float drum) => drum >= FastBeatAbove ? Beat * FastBeatMul : Beat;

        // `interval` = time since the previous stroke (infinity for the first one).
        public static Stroke JudgeStroke(float interval, float beat) =>
            interval < beat - PerfectWindow ? Stroke.Early : interval > beat + PerfectWindow ? Stroke.Late : Stroke.Perfect;

        public static void ApplyStroke(Stroke stroke, ref float drum, ref float pressure)
        {
            if (stroke == Stroke.Perfect) drum = Mathf.Clamp01(drum + DrumPerPerfect);
            else if (stroke == Stroke.Early) pressure = Mathf.Clamp01(pressure + EarlyPressure);
        }

        public static void StepPumpIdle(ref float drum, ref float pressure, float sinceStroke, float dt)
        {
            pressure = Mathf.Clamp01(pressure - PumpPressureRelief * dt);
            if (sinceStroke > StallAfterSec) drum = Mathf.Clamp01(drum - StallDrainPerSec * dt);
        }

        // Half speed, half share of perfect strokes.
        public static float Performance(float elapsed, int perfects, int strokes) =>
            Mathf.Clamp01(0.5f * Mathf.InverseLerp(ParSec * 2.5f, ParSec, elapsed) + 0.5f * (strokes > 0 ? (float)perfects / strokes : 0f));
    }
}
