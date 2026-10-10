using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EnerGo.Editor
{
    [InitializeOnLoad]
    public static class LobbySceneBuilder
    {
        const string LobbyScenePath = "Assets/EnerGo/LobbyScene.unity";
        const string ARScenePath = "Assets/EnerGo/ARPrototype.unity";
        const string SensorScenePath = "Assets/EnerGo/Sensor360Scene.unity";
        const string ExploreScenePath = "Assets/EnerGo/ExploreScene.unity";

        static LobbySceneBuilder()
        {
            EditorApplication.delayCall += EnsureLobbyScene;
        }

        [MenuItem("EnerGo/Setup Lobby Scene")]
        public static void EnsureLobbyScene()
        {
            // Check if scene exists already
            var existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScenePath);
            if (existingScene == null)
            {
                CreateLobbyScene();
            }
            UpdateBuildSettings();
        }

        public static void CreateLobbyScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            bool wasSaved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!wasSaved) return;

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.08f, 0.12f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.tag = "MainCamera";

            // 2. Lobby Controller
            var lobbyGo = new GameObject("Lobby Controller");
            lobbyGo.AddComponent<LobbyController>();

            // 3. Save Scene
            EditorSceneManager.SaveScene(newScene, LobbyScenePath);
            Debug.Log("LobbyScene created and saved at: " + LobbyScenePath);
        }

        // Run once from the menu (or via MCP). Not done automatically on load, because it switches the open scene.
        [MenuItem("EnerGo/Setup Explore Scene")]
        public static void CreateExploreScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExploreScenePath) != null) { UpdateBuildSettings(); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.98f, 0.97f, 0.94f, 1f); // Cream; the radar draws over it anyway
            cam.orthographic = true;
            camGo.tag = "MainCamera";
            var radar = new GameObject("Radar");
            radar.AddComponent<LocationFeed>();
            radar.AddComponent<RadarController>();
            EditorSceneManager.SaveScene(scene, ExploreScenePath);
            UpdateBuildSettings();
        }

        public static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(LobbyScenePath, true));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExploreScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(ExploreScenePath, true));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ARScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(ARScenePath, true));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SensorScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(SensorScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"EnerGo Build Scenes configured ({scenes.Count} scenes registered).");
        }
    }
}
