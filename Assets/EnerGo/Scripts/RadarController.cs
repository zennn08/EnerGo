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
        private readonly Dictionary<string, Texture2D> crystals = new Dictionary<string, Texture2D>();
        private GUIStyle sTitle, sBody, sMuted, sSmall, sSmallCentered, sDanger, sPrimary, sSecondary, sCardTitle, sCount, sNorth;
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
            if (sTitle != null) return;
            sTitle = Text(20, true, Ink);
            sBody = Text(14, false, Ink, TextAnchor.MiddleCenter, true);
            sMuted = Text(13, false, Muted, TextAnchor.MiddleLeft);
            sSmall = Text(11, true, Muted, TextAnchor.MiddleLeft);
            sSmallCentered = Text(12, true, Muted);
            sDanger = Text(11, true, Danger, TextAnchor.MiddleLeft);
            sPrimary = Text(17, true, Paper);
            sSecondary = Text(14, true, Muted);
            sCardTitle = Text(17, true, Ink, TextAnchor.MiddleLeft);
            sCount = Text(13, true, Ink);
            sNorth = Text(13, true, Orange);
        }

        private Texture2D Crystal(string id)
        {
            if (!crystals.TryGetValue(id, out var tex) || tex == null) crystals[id] = tex = Resources.Load<Texture2D>("UI/crystal_" + id);
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();
            var prevMatrix = GUI.matrix;
            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale, sh = Screen.height / scale;
            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float top = Mathf.Max(16f, (Screen.height - Screen.safeArea.yMax) / scale + 12f);
            float bottom = Mathf.Max(20f, Screen.safeArea.yMin / scale + 16f);
            float w = Mathf.Min(sw - 40f, 440f), x = (sw - w) * 0.5f;
            bool interactive = safetySeen;

            // Top bar: back to lobby, countdown to the next spawn window.
            var back = new Rect(x, top, 112f, 48f);
            GUI.Label(DrawSecondaryButton(back, IsHeld(back, scale)), "‹  LOBBY", sCount);
            if (interactive && TapHit(back, scale))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                SceneManager.LoadScene("LobbyScene");
            }
            long secs = GeoSpawner.SecondsToNextWindow(NowUtc());
            var timer = new Rect(x + w - 168f, top + 2f, 168f, 40f);
            DrawCard(timer, 20f);
            GUI.Label(timer, "Spawn baru " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00"), sCount);

            var card = new Rect(x, sh - bottom - 136f, w, 136f);
            if (feed.Status == LocationFeed.FeedStatus.Running)
            {
                float radarTop = top + 70f, radarBottom = card.y - 44f;
                float R = Mathf.Min(w, radarBottom - radarTop) * 0.5f;
                var c = new Vector2(sw * 0.5f, (radarTop + radarBottom) * 0.5f);
                DrawRadar(c, R, scale, interactive);

                float infoY = card.y - 34f;
                GUI.Label(new Rect(x, infoY - 18f, w, 18f), feed.HasCompass
                    ? "Kompas: " + feed.HeadingSource + " · " + Mathf.RoundToInt(feed.HeadingDeg) + "°"
                    : "Kompas tidak tersedia, utara di atas", feed.HasCompass ? sSmall : sDanger);
                GUI.Label(new Rect(x, infoY, w, 18f), "GPS ±" + Mathf.RoundToInt(feed.AccuracyM) + " m", sSmall);
                if (feed.AccuracyM > WeakGpsM)
                    GUI.Label(new Rect(x + 80f, infoY, w - 80f, 18f), "Sinyal GPS lemah, coba ke tempat terbuka", sDanger);

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
            DrawPanel(new Rect(c.x - R, c.y - R + 6f, R * 2f, R * 2f), Alpha(Ink, 0.08f), Color.clear, R);
            DrawPanel(Circle(c, R), Paper, Line, R, 2f);
            foreach (double ring in new[] { 50.0, 100.0 })
            {
                float rr = (float)(ring / RangeM) * (R - 18f);
                DrawPanel(Circle(c, rr), Color.clear, Line, rr, 1.5f);
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
                GUI.Label(new Rect(n.x - 10f, n.y - 10f, 20f, 20f), letters[k], k == 0 ? sNorth : sSmallCentered);
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
                var tex = Crystal(s.ResourceId);
                if (tex != null) GUI.DrawTexture(new Rect(p.x - size * 0.43f, p.y - size * 0.5f, size * 0.866f, size), tex, ScaleMode.ScaleToFit);
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
            DrawCard(card, 20f);
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

            var tex = Crystal(sel.ResourceId);
            if (tex != null) GUI.DrawTexture(new Rect(card.x + 18f, card.y + 14f, 30f, 35f), tex, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(card.x + 60f, card.y + 12f, card.width - 80f, 22f), ResourceIds.Name(sel.ResourceId), sCardTitle);
            GUI.Label(new Rect(card.x + 60f, card.y + 34f, card.width - 110f, 18f), Mathf.RoundToInt((float)d) + " m · " + GeoSpawner.Direction8(b), sMuted);
            // Direction hint: a small crystal pointing the way to walk (relative to where the phone faces).
            var arrow = Crystal("random");
            if (arrow != null)
            {
                var m = GUI.matrix;
                var pivot = new Vector2(card.xMax - 34f, card.y + 30f);
                RotateAround((float)(b - Heading) - 90f, pivot);
                GUI.DrawTexture(new Rect(pivot.x - 13f, pivot.y - 8f, 26f, 16f), arrow, ScaleMode.StretchToFill);
                GUI.matrix = m;
            }

            var btn = new Rect(card.x + 16f, card.yMax - 16f - 58f, card.width - 32f, 58f);
            if (locked)
            {
                GUI.Label(DrawSecondaryButton(btn), "SEGERA HADIR", sSecondary);
                TapHit(btn, scale); // swallow the tap
            }
            else if (inRange)
            {
                GUI.Label(DrawPrimaryButton(btn, IsHeld(btn, scale)), "TANGKAP", sPrimary);
                if (interactive && TapHit(btn, scale)) Capture(sel);
            }
            else
            {
                GUI.Label(DrawSecondaryButton(btn), "DEKATI DULU (" + Mathf.RoundToInt((float)d) + " m)", sSecondary);
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
                    DrawCard(card, 20f);
                    GUI.Label(card, "Mencari sinyal GPS...", sBody);
                    return;
            }
            var box = new Rect(card.x, card.y - 150f, card.width, card.height + 150f);
            DrawCard(box, 20f);
            GUI.Label(new Rect(box.x + 20f, box.y + 18f, box.width - 40f, 26f), title, sTitle);
            GUI.Label(new Rect(box.x + 20f, box.y + 48f, box.width - 40f, 44f), body, sBody);
            var retry = new Rect(box.x + 16f, box.yMax - 16f - 58f - 66f, box.width - 32f, 58f);
            GUI.Label(DrawPrimaryButton(retry, IsHeld(retry, scale)), "COBA LAGI", sPrimary);
            if (interactive && TapHit(retry, scale)) feed.Restart();
            var practice = new Rect(box.x + 16f, box.yMax - 16f - 58f, box.width - 32f, 58f);
            GUI.Label(DrawSecondaryButton(practice, IsHeld(practice, scale)), "MAIN MODE LATIHAN", new GUIStyle(sSecondary) { normal = { textColor = Ink } });
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
            var box = new Rect((sw - w) * 0.5f, sh * 0.5f - 130f, w, 260f);
            DrawCard(box, 20f);
            GUI.Label(new Rect(box.x + 20f, box.y + 22f, w - 40f, 28f), "Hati-hati di jalan", sTitle);
            GUI.Label(new Rect(box.x + 20f, box.y + 58f, w - 40f, 90f),
                "Perhatikan jalan dan sekitarmu saat berburu. Jangan masuk ke area berbahaya atau milik orang lain.", sBody);
            var ok = new Rect(box.x + 16f, box.yMax - 16f - 58f, w - 32f, 58f);
            GUI.Label(DrawPrimaryButton(ok, IsHeld(ok, scale)), "SAYA MENGERTI", sPrimary);
            if (TapHit(ok, scale))
            {
                safetySeen = true;
                PlayerPrefs.SetInt(PrefSafetySeen, 1);
                PlayerPrefs.Save();
            }
        }
    }
}
