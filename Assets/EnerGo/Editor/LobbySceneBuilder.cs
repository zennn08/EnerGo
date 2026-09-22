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

        public static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(LobbyScenePath, true));
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
