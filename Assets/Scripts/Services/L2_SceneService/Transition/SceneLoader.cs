using System;
using System.Collections;
using System.Collections.Generic;
using RedDust.Addressables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedDust.GameScene
{
    /// <summary>
    /// Coroutine-based scene and asset loading. Wraps SceneManager and AddressablesService.
    /// </summary>
    public class SceneLoader
    {
        private readonly AddressablesService _addressables;
        private readonly MonoBehaviour _owner;

        public SceneLoader(AddressablesService addressables, MonoBehaviour owner)
        {
            _addressables = addressables;
            _owner = owner;
        }

        /// <summary>Load a scene additively. Coroutine-compatible.</summary>
        public IEnumerator LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            var op = SceneManager.LoadSceneAsync(path, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene '{path}'. Not in build settings?");
                yield break;
            }

            while (!op.isDone)
                yield return null;
        }

        /// <summary>Unload a scene by name.</summary>
        public IEnumerator UnloadSceneAsync(string name)
        {
            var scene = SceneManager.GetSceneByName(name);
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[SceneLoader] Unload '{name}' skipped: scene not loaded. Loaded scenes: {string.Join(", ", GetAllSceneNames())}");
                yield break;
            }
            Debug.Log($"[SceneLoader] Unloading scene '{name}'...");
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static string[] GetAllSceneNames()
        {
            var names = new string[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++)
                names[i] = SceneManager.GetSceneAt(i).name;
            return names;
        }

        /// <summary>
        /// Load all assets matching the given labels via AddressablesService.
        /// Progress callback reports 0f on start, 1f on completion (binary — no per-asset granularity).
        /// </summary>
        public IEnumerator LoadLabelAsync(List<string> labels, Action<float> onProgress)
        {
            if (labels == null || labels.Count == 0)
            {
                onProgress?.Invoke(1f);
                yield break;
            }

            onProgress?.Invoke(0f);

            bool done = false;
            int remaining = labels.Count;

            foreach (var label in labels)
            {
                _addressables.LoadByLabel<UnityEngine.Object>(label, _ =>
                {
                    remaining--;
                    if (remaining <= 0) done = true;
                });
            }

            while (!done)
                yield return null;

            onProgress?.Invoke(1f);
        }

        /// <summary>Release Addressables handles for a set of labels (scene unload).</summary>
        public void ReleaseLabels(List<string> labels)
        {
            if (labels == null) return;
            foreach (var label in labels)
                _addressables.Release(label);
        }

        /// <summary>Release all non-boot handles. Called on session end.</summary>
        public void UnloadAll()
        {
            _addressables.ReleaseAll();
        }
    }
}
