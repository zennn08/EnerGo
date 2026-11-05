using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.EditorTools
{
    // Renders a UXML to a PNG in edit mode, for comparing screens with the design-system previews.
    // Begin() makes a temporary DontSave UIDocument that targets a RenderTexture; give the editor a few ticks
    // (QueuePlayerLoopUpdate) so the panel builds, then End() renders it and cleans up.
    public static class UiSnapshot
    {
        const string Name = "__ui_snapshot";

        public static string Begin(string uxml, int width = 480, int height = 800)
        {
            var old = GameObject.Find(Name);
            if (old != null) Object.DestroyImmediate(old);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset"));
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            ps.targetTexture = rt;
            ps.clearColor = true;
            var go = new GameObject(Name) { hideFlags = HideFlags.DontSave };
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
            go.SetActive(true);
            EditorApplication.QueuePlayerLoopUpdate();
            return doc.visualTreeAsset != null ? "begun" : "uxml not found";
        }

        public static VisualElement Root => GameObject.Find(Name)?.GetComponent<UIDocument>().rootVisualElement;

        // Fills the lobby with sample values through LobbyView's own tile code.
        public static void FillLobbySample()
        {
            string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };
            int[] n = { 12, 31, 14, 6, 0 };
            for (int i = 0; i < ids.Length; i++) EnerGo.UI.LobbyView.FillTile(Root.Q("tile-" + ids[i]), ids[i], n[i], false, true);
            Root.Q<Label>("total-unit").text = LobbyText.TotalUnits(63);
            Root.Q<Label>("mode-kamera").text = LobbyText.CameraMode(true);
        }

        // Shows one sheet the way the LayarSheet preview pictures it (state copied into the elements by hand).
        public static void ShowSheetSample(string which)
        {
            FillLobbySample();
            var r = Root;
            r.Q("lapis-sheet").style.display = DisplayStyle.Flex;
            foreach (var n in new[] { "sheet-pengaturan", "sheet-latihan", "sheet-panduan" })
            {
                var s = r.Q(n);
                if (s == null) continue;
                bool on = n == "sheet-" + which;
                s.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                s.EnableInClassList("is-terbuka", on);
            }
            if (which == "pengaturan")
            {
                r.Q("atur-ar").Q(className: "eg-toggle").AddToClassList("is-nyala");
                r.Q<Label>("atur-ar-sub").text = "Butuh Google Play Services for AR";
                r.Q("atur-getar").Q(className: "eg-toggle").AddToClassList("is-nyala");
                r.Q("atur-volume").Q(className: "eg-bar__isi").style.width = Length.Percent(70);
                r.Q("atur-volume").Q(className: "eg-slider__kenop").style.left = Length.Percent(70);
                r.Q<Label>("atur-volume-persen").text = "70%";
                var reset = r.Q<Button>("btn-reset");
                reset.text = "TEKAN LAGI UNTUK MENGHAPUS";
                reset.RemoveFromClassList("eg-btn--kertas"); reset.RemoveFromClassList("eg-btn--teks-bahaya"); reset.AddToClassList("eg-btn--bahaya");
                r.Q("reset-bar").Q(className: "eg-bar__isi").style.width = Length.Percent(60);
            }
            if (which == "latihan")
            {
                string[] ids = { "sun", "coal", "gold", "oil", "uranium", "random" };
                int[] n = { 12, 31, 14, 6, 0, -1 };
                for (int i = 0; i < ids.Length; i++)
                {
                    EnerGo.UI.LobbyView.FillTile(r.Q("latihan-" + ids[i]), ids[i], n[i], false, false);
                    r.Q("latihan-" + ids[i]).Q(className: "eg-tile").EnableInClassList("is-terpilih", ids[i] == "gold");
                }
            }
        }

        // Guide sheet with the read-mode card open on a page (preview: Emas, step 2 of 3).
        public static void ShowGuideSample(bool withCard)
        {
            ShowSheetSample("panduan");
            var r = Root;
            foreach (var id in GuideBook.Ids) EnerGo.UI.LobbyView.FillTile(r.Q("panduan-" + id), id, -1, false, false);
            r.Q<Label>("panduan-notice").style.visibility = Visibility.Hidden;
            r.Q("lapis-panduan").style.display = withCard ? DisplayStyle.Flex : DisplayStyle.None;
            if (!withCard) return;
            var cards = GuideBook.For(ResourceIds.Gold);
            var pager = new GuidePager(cards.Length);
            pager.Next();
            r.Q<Label>("panduan-selotip").text = "EMAS";
            r.Q<Label>("panduan-langkah").text = pager.StepLabel;
            r.Q("panduan-ilustrasi").AddToClassList("eg-ikon--gold");
            r.Q<Label>("panduan-judul").text = cards[pager.Page].Title;
            r.Q<Label>("panduan-isi").text = cards[pager.Page].Body;
            r.Q<Button>("btn-panduan-utama").text = pager.PrimaryLabel;
            var titik = r.Q("panduan-titik");
            for (int i = 0; i < pager.Count; i++) { var d = new VisualElement(); d.AddToClassList(i == pager.Page ? "panduan__titik-kini" : "panduan__titik-lain"); titik.Add(d); }
        }

        public static string End(string outPng)
        {
            var go = GameObject.Find(Name);
            if (go == null) return "no snapshot";
            var doc = go.GetComponent<UIDocument>();
            var rt = doc.panelSettings.targetTexture;
            var util = typeof(UIDocument).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
            var bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            for (int k = 0; k < 3; k++)
            {
                util.GetMethod("UpdatePanels", bf, null, System.Type.EmptyTypes, null)?.Invoke(null, null);
                foreach (var m in util.GetMethods(bf))
                    if (m.Name == "RepaintPanels" && m.GetParameters().Length == 1) m.Invoke(null, new object[] { false });
                util.GetMethod("RenderOffscreenPanels", bf, null, System.Type.EmptyTypes, null)?.Invoke(null, null);
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            rt.Release();
            return "saved " + outPng;
        }
    }
}
