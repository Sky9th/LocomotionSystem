using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Audio;
using RedDust.Character.Kinematic;
using RedDust.GameScene;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// Centralized registry for all boot-loaded ScriptableObject assets.
    ///
    /// Owned by GameService (DontDestroyOnLoad) — the instance reference and the
    /// BootAssetCatalog reference together keep assets alive across scene transitions.
    ///
    /// All 8 former standalone registries are now unified here:
    ///   Character, Item, AbilityTree, PropertyDef, PropertyTree,
    ///   AnimationProfile, GroundSystemConfig, AudioConfig.
    ///
    /// Populated by BootTasks, consumed by PlayerService / CharacterActor / PropertyTable.
    /// </summary>
    public class GameRegistry
    {
        // ── Backing stores ──

        private Dictionary<string, CharacterDefSO> _characters;
        private Dictionary<string, PropertyPresetSO> _items;
        private Dictionary<string, AbilityTreeSO> _abilityTrees;
        private Dictionary<string, PropertyDefSO> _propertyDefs;
        private Dictionary<string, PropertyTreeSO> _propertyTrees;
        private Dictionary<string, CharacterAnimationProfileSO> _animProfiles;
        private Dictionary<string, GroundSystemConfigSO> _groundConfigs;
        private Dictionary<string, CharacterAudioConfigSO> _audioConfigs;

        /// <summary>
        /// Kept alive to maintain a secondary strong-reference root to all
        /// boot-loaded assets, preventing Unity native-side teardown.
        /// </summary>
        private BootAssetCatalog _catalog;

        private bool _charactersReady;
        private bool _itemsReady;
        private bool _abilityTreesReady;
        private bool _propertyDefsReady;
        private bool _propertyTreesReady;
        private bool _animProfilesReady;
        private bool _groundConfigsReady;
        private bool _audioConfigsReady;

        /// <summary>True once at least one Init has been called.</summary>
        public bool IsInitialized { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // Catalog (strong-reference root)
        // ═══════════════════════════════════════════════════════════════

        public void SetCatalog(BootAssetCatalog catalog)
        {
            _catalog = catalog;
            IsInitialized = true;
        }

        // ═══════════════════════════════════════════════════════════════
        // Initialize methods (called by BootTasks in order)
        // ═══════════════════════════════════════════════════════════════

        public void InitCharacters(List<CharacterDefSO> defs)
        {
            _characters = new Dictionary<string, CharacterDefSO>();
            _charactersReady = true;

            if (defs == null) return;

            foreach (var d in defs)
            {
                if (d == null || (object)d == null || string.IsNullOrEmpty(d.name))
                {
                    Debug.LogWarning("[GameRegistry] Character: skipping null/invalid entry.");
                    continue;
                }
                var key = d.name;
                if (_characters.ContainsKey(key))
                {
                    Debug.LogWarning($"[GameRegistry] Character: duplicate key '{key}' — skipping.");
                    continue;
                }
                _characters[key] = d;
            }

            Debug.Log($"[GameRegistry] Characters initialized: {_characters.Count} entries [{string.Join(", ", _characters.Keys)}]");
        }

        public void InitItems(List<PropertyPresetSO> presets)
        {
            _items = new Dictionary<string, PropertyPresetSO>();
            _itemsReady = true;

            if (presets == null) return;

            foreach (var p in presets)
            {
                if (p == null) continue;
                var key = p.name;
                if (_items.ContainsKey(key))
                {
                    Debug.LogWarning($"[GameRegistry] Item: duplicate key '{key}' — skipping.");
                    continue;
                }
                _items[key] = p;
            }

            Debug.Log($"[GameRegistry] Items initialized: {_items.Count} entries [{string.Join(", ", _items.Keys)}]");
        }

        public void InitAbilityTrees(List<AbilityTreeSO> trees)
        {
            _abilityTrees = new Dictionary<string, AbilityTreeSO>();
            _abilityTreesReady = true;

            if (trees == null) return;

            foreach (var t in trees)
            {
                if (t == null || string.IsNullOrEmpty(t.treeId)) continue;
                if (_abilityTrees.ContainsKey(t.treeId))
                {
                    Debug.LogWarning($"[GameRegistry] AbilityTree: duplicate treeId '{t.treeId}' — skipping.");
                    continue;
                }
                _abilityTrees[t.treeId] = t;
            }

            Debug.Log($"[GameRegistry] AbilityTrees initialized: {_abilityTrees.Count} entries [{string.Join(", ", _abilityTrees.Keys)}]");
        }

        public void InitPropertyDefs(List<PropertyDefSO> defs)
        {
            _propertyDefs = new Dictionary<string, PropertyDefSO>();
            _propertyDefsReady = true;

            if (defs == null) return;

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                if (_propertyDefs.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[GameRegistry] PropertyDef: duplicate Id '{def.Id}' — skipping.");
                    continue;
                }
                _propertyDefs[def.Id] = def;
            }

            Debug.Log($"[GameRegistry] PropertyDefs initialized: {_propertyDefs.Count} entries [{string.Join(", ", _propertyDefs.Keys)}]");
        }

        public void InitPropertyTrees(List<PropertyTreeSO> trees)
        {
            _propertyTrees = new Dictionary<string, PropertyTreeSO>();
            _propertyTreesReady = true;

            if (trees == null) return;

            foreach (var t in trees)
            {
                if (t == null) continue;
                var key = t.name;
                if (_propertyTrees.ContainsKey(key))
                {
                    Debug.LogWarning($"[GameRegistry] PropertyTree: duplicate name '{key}' — skipping.");
                    continue;
                }
                _propertyTrees[key] = t;
            }

            Debug.Log($"[GameRegistry] PropertyTrees initialized: {_propertyTrees.Count} entries [{string.Join(", ", _propertyTrees.Keys)}]");
        }

        public void InitAnimProfiles(List<CharacterAnimationProfileSO> profiles)
        {
            _animProfiles = new Dictionary<string, CharacterAnimationProfileSO>();
            _animProfilesReady = true;
            if (profiles == null) return;
            foreach (var p in profiles)
            {
                if (p == null) continue;
                _animProfiles[p.name] = p;
            }
        }

        public void InitGroundConfigs(List<GroundSystemConfigSO> configs)
        {
            _groundConfigs = new Dictionary<string, GroundSystemConfigSO>();
            _groundConfigsReady = true;
            if (configs == null) return;
            foreach (var c in configs)
            {
                if (c == null) continue;
                _groundConfigs[c.name] = c;
            }
        }

        public void InitAudioConfigs(List<CharacterAudioConfigSO> configs)
        {
            _audioConfigs = new Dictionary<string, CharacterAudioConfigSO>();
            _audioConfigsReady = true;
            if (configs == null) return;
            foreach (var c in configs)
            {
                if (c == null) continue;
                _audioConfigs[c.name] = c;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Find methods
        // ═══════════════════════════════════════════════════════════════

        public CharacterDefSO FindCharacter(string key)
        {
            if (!_charactersReady) { LogNotReady("Characters"); return null; }
            if (_characters.TryGetValue(key, out var def))
                return def;
            Debug.LogError($"[GameRegistry] Character key '{key}' not found. Available ({_characters.Count}): [{string.Join(", ", _characters.Keys)}]");
            return null;
        }

        public PropertyPresetSO FindItem(string key)
        {
            if (!_itemsReady) { LogNotReady("Items"); return null; }
            if (_items.TryGetValue(key, out var preset))
                return preset;
            Debug.LogError($"[GameRegistry] Item key '{key}' not found. Available ({_items.Count}): [{string.Join(", ", _items.Keys)}]");
            return null;
        }

        public T FindItem<T>(string key) where T : PropertyPresetSO
        {
            return FindItem(key) as T;
        }

        public bool ContainsItem(string key)
        {
            if (!_itemsReady) return false;
            return _items.ContainsKey(key);
        }

        public AbilityTreeSO FindAbilityTree(string treeId)
        {
            if (!_abilityTreesReady) { LogNotReady("AbilityTrees"); return null; }
            _abilityTrees.TryGetValue(treeId, out var tree);
            return tree;
        }

        /// <summary>
        /// 批量解析技能树 ID → AbilityTreeSO[]。未找到的 ID 记 warning，过滤掉。
        /// 这是所有"ID 数组 → SO 数组"的唯一入口，调用方不应自己写 Registry 遍历循环。
        /// </summary>
        public AbilityTreeSO[] ResolveAbilityTrees(string[] treeIds)
        {
            if (!_abilityTreesReady) { LogNotReady("AbilityTrees"); return System.Array.Empty<AbilityTreeSO>(); }
            if (treeIds == null || treeIds.Length == 0) return System.Array.Empty<AbilityTreeSO>();

            var list = new List<AbilityTreeSO>();
            foreach (var id in treeIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (_abilityTrees.TryGetValue(id, out var tree) && tree != null)
                    list.Add(tree);
                else
                    Debug.LogWarning($"[GameRegistry] AbilityTree '{id}' not found.");
            }
            return list.ToArray();
        }

        public IReadOnlyList<AbilityTreeSO> AllAbilityTrees
        {
            get
            {
                if (!_abilityTreesReady) return System.Array.Empty<AbilityTreeSO>();
                return new List<AbilityTreeSO>(_abilityTrees.Values);
            }
        }

        public PropertyDefSO FindPropertyDef(string id)
        {
            if (!_propertyDefsReady) { LogNotReady("PropertyDefs"); return null; }
            _propertyDefs.TryGetValue(id, out var def);
            return def;
        }

        public bool ContainsPropertyDef(string id)
        {
            if (!_propertyDefsReady) return false;
            return _propertyDefs.ContainsKey(id);
        }

        public PropertyTreeSO FindPropertyTree(string treeId)
        {
            if (!_propertyTreesReady) { LogNotReady("PropertyTrees"); return null; }
            _propertyTrees.TryGetValue(treeId, out var tree);
            return tree;
        }

        public bool ContainsPropertyTree(string treeId)
        {
            if (!_propertyTreesReady) return false;
            return _propertyTrees.ContainsKey(treeId);
        }

        public CharacterAnimationProfileSO FindAnimProfile(string key)
        {
            if (!_animProfilesReady) return null;
            _animProfiles.TryGetValue(key, out var profile);
            return profile;
        }

        public GroundSystemConfigSO FindGroundConfig(string key)
        {
            if (!_groundConfigsReady) return null;
            _groundConfigs.TryGetValue(key, out var config);
            return config;
        }

        public CharacterAudioConfigSO FindAudioConfig(string key)
        {
            if (!_audioConfigsReady) return null;
            _audioConfigs.TryGetValue(key, out var config);
            return config;
        }


        // ── Helpers ──

        private static void LogNotReady(string name)
        {
            Debug.LogWarning($"[GameRegistry] {name} not initialized yet.");
        }
    }
}
