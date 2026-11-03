using UnityEngine;

namespace EnerGo
{
    // The 3D gold pan seen from above while panning. It only shows the existing pan state
    // (goldTilt, goldProgress, goldPos, goldLost); the game logic stays in UpdateGoldPan.
    public partial class ARCapturePrototype
    {
        private const float DulangDistance = 0.6f;    // metres in front of the camera
        private const float DulangRimRadius = 0.255f; // sda_dulang, model units
        private const float DulangInnerRadius = 0.2f; // where gold may sit on the sand
        private const float DulangSandTop = 0.028f;   // height of the sand bed's crown
        private const float DulangSandLift = 0.014f;  // a full bed sits this much higher, so it covers the pan floor

        private Transform dulang, dulangTilt, dulangAir, dulangPasir;
        private Transform[] dulangPieces;
        private Vector3 dulangAirHome, dulangPasirScale;

        private void LateUpdate()
        {
            bool show = goldPanning && arCamera != null && EnerGoProgress.Current.pendingCapture == null;
            if (!show) { if (dulang != null) { Destroy(dulang.gameObject); dulang = null; } return; }
            if (dulang == null && !BuildDulang()) return;
            PoseDulang();
        }

        private bool BuildDulang()
        {
            var panModel = Resources.Load<GameObject>("Gold/sda_dulang");
            var big = Resources.Load<GameObject>("Gold/sda_emas_keping_besar");
            var small = Resources.Load<GameObject>("Gold/sda_emas_keping_kecil");
            if (panModel == null || big == null || small == null)
            {
                Debug.LogError("EnerGo: missing Resources/Gold/sda_dulang or sda_emas_keping_*.");
                return false;
            }
            EnsureModelLight();
            dulang = new GameObject("Dulang 3D").transform;
            dulang.SetParent(arCamera.transform, false);
            dulangTilt = Instantiate(panModel, dulang).transform;
            dulangAir = dulangTilt.Find("sda_dulang_air");
            dulangPasir = dulangTilt.Find("sda_dulang_pasir");
            if (dulangAir != null) dulangAirHome = dulangAir.localPosition;
            if (dulangPasir != null) dulangPasirScale = Vector3.Scale(dulangPasir.localScale, new Vector3(1.45f, 1f, 1.45f)); // fill the pan floor

            dulangPieces = new Transform[GoldPieces];
            for (int i = 0; i < GoldPieces; i++)
            {
                // Same split as goldSize: the first four are nuggets, the rest flakes.
                var p = Instantiate(i < 4 ? big : small, dulangTilt).transform;
                p.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                dulangPieces[i] = p;
            }
            return true;
        }

        // Nuggets and flakes are drawn larger than life so they read at phone size.
        private float PieceScale(int i) => 1.6f * goldSize[i] / (i < 4 ? 7.5f : 4f);

        private void PoseDulang()
        {
            // Same footprint as the 2D pan had (GoldPanRect), so the HUD pill and overlays still line up.
            float sw = Screen.width / HudScale, sh = Screen.height / HudScale;
            GoldPanRect(sw, sh, out Vector2 c, out float R);
            var px = new Vector3(c.x * HudScale, Screen.height - c.y * HudScale, DulangDistance);
            dulang.position = arCamera.ScreenToWorldPoint(px);
            float worldR = DulangPose.WorldRadius(R * HudScale, DulangDistance, arCamera.fieldOfView, Screen.height);
            dulang.localScale = Vector3.one * (worldR / DulangRimRadius);
            // Pan-local x = screen right, z = screen up, y = toward the viewer.
            dulang.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.back);

            // Light the pan from behind the viewer's shoulder whatever way the camera points.
            modelLight.transform.rotation = arCamera.transform.rotation * Quaternion.Euler(55f, -40f, 0f); // grazing enough to shade the bowl

            // Screen-space tilt (x right, y up) from the sensor; the low side sinks away from the viewer.
            var tilt = new Vector2(goldTilt.x, goldTilt.y);
            var tiltRot = DulangPose.Tilt(tilt);
            dulangTilt.localRotation = tiltRot;

            // Water half-levels and runs to the low side (fully level would poke over the low rim at 22°).
            Vector2 slosh = Vector2.ClampMagnitude(tilt * GoldSloshGain, 0.9f);
            if (dulangAir != null)
            {
                dulangAir.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(tiltRot), 0.45f);
                dulangAir.localPosition = dulangAirHome + new Vector3(slosh.x, 0f, slosh.y) * 0.045f;
            }

            // The sand bed shrinks and thins as it washes out, sliding a little downhill.
            float left = 1f - goldProgress;
            if (dulangPasir != null)
            {
                dulangPasir.gameObject.SetActive(left > 0.02f);
                float k = Mathf.Sqrt(Mathf.Max(left, 0.02f));
                dulangPasir.localScale = new Vector3(dulangPasirScale.x * k, dulangPasirScale.y * Mathf.Lerp(0.3f, 1f, left), dulangPasirScale.z * k);
                dulangPasir.localPosition = new Vector3(slosh.x * 0.03f, DulangSandLift * left, slosh.y * 0.03f);
            }

            for (int i = 0; i < GoldPieces; i++)
            {
                var p = dulangPieces[i];
                // goldPos is in GUI space (y down); pan-local z is screen up.
                Vector2 g = goldPos[i];
                var home = new Vector3(g.x, 0f, -g.y) * DulangInnerRadius;
                if (!goldLost[i])
                {
                    p.gameObject.SetActive(true);
                    // Sits just under the sand crown, so washing the sand away is what reveals it.
                    float y = Mathf.Lerp(0.014f, DulangSandTop, 1f - home.magnitude / DulangInnerRadius);
                    p.localPosition = home + Vector3.up * (y + 0.002f * Mathf.Sin(Time.time * 5f + i));
                    p.localScale = Vector3.one * PieceScale(i);
                    continue;
                }
                // Spilled: arcs over the rim toward the viewer and shrinks away.
                float t = (Time.time - goldLostAt[i]) / 0.6f;
                if (t >= 1f) { p.gameObject.SetActive(false); continue; }
                Vector2 d = goldLostDir[i];
                var outward = new Vector3(d.x, 0f, -d.y) * DulangRimRadius * (0.8f + t * 0.9f);
                p.localPosition = outward + Vector3.up * (0.25f * t - 0.15f * t * t);
                p.localScale = Vector3.one * PieceScale(i) * (1f - t);
                p.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
            }
        }
    }
}
