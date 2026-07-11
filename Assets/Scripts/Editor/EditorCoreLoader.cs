#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RedDust.Shared
{
    [InitializeOnLoad]
    public static class EditorCoreLoader
    {
        private const string CoreScenePath = "Assets/Scenes/Core.unity";
        private const string StartupSceneNameKey = "RedDust.Editor.StartupSceneName";
        private const string PreviousPlayModeStartScenePathKey = "RedDust.Editor.PreviousPlayModeStartScenePath";

        static EditorCoreLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    PreparePlayFromCore();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    RestoreEditorPlayModeScene();
                    break;
            }
        }

        private static void PreparePlayFromCore()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name == "Core")
            {
                SessionState.EraseString(StartupSceneNameKey);
                return;
            }

            SessionState.SetString(StartupSceneNameKey, activeScene.name);

            var previousStartScene = EditorSceneManager.playModeStartScene;
            string previousStartScenePath = previousStartScene != null
                ? AssetDatabase.GetAssetPath(previousStartScene)
                : string.Empty;
            SessionState.SetString(PreviousPlayModeStartScenePathKey, previousStartScenePath);

            var coreSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreScenePath);
            if (coreSceneAsset != null)
                EditorSceneManager.playModeStartScene = coreSceneAsset;
        }

        private static void RestoreEditorPlayModeScene()
        {
            string previousStartScenePath = SessionState.GetString(PreviousPlayModeStartScenePathKey, string.Empty);

            if (string.IsNullOrEmpty(previousStartScenePath))
            {
                EditorSceneManager.playModeStartScene = null;
            }
            else
            {
                var previousSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(previousStartScenePath);
                EditorSceneManager.playModeStartScene = previousSceneAsset;
            }

            SessionState.EraseString(PreviousPlayModeStartScenePathKey);
            SessionState.EraseString(StartupSceneNameKey);
        }
    }
}
#endif
