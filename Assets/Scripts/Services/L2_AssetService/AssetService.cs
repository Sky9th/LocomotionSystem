using System;
using System.Collections;
using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Audio;
using RedDust.Character.Kinematic;
using RedDust.Core;
using RedDust.Items;
using RedDust.Properties;
using RedDust.Shared;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace RedDust.Assets
{
    /// <summary>
    /// L2 asset hub. Handles Addressables lifecycle, label caching, boot init, and GC anchoring.
    /// Fills <see cref="GameService.Assets"/> during boot.
    /// </summary>
    public class AssetService : ModuleChildMono, IGameplaySessionHandler
    {
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
        private readonly List<UnityEngine.Object> _loadedAssets = new();
        private static readonly HashSet<string> PinnedLabels = new() { "boot" };
        private LogChannel _log;

        private List<UnityEngine.Object> _bootAssets;
        private bool _bootComplete;

        public bool IsInitialized { get; private set; }
        public bool BootComplete => _bootComplete;

        public override void OnAssemble()
        {
            _log = LogManager.GetChannel(GetType().Name);
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire() { }

        // ═══════════════════════════════════════════════
        // Init
        // ═══════════════════════════════════════════════

        public IEnumerator EnsureInitialized()
        {
            if (IsInitialized) yield break;

            var op = UnityAddressables.InitializeAsync();
            while (op.IsValid() && !op.IsDone) yield return null;

            if (!op.IsValid())
            {
                _log.Warning("Addressables init became invalid — proceeding degraded.");
                IsInitialized = true;
                yield break;
            }

            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                IsInitialized = true;
                _log.Info("Addressables initialized.");
            }
            else
            {
                _log.Error($"Addressables init failed: {op.OperationException}");
            }
        }

        // ═══════════════════════════════════════════════
        // Load
        // ═══════════════════════════════════════════════

        public void LoadByLabel<T>(string label, Action<List<T>> onComplete) where T : UnityEngine.Object
        {
            var key = $"{typeof(T).Name}:{label}";
            if (_handles.TryGetValue(key, out var cached))
            {
                if (cached.Status == AsyncOperationStatus.Succeeded && cached.Result is IList<T> list)
                {
                    onComplete?.Invoke(new List<T>(list));
                    return;
                }
            }

            var handle = UnityAddressables.LoadAssetsAsync<T>(
                new List<string> { label }, null, UnityAddressables.MergeMode.Union);
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    var result = new List<T>();
                    foreach (var item in op.Result)
                        if (item != null) { result.Add(item); _loadedAssets.Add(item); }
                    onComplete?.Invoke(result);
                }
                else
                {
                    _log.Error($"LoadByLabel<{typeof(T).Name}>('{label}') failed: {op.OperationException}");
                    onComplete?.Invoke(new List<T>());
                }
            };
            _handles[key] = handle;
        }

        public void LoadByLabels<T>(List<string> labels, Action onComplete) where T : UnityEngine.Object
        {
            if (labels == null || labels.Count == 0) { onComplete?.Invoke(); return; }
            int remaining = labels.Count;
            foreach (var label in labels)
                LoadByLabel<T>(label, _ => { remaining--; if (remaining <= 0) onComplete?.Invoke(); });
        }

        // ═══════════════════════════════════════════════
        // Release
        // ═══════════════════════════════════════════════

        public void ReleaseLabel(string[] labels)
        {
            if (labels == null) return;
            foreach (var label in labels)
            {
                if (PinnedLabels.Contains(label)) continue;
                var toRemove = new List<string>();
                foreach (var kv in _handles)
                    if (kv.Key.EndsWith($":{label}")) { UnityAddressables.Release(kv.Value); toRemove.Add(kv.Key); }
                foreach (var k in toRemove) _handles.Remove(k);
            }
        }

        // ═══════════════════════════════════════════════
        // Boot
        // ═══════════════════════════════════════════════

        public void RunBootInit()
        {
            if (_bootComplete) return;

            _bootAssets = new List<UnityEngine.Object>(_loadedAssets);
            var index = new TypedIndex(_bootAssets);
            var catalog = GameService.Instance.Assets;

            RebuildAllCaches(index.Get<RdTagDefSO>());

            catalog.InitPropertyDefs(MergeWithMemory<PropertyDefSO>(index));
            catalog.InitPropertyTrees(index.Get<PropertyTreeSO>());
            catalog.InitAbilityTrees(index.Get<AbilityTreeSO>());

            var items = new List<PropertyPresetSO>();
            items.AddRange(index.Get<ItemDefSO>());
            items.AddRange(index.Get<MeleeWeaponSO>());
            items.AddRange(index.Get<RangedWeaponSO>());
            catalog.InitItems(items);

            catalog.InitCharacters(index.Get<CharacterDefSO>());
            catalog.InitAnimProfiles(index.Get<CharacterAnimationProfileSO>());
            catalog.InitGroundConfigs(index.Get<GroundSystemConfigSO>());
            catalog.InitAudioConfigs(index.Get<CharacterAudioConfigSO>());

            var allTags = new List<RdTagDefSO>(Resources.FindObjectsOfTypeAll<RdTagDefSO>());
            RebuildAllCaches(allTags);

            _bootComplete = true;
            Debug.Log($"[AssetService] Boot complete. {_bootAssets.Count} assets anchored.");
        }

        // ═══════════════════════════════════════════════
        // Session
        // ═══════════════════════════════════════════════

        public void OnGameplaySessionEnd()
        {
            var toRemove = new List<string>();
            foreach (var kv in _handles)
            {
                bool pinned = false;
                foreach (var p in PinnedLabels) if (kv.Key.EndsWith($":{p}")) { pinned = true; break; }
                if (pinned) continue;
                UnityAddressables.Release(kv.Value);
                toRemove.Add(kv.Key);
            }
            foreach (var k in toRemove) _handles.Remove(k);
        }

        // ═══════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════

        /// <summary>BFS root-first FullTag rebuild. Moved from RdTagDefSO — boot infra, not data-model concern.</summary>
        private static void RebuildAllCaches(List<RdTagDefSO> loadedTags)
        {
            var allInMemory = Resources.FindObjectsOfTypeAll<RdTagDefSO>();
            var allTags = new HashSet<RdTagDefSO>(allInMemory);
            int loadedCount = loadedTags.Count;
            int totalInMemory = allTags.Count;
            int missedByLabel = totalInMemory - loadedCount;

            var refreshed = new HashSet<RdTagDefSO>();
            var queue = new Queue<RdTagDefSO>();

            foreach (var tag in allTags)
                if (tag.Parent == null) { tag.RefreshCache(); refreshed.Add(tag); queue.Enqueue(tag); }

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                foreach (var tag in allTags)
                    if (tag.Parent == parent && refreshed.Add(tag)) { tag.RefreshCache(); queue.Enqueue(tag); }
            }

            foreach (var tag in allTags)
                if (refreshed.Add(tag))
                {
                    Debug.LogWarning($"[AssetService] Tag '{tag.name}' has broken parent chain — refreshing as root.");
                    tag.RefreshCache();
                }

            if (missedByLabel > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[AssetService] {missedByLabel} tags loaded via scene refs (not Addressables):");
                foreach (var tag in allTags)
                    if (!loadedTags.Contains(tag))
                        sb.Append($"\n  {tag.name} → FullTag='{tag.FullTag}'  depth={tag.Depth}");
                Debug.Log(sb.ToString());
            }
        }

        private static List<T> MergeWithMemory<T>(TypedIndex index) where T : UnityEngine.Object
        {
            var from = index.Get<T>();
            var inMemory = Resources.FindObjectsOfTypeAll<T>();
            var merged = new HashSet<T>(from);
            foreach (var item in inMemory) merged.Add(item);
            return new List<T>(merged);
        }

        private class TypedIndex
        {
            private readonly Dictionary<Type, List<UnityEngine.Object>> _map = new();

            public TypedIndex(List<UnityEngine.Object> assets)
            {
                foreach (var a in assets)
                {
                    if (a == null) continue;
                    var type = a.GetType();
                    if (!_map.TryGetValue(type, out var list))
                        _map[type] = list = new List<UnityEngine.Object>();
                    list.Add(a);
                }
            }

            public List<T> Get<T>() where T : UnityEngine.Object
            {
                var result = new List<T>();
                foreach (var kv in _map)
                    if (typeof(T).IsAssignableFrom(kv.Key))
                        foreach (var item in kv.Value)
                            if (item is T t) result.Add(t);
                return result;
            }
        }
    }
}
