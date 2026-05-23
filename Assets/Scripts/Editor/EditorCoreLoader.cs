#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EditorCoreLoader
{
	static EditorCoreLoader()
	{
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}

	private static void OnPlayModeChanged(PlayModeStateChange change)
	{
		if (change != PlayModeStateChange.ExitingEditMode) return;

		var activeScene = SceneManager.GetActiveScene();
		if (activeScene.name == "Core") return;

		var corePath = "Assets/Scenes/Core.unity";
		var coreScene = EditorSceneManager.GetSceneByPath(corePath);
		if (!coreScene.isLoaded)
			EditorSceneManager.OpenScene(corePath, OpenSceneMode.Additive);
	}
}
#endif
