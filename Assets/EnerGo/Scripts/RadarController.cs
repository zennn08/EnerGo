using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.EnhancedTouch;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Exploration screen: SDA around the player on a heading-up radar. Walk within range of one and tap TANGKAP
    // to open its capture mini-game; the spawn id travels along so the reward also removes it from the radar.
    [RequireComponent(typeof(LocationFeed))]
    public class RadarController : MonoBehaviour
    {
        private const double RangeM = 150.0;
        private const float RefreshSec = 0.5f;
        private const float WeakGpsM = 30f;
        private const string PrefSafetySeen = "EnerGo.RadarSafetySeen";

        private LocationFeed feed;
        private List<GeoSpawner.Spawn> nearby = new List<GeoSpawner.Spawn>();
        private readonly GeoSpawner.RangeAlert rangeAlert = new GeoSpawner.RangeAlert(); // one buzz when a spawn comes into range
        private float nextRefresh;
        private string selectedId;
        private bool safetySeen;
        private GUIStyle sBody, sMuted, sSmall, sSmallCentered, sInfo, sInfoWarn, sCardTitle;
        private Texture2D beam;

        private float HudScale => Mathf.Max(1f, Mathf.Min(Screen.width / 480f, Screen.height / 800f));
        private float Heading => feed.HasCompass ? feed.HeadingDeg : 0f;

        private void Awake()
        {
            feed = GetComponent<LocationFeed>();
            safetySeen = PlayerPrefs.GetInt(PrefSafetySeen, 0) == 1;
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private static long NowUtc() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Update()
        {
            if (feed.Status != LocationFeed.FeedStatus.Running || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSec;
            Refresh();
        }

        // Public so editor checks can force a refresh (the editor does not tick in play mode).
        public void Refresh()
        {
            var captured = new HashSet<string>(EnerGoProgress.Current.capturedSpawns ?? new string[0]);
            nearby = GeoSpawner.Nearby(feed.Lat, feed.Lon, RangeM, NowUtc(), DateTime.Now.Hour, captured);
            if (nearby.FindIndex(s => s.Id == selectedId) < 0) selectedId = nearby.Count > 0 ? nearby[0].Id : null;
            var inRange = new List<string>();
            foreach (var s in nearby)
                if (ResourceIds.IsPlayable(s.ResourceId) && GeoSpawner.CanCapture(GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon), feed.AccuracyM))
                    inRange.Add(s.Id);
            if (rangeAlert.Update(inRange, Time.unscaledTime)) Haptics.Pulse(60);
        }

        private void InitStyles()
        {
            if (sBody != null) return;
            sBody = Text(16, false, Ink, TextAnchor.MiddleCenter, true);
            sMuted = Text(14, false, Muted, TextAnchor.MiddleLeft);
            sSmall = Text(12, true, Muted, TextAnchor.MiddleLeft);
            sSmallCentered = Text(13, true, Muted);
            sInfo = Text(13, true, Color.white, TextAnchor.MiddleLeft);
            sInfoWarn = Text(13, true, OrangeTop, TextAnchor.MiddleLeft);
            sCardTitle = Text(19, true, Ink, TextAnchor.MiddleLeft);
        }

        private void OnGUI()
        {
            InitStyles();
            var prevMatrix = GUI.matrix;
            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale, sh = Screen.height / scale;
            DrawWorld(sw, sh);

            float top = SafeTop(scale);
            float bottom = SafeBottom(scale);
            float w = Mathf.Min(sw - 40f, 440f), x = (sw - w) * 0.5f;
            bool interactive = safetySeen;

            // Top bar: back to lobby, countdown to the next spawn window.
            var back = new Rect(x, top, 120f, 52f);
            ButtonLabel(DrawSecondaryButton(back, IsHeld(back, scale)), "‹  LOBBY", 18, ButtonKind.Paper);
            if (interactive && TapHit(back, scale))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                SceneManager.LoadScene("LobbyScene");
            }
            long secs = GeoSpawner.SecondsToNextWindow(NowUtc());
            var timer = new Rect(x + w - 190f, top + 4f, 190f, 44f);
            GUI.Label(DrawPill(timer), "SPAWN BARU " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00"), Title(16, Sun));

            var card = new Rect(x, sh - bottom - 156f, w, 148f);
            if (feed.Status == LocationFeed.FeedStatus.Running)
            {
                float radarTop = top + 76f, radarBottom = card.y - 84f;
                float R = Mathf.Min(w, radarBottom - radarTop) * 0.5f;
                var c = new Vector2(sw * 0.5f, (radarTop + radarBottom) * 0.5f);
                DrawRadar(c, R, scale, interactive);

                // Compass and GPS state in one pill above the card; warnings in orange text.
                var info = new Rect(x, card.y - 64f, w, 48f);
                var inner = DrawPill(info);
                GUI.Label(new Rect(inner.x, inner.y + 2f, inner.width, inner.height * 0.5f), feed.HasCompass
                    ? "Kompas: " + feed.HeadingSource + " · " + Mathf.RoundToInt(feed.HeadingDeg) + "°"
                    : "Kompas tidak tersedia, utara di atas", feed.HasCompass ? sInfo : sInfoWarn);
                bool weak = feed.AccuracyM > WeakGpsM;
                GUI.Label(new Rect(inner.x, inner.y + inner.height * 0.5f - 2f, inner.width, inner.height * 0.5f),
                    "GPS ±" + Mathf.RoundToInt(feed.AccuracyM) + " m" + (weak ? " · Sinyal lemah, coba ke tempat terbuka" : ""), weak ? sInfoWarn : sInfo);

                DrawSelected(card, scale, interactive);
            }
            else DrawStatus(card, scale, interactive);

            if (!safetySeen) DrawSafety(sw, sh, scale);
            GUI.matrix = prevMatrix;
        }

        // Screen position of a world bearing/distance on the heading-up radar.
        private Vector2 OnRadar(Vector2 c, float R, double bearing, double dist)
        {
            float a = (float)(bearing - Heading) * Mathf.Deg2Rad;
            float r = Mathf.Clamp01((float)(dist / RangeM)) * (R - 18f);
            return c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * r;
        }

        private static Rect Circle(Vector2 p, float r) => new Rect(p.x - r, p.y - r, r * 2f, r * 2f);

        // White fan (apex at the bottom centre, ±35° wide, fading outward), tinted orange when drawn.
        private Texture2D Beam()
        {
            if (beam != null) return beam;
            const int W = 88, H = 76;
            beam = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x + 0.5f - W * 0.5f, dy = y + 0.5f; // y = 0 is the bottom row (apex)
                float ang = Mathf.Abs(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg), r = Mathf.Sqrt(dx * dx + dy * dy) / H;
                float a = Mathf.Clamp01((35f - ang) / 6f) * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
                beam.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            beam.Apply();
            return beam;
        }

        private void OnDestroy() { if (beam != null) Destroy(beam); }

        // RotateAroundPivot takes the pivot in screen pixels, but this GUI is drawn under a HudScale matrix.
        private void RotateAround(float angle, Vector2 pivot) => GUIUtility.RotateAroundPivot(angle, pivot * HudScale);

        private void DrawRadar(Vector2 c, float R, float scale, bool interactive)
        {
            DrawCard(Circle(c, R), R);
            foreach (double ring in new[] { 50.0, 100.0 })
            {
                float rr = (float)(ring / RangeM) * (R - 18f);
                DrawPanel(Circle(c, rr), Color.clear, Alpha(Ink, 0.15f), rr, 2f);
                GUI.Label(new Rect(c.x + 4f, c.y - rr - 16f, 60f, 14f), ring + " m", sSmall);
            }

            // Sweep line, slowly turning.
            var m = GUI.matrix;
            RotateAround(Time.unscaledTime * 90f % 360f, c);
            DrawPanel(new Rect(c.x - 1f, c.y - R + 4f, 2f, R - 4f), Alpha(Orange, 0.35f), Color.clear, 1f);
            GUI.matrix = m;

            // Compass letters ride the rim and turn with the world (U = utara/north in orange).
            string[] letters = { "U", "T", "S", "B" };
            for (int k = 0; k < 4; k++)
            {
                Vector2 n = OnRadar(c, R, k * 90.0, RangeM);
                n = c + (n - c).normalized * (R - 12f);
                GUI.Label(new Rect(n.x - 12f, n.y - 12f, 24f, 24f), letters[k], k == 0 ? Title(20, Orange) : sSmallCentered);
            }

            foreach (var s in nearby)
            {
                double d = GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon);
                Vector2 p = OnRadar(c, R, GeoSpawner.BearingDeg(feed.Lat, feed.Lon, s.Lat, s.Lon), d);
                bool inRange = GeoSpawner.CanCapture(d, feed.AccuracyM);
                bool locked = !ResourceIds.IsPlayable(s.ResourceId);
                float size = inRange ? 30f : 22f;
                if (s.ResourceId == ResourceIds.Uranium) // rare: pulse a glow so it is noticed
                {
                    float glow = size * (0.9f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f));
                    DrawPanel(Circle(p, glow), Alpha(Resource(s.ResourceId), 0.25f), Color.clear, glow);
                }
                if (s.Id == selectedId) DrawPanel(Circle(p, size * 0.75f), Color.clear, Ink, size * 0.75f, 2f);
                else if (inRange && !locked) DrawPanel(Circle(p, size * 0.75f), Color.clear, Orange, size * 0.75f, 2.5f);
                DrawIcon(Circle(p, size * 0.5f), ResourceIcon(s.ResourceId));
                if (locked) DrawLock(p + new Vector2(size * 0.4f, size * 0.35f), 9f);
                if (interactive && TapHit(Circle(p, 22f), scale)) selectedId = s.Id;
            }

            // The player: an orange dot with a soft beam pointing up. The radar is heading-up, so "up" is always
            // the way the phone faces and the world turns around the player.
            GUI.color = Alpha(Orange, 0.55f);
            GUI.DrawTexture(new Rect(c.x - 44f, c.y - 76f, 88f, 76f), Beam(), ScaleMode.StretchToFill);
            GUI.color = Color.white;
            DrawPanel(Circle(c + new Vector2(0f, 2f), 13f), Alpha(Ink, 0.15f), Color.clear, 13f);
            DrawPanel(Circle(c, 12f), Paper, Color.clear, 12f);
            DrawPanel(Circle(c, 8.5f), Orange, Color.clear, 8.5f);
        }

        private void DrawSelected(Rect card, float scale, bool interactive)
        {
            DrawCard(card);
            int i = nearby.FindIndex(s => s.Id == selectedId);
            if (i < 0)
            {
                GUI.Label(new Rect(card.x + 20f, card.y, card.width - 40f, card.height), "Belum ada SDA di sekitar. Coba berjalan ke area lain.", sBody);
                return;
            }
            var sel = nearby[i];
            double d = GeoSpawner.DistanceM(feed.Lat, feed.Lon, sel.Lat, sel.Lon);
            double b = GeoSpawner.BearingDeg(feed.Lat, feed.Lon, sel.Lat, sel.Lon);
            bool locked = !ResourceIds.IsPlayable(sel.ResourceId);
            bool inRange = GeoSpawner.CanCapture(d, feed.AccuracyM);

            DrawIcon(new Rect(card.x + 18f, card.y + 14f, 40f, 40f), ResourceIcon(sel.ResourceId));
            GUI.Label(new Rect(card.x + 68f, card.y + 12f, card.width - 120f, 24f), ResourceIds.Name(sel.ResourceId), sCardTitle);
            GUI.Label(new Rect(card.x + 68f, card.y + 36f, card.width - 120f, 20f), Mathf.RoundToInt((float)d) + " m · " + GeoSpawner.Direction8(b), sMuted);
            // Direction hint: an arrow pointing the way to walk (relative to where the phone faces).
            {
                var m = GUI.matrix;
                var pivot = new Vector2(card.xMax - 40f, card.y + 34f);
                RotateAround((float)(b - Heading) - 90f, pivot);
                DrawIcon(new Rect(pivot.x - 20f, pivot.y - 20f, 40f, 40f), "arrow");
                GUI.matrix = m;
            }

            var btn = new Rect(card.x + 16f, card.yMax - 18f - 64f, card.width - 32f, 64f);
            if (locked)
            {
                ButtonLabel(DrawButton(btn, ButtonKind.Off), "SEGERA HADIR", 20, ButtonKind.Off);
                TapHit(btn, scale); // swallow the tap
            }
            else if (inRange)
            {
                ButtonLabel(DrawPrimaryButton(btn, IsHeld(btn, scale)), "TANGKAP", 26, ButtonKind.Orange);
                if (interactive && TapHit(btn, scale)) Capture(sel);
            }
            else
            {
                ButtonLabel(DrawButton(btn, ButtonKind.Off), "DEKATI DULU (" + Mathf.RoundToInt((float)d) + " m)", 20, ButtonKind.Off);
                TapHit(btn, scale);
            }
        }

        private void Capture(GeoSpawner.Spawn s)
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            ResourceIds.Selected = s.ResourceId;
            ExploreSession.PendingSpawnId = s.Id;
            ExploreSession.PendingBearingDeg = (float)GeoSpawner.BearingDeg(feed.Lat, feed.Lon, s.Lat, s.Lon);
            SceneManager.LoadScene(LobbyController.CaptureSceneName());
        }

        private void DrawStatus(Rect card, float scale, bool interactive)
        {
            string title, body;
            switch (feed.Status)
            {
                case LocationFeed.FeedStatus.Denied: title = "Izin lokasi ditolak"; body = "Radar butuh lokasimu untuk menemukan SDA di sekitar."; break;
                case LocationFeed.FeedStatus.Disabled: title = "Lokasi HP mati"; body = "Nyalakan Lokasi di pengaturan HP, lalu coba lagi."; break;
                case LocationFeed.FeedStatus.Failed: title = "GPS tidak merespons"; body = "Coba pindah ke tempat terbuka, lalu coba lagi."; break;
                default:
                    DrawCard(card);
                    GUI.Label(card, "Mencari sinyal GPS...", sBody);
                    return;
            }
            var box = new Rect(card.x, card.y - 170f, card.width, card.height + 170f);
            DrawCard(box);
            DrawRibbon(box, title.ToUpperInvariant(), ButtonKind.Bad);
            GUI.Label(new Rect(box.x + 20f, box.y + 50f, box.width - 40f, 50f), body, sBody);
            var retry = new Rect(box.x + 16f, box.yMax - 18f - 64f - 74f, box.width - 32f, 64f);
            ButtonLabel(DrawPrimaryButton(retry, IsHeld(retry, scale)), "COBA LAGI", 24, ButtonKind.Orange);
            if (interactive && TapHit(retry, scale)) feed.Restart();
            var practice = new Rect(box.x + 16f, box.yMax - 18f - 64f, box.width - 32f, 64f);
            ButtonLabel(DrawButton(practice, ButtonKind.Info, IsHeld(practice, scale)), "MAIN MODE LATIHAN", 20, ButtonKind.Info);
            if (interactive && TapHit(practice, scale))
            {
                LobbyController.OpenPracticeOnLoad = true;
                SceneManager.LoadScene("LobbyScene");
            }
        }

        // One-time safety reminder before the first hunt.
        private void DrawSafety(float sw, float sh, float scale)
        {
            GUI.color = Alpha(Ink, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = Mathf.Min(sw - 40f, 380f);
            var box = new Rect((sw - w) * 0.5f, sh * 0.5f - 130f, w, 270f);
            DrawCard(box);
            DrawRibbon(box, "HATI-HATI DI JALAN", ButtonKind.Orange);
            GUI.Label(new Rect(box.x + 20f, box.y + 52f, w - 40f, 100f),
                "Perhatikan jalan dan sekitarmu saat berburu. Jangan masuk ke area berbahaya atau milik orang lain.", sBody);
            var ok = new Rect(box.x + 16f, box.yMax - 18f - 64f, w - 32f, 64f);
            ButtonLabel(DrawPrimaryButton(ok, IsHeld(ok, scale)), "SAYA MENGERTI", 24, ButtonKind.Orange);
            if (TapHit(ok, scale))
            {
                safetySeen = true;
                PlayerPrefs.SetInt(PrefSafetySeen, 1);
                PlayerPrefs.Save();
            }
        }
    }
}
