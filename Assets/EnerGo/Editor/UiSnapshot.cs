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
