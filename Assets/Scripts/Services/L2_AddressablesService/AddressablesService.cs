using System;
using System.Collections;
using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace RedDust.Addressables
{
    /// <summary>
    /// L2 service wrapping Unity Addressables API. Provides label-based batch loading with handle
    /// caching to prevent duplicate loads. Initialized once at preload, then used throughout the session.
    /// </summary>
    public class AddressablesService : ModuleChildMono
    {
        private readonly Dictionary<string, AsyncOperationHandle> _handleCache = new();
        private Shared.LogChannel _log;

        /// <summary>Labels that are never released — system-level assets that persist for the session.</summary>
        private static readonly HashSet<string> PinnedLabels = new() { "boot" };

        public bool IsInitialized { get; private set; }

        public override void OnAssemble()
        {
            _log = Shared.LogManager.GetChannel(GetType().Name);
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire() { }

        /// <summary>
        /// Initialize the Addressables system. Must be called once before any asset loading.
        /// Uses coroutine yield on the async handle — no callbacks needed.
        /// </summary>
        public IEnumerator InitializeAsync()
        {
            if (IsInitialized) yield break;

            var initOp = UnityAddressables.InitializeAsync();
            while (initOp.IsValid() && !initOp.IsDone)
                yield return null;

            if (!initOp.IsValid())
            {
                _log.Warning("Addressables init handle became invalid before completion.");
                IsInitialized = true;
                yield break;
            }

            if (initOp.Status == AsyncOperationStatus.Succeeded)
            {
                IsInitialized = true;
                _log.Info("Addressables initialized successfully.");
            }
            else
            {
                _log.Error($"Addressables initialization failed: {initOp.OperationException}");
                IsInitialized = false;
            }
        }

        /// <summary>
        /// Load all assets with the given label. Results are delivered via callback when complete.
        /// The handle is cached per (type, label) pair; subsequent calls with the same pair
        /// return the cached result instantly (no re-load).
        /// </summary>
        public void LoadByLabel<T>(string label, Action<List<T>> onComplete) where T : UnityEngine.Object
        {
            var cacheKey = $"{typeof(T).Name}:{label}";
            if (_handleCache.TryGetValue(cacheKey, out var cached))
            {
                // Already loaded — invoke callback immediately with cached result
                if (cached.Status == AsyncOperationStatus.Succeeded && cached.Result is IList<T> list)
                {
                    onComplete?.Invoke(new List<T>(list));
                    return;
                }
            }

            // Wrap in List to avoid string→IEnumerable<char> overload (Unity ADDR-3237)
            var handle = UnityAddressables.LoadAssetsAsync<T>(new List<string> { label }, null, UnityAddressables.MergeMode.Union);
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    var result = new List<T>();
                    foreach (var item in op.Result)
                        if (item != null) result.Add(item);
                    onComplete?.Invoke(result);
                }
                else
                {
                    _log.Error($"LoadByLabel<{typeof(T).Name}>('{label}') failed: {op.OperationException}");
                    onComplete?.Invoke(new List<T>());
                }
            };

            _handleCache[cacheKey] = handle;
        }

        /// <summary>Release all cached handles. Called on session teardown.</summary>
        public void ReleaseAll()
        {
            foreach (var kv in _handleCache)
                UnityAddressables.Release(kv.Value);
            _handleCache.Clear();
            IsInitialized = false;
        }

        /// <summary>Release a specific label's handle for all types. Pinned labels (e.g. "boot") are ignored.</summary>
        public void Release(string label)
        {
            if (PinnedLabels.Contains(label))
            {
                _log.Info($"Release('{label}') skipped — label is pinned (system-level assets).");
                return;
            }

            var keysToRemove = new List<string>();
            foreach (var kv in _handleCache)
            {
                if (kv.Key.EndsWith($":{label}"))
                {
                    UnityAddressables.Release(kv.Value);
                    keysToRemove.Add(kv.Key);
                }
            }
            foreach (var key in keysToRemove)
                _handleCache.Remove(key);
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }
    }
}
