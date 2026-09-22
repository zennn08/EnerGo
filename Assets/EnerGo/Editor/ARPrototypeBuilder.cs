using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

namespace EnerGo.Editor
{
    public static class ARPrototypeBuilder
    {
        [MenuItem("EnerGo/Create AR Prototype")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string folder = "Assets/EnerGo";
            const string scenePath = folder + "/ARPrototype.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                EditorSceneManager.OpenScene(scenePath);
                return;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Selection.activeGameObject = null;
            if (!EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session"))
                throw new System.InvalidOperationException("AR Session menu unavailable.");
            Selection.activeGameObject = null;
            if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)"))
                throw new System.InvalidOperationException("XR Origin menu unavailable.");
            var cameraManager = Object.FindFirstObjectByType<ARCameraManager>();
            if (cameraManager == null) throw new System.InvalidOperationException("AR camera not created.");
            var camera = cameraManager.GetComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new System.InvalidOperationException("URP Unlit shader unavailable.");
            const string materialPath = folder + "/SolarEnergy.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "SolarEnergy" };
                material.SetColor("_BaseColor", new Color(1f, 0.72f, 0.08f));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var controller = new GameObject("EnerGo Capture").AddComponent<ARCapturePrototype>();
            controller.arCamera = camera;
            controller.energyMaterial = material;

            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (renderer.rendererFeatures.Any(f => f is ARBackgroundRendererFeature)) continue;
                var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                feature.name = "AR Background Renderer Feature";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                EditorUtility.SetDirty(renderer);
            }
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.energigo.prototype");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            // Keep existing scenes; place prototype first for the test APK.
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = controller.gameObject;
            Debug.Log("EnerGo AR prototype ready. Check Android XR Project Validation, then Build And Run on the phone.");
        }

        [MenuItem("EnerGo/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            var scenes = new[] { "Assets/EnerGo/LobbyScene.unity", "Assets/EnerGo/ARPrototype.unity", "Assets/EnerGo/Sensor360Scene.unity" };
            Directory.CreateDirectory("Builds");
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/EnerGo-A16-360.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"EnerGo Build: {report.summary.result} ({report.summary.totalSize} bytes)");
        }
    }
}

