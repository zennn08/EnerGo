using System.IO;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace EnerGo.EditorTools
{
    // One-shot setup for the UI Toolkit screens: font assets from the static fonts, the design-system
    // icons and logo as vector images, and the 480 x 800 panel settings. Safe to run again.
    public static class UiAssetSetup
    {
        const string Fonts = "Assets/Fonts";
        const string Icons = "Assets/UI/Icons";
        const string Logo = "Assets/UI/Logo";
        const string Panel = "Assets/UI/PanelSettings.asset";

        [MenuItem("EnerGo/UI/Setup Aset UI")]
        public static void SetupUiAssets()
        {
            Directory.CreateDirectory(Icons);
            Directory.CreateDirectory(Logo);
            string ds = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design-system", "assets"));
            foreach (string svg in Directory.GetFiles(Path.Combine(ds, "Ikon"), "*.svg"))
                File.Copy(svg, Path.Combine(Icons, Path.GetFileName(svg)), true);
            foreach (string svg in Directory.GetFiles(Path.Combine(ds, "Logo"), "*.svg"))
                File.Copy(svg, Path.Combine(Logo, Path.GetFileName(svg)), true);
            AssetDatabase.Refresh();

            foreach (string dir in new[] { Icons, Logo })
                foreach (string guid in AssetDatabase.FindAssets("", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is SVGImporter imp && imp.SvgType != SVGType.VectorImage)
                    {
                        imp.SvgType = SVGType.VectorImage;
                        imp.SaveAndReimport();
                    }
                }

            foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { Fonts }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetPath = Path.Combine(Fonts, Path.GetFileNameWithoutExtension(path) + " SDF.asset").Replace('\\', '/');
                if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null) continue;
                var fa = FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(path));
                AssetDatabase.CreateAsset(fa, assetPath);
                // The atlas texture and material live inside the font asset so it survives a reimport.
                if (fa.atlasTextures != null)
                    foreach (var tex in fa.atlasTextures) if (tex != null) { tex.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
                if (fa.material != null) { fa.material.name = fa.name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            }

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(Panel);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, Panel); }
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(480, 800);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0f;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            Debug.Log("EnerGo UI assets ready.");
        }
    }
}
