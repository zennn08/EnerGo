using System.Collections.Generic;
using UnityEngine;

namespace EnerGo
{
    // What the mini-game overlays show (EnerGo.UI.CaptureView.Overlay reads this). Rects and points are GUI units, the
    // same ones the 3D props and the input already use, so the drawing stays exactly on top of what is touched.
    public partial class ARCapturePrototype
    {
        public struct CoalPip { public Vector2 ScreenPos; public int Hits, Required; }

        private float GuiW => Screen.width / HudScale;
        private float GuiH => Screen.height / HudScale;

        public float ReticleRadiusGui => ReticleRadius;
        public Vector2 ReticleCentre => new Vector2(GuiW * 0.5f, GuiH * 0.5f);
        public bool SunAiming => resourceId == ResourceIds.Sun && target != null;
        public bool SunLocked => sunInside;
        public float SunProgress => sunProgress;

        public void ReadCoalPips(List<CoalPip> into)
        {
            into.Clear();
            if (resourceId != ResourceIds.Coal || target == null) return;
            foreach (var lump in coalLumps)
            {
                if (lump.root == null) continue;
                Vector3 sp = arCamera.WorldToScreenPoint(lump.root.position + Vector3.up * lump.size * 0.75f);
                if (sp.z <= arCamera.nearClipPlane) continue;
                into.Add(new CoalPip { ScreenPos = sp, Hits = lump.hits, Required = lump.required });
            }
        }

        public GoldPhase GoldPhaseNow => goldPhase;
        public void ReadGoldPan(out Vector2 centre, out float radius) => GoldPanRect(GuiW, GuiH, out centre, out radius);
        public string GoldCountLabel => "EMAS " + GoldLeft() + "/" + GoldPieces;
        public bool GoldFlat => goldFlatHold > 0f;
        public int GoldCountdown => Mathf.Clamp(Mathf.CeilToInt(GoldCountdownSec - (Time.time - goldPhaseAt)), 1, 3);
        public float GoldPhaseAge => Time.time - goldPhaseAt;
        public bool GoldSwirling => Mathf.Abs(goldSwirl) >= DulangMath.MinSwirl && goldTiltMag >= DulangMath.MinTilt;
        public bool GoldRough => goldRough;
        public float? GoldTiltDeg => goldTiltMag >= DulangMath.MinTilt * 0.5f ? Mathf.Atan2(goldTilt.y, goldTilt.x) * Mathf.Rad2Deg : (float?)null;

        public Vector2 GoldBubbleOffset
        {
            get
            {
                ReadGoldPan(out _, out float r);
                return Vector2.ClampMagnitude(goldBubble * r * 1.6f, r * 0.7f);
            }
        }

        public void RetryGoldFromCard()
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            RetryGoldAtNewDeposit();
        }

        public bool OilActive => oilPhase != OilPhase.None;
        // The section stays up a moment after striking oil, so the splash is seen.
        public bool OilShowsPump => oilPumping && !(oilPhase == OilPhase.Pump && Time.time - oilPhaseAt < OilSplashHold);
        public float OilGas => oilGasPressure;
        public int OilLayerCount => OilLayerColors.Length;
        public string OilLayerName(int i) => OilLayerNames[i];
        public const float PumpTargetRadius = 44f;
        public Vector2 PumpCentre => new Vector2(GuiW * 0.5f, GuiH * 0.7f);
        public bool PumpStarted => !float.IsNegativeInfinity(oilLastStrokeAt);
        public float PumpStrokeAge => Time.time - oilLastStrokeAt;
        public float PumpBeat => OilMath.BeatFor(oilDrum);
        public float OilDrum => oilDrum;
        public float PumpPressure => oilPumpPressure;

        public Rect OilSectionGui
        {
            get
            {
                OilSectionRect(GuiW, GuiH, out Rect p);
                return p;
            }
        }

        public Rect OilBandGui(int i)
        {
            Rect p = OilSectionGui;
            float top = i > 0 && i - 1 < oilLayers.Length ? oilLayers[i - 1] : 0f;
            float bottom = i < oilLayers.Length ? oilLayers[i] : 1f;
            return new Rect(p.x, p.y + top * p.height, p.width, (bottom - top) * p.height);
        }

        public bool UraniumAiming => uraniumPhase == UraniumPhase.Search || uraniumPhase == UraniumPhase.Collect;
        public bool UraniumCollecting => uraniumPhase == UraniumPhase.Collect;
        public bool UraniumNear => uraniumAngle <= (uraniumPhase == UraniumPhase.Search ? UraniumMath.RevealAngleDeg : UraniumMath.GripAngleDeg);
        public int UraniumMeter => uraniumMeter;
        public int UraniumTrend => uraniumTrend;
        public float UraniumGrip => uraniumGrip;
        public bool UraniumGripHeld => uraniumGripHeld;
        public float UraniumDose => uraniumDose;
        public string UraniumCount => uraniumCollected + "/" + UraniumMath.FragmentCount;

        public Rect UraniumCloseGui
        {
            get
            {
                UraniumCloseRect(GuiH, out Rect btn);
                return btn;
            }
        }

        public Rect UraniumGripGui
        {
            get
            {
                UraniumGripRect(GuiW, GuiH, out Rect btn);
                return btn;
            }
        }
    }
}
