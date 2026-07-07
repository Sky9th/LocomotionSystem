using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Audio;
using RedDust.Character.Kinematic;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Assets
{
    /// <summary>
    /// Centralized catalog of all boot-loaded ScriptableObject assets.
    /// Owned by GameService, filled by AssetService.RunBootInit, queried by all services.
    /// </summary>
    public class AssetCatalog
    {
        private Dictionary<string, CharacterDefSO> _characters;
        private Dictionary<string, PropertyPresetSO> _items;
        private Dictionary<string, AbilityTreeSO> _abilityTrees;
        private Dictionary<string, PropertyDefSO> _propertyDefs;
        private Dictionary<string, PropertyTreeSO> _propertyTrees;
        private Dictionary<string, CharacterAnimationProfileSO> _animProfiles;
        private Dictionary<string, GroundSystemConfigSO> _groundConfigs;
        private Dictionary<string, CharacterAudioConfigSO> _audioConfigs;

        // ═══════════════════════════════════════════════════════════════
        // Init (called by AssetService.RunBootInit)
        // ═══════════════════════════════════════════════════════════════

        public void InitCharacters(List<CharacterDefSO> defs)
        {
            _characters = new Dictionary<string, CharacterDefSO>();


            if (defs == null) return;

            foreach (var d in defs)
            {
                if (d == null || (object)d == null || string.IsNullOrEmpty(d.name))
                {
                    Debug.LogWarning("[AssetCatalog] Character: skipping null/invalid entry.");
                    continue;
                }
                var key = d.name;
                if (_characters.ContainsKey(key))
                {
                    Debug.LogWarning($"[AssetCatalog] Character: duplicate key '{key}' — skipping.");
                    continue;
                }
                _characters[key] = d;
            }

            Debug.Log($"[AssetCatalog] Characters initialized: {_characters.Count} entries [{string.Join(", ", _characters.Keys)}]");
        }

        public void InitItems(List<PropertyPresetSO> presets)
        {
            _items = new Dictionary<string, PropertyPresetSO>();

            if (presets == null) return;

            foreach (var p in presets)
            {
                if (p == null) continue;
                var key = p.name;
                if (_items.ContainsKey(key))
                {
                    Debug.LogWarning($"[AssetCatalog] Item: duplicate key '{key}' — skipping.");
                    continue;
                }
                _items[key] = p;
            }

            Debug.Log($"[AssetCatalog] Items initialized: {_items.Count} entries [{string.Join(", ", _items.Keys)}]");
        }

        public void InitAbilityTrees(List<AbilityTreeSO> trees)
        {
            _abilityTrees = new Dictionary<string, AbilityTreeSO>();

            if (trees == null) return;

            foreach (var t in trees)
            {
                if (t == null || string.IsNullOrEmpty(t.treeId)) continue;
                if (_abilityTrees.ContainsKey(t.treeId))
                {
                    Debug.LogWarning($"[AssetCatalog] AbilityTree: duplicate treeId '{t.treeId}' — skipping.");
                    continue;
                }
                _abilityTrees[t.treeId] = t;
            }

            Debug.Log($"[AssetCatalog] AbilityTrees initialized: {_abilityTrees.Count} entries [{string.Join(", ", _abilityTrees.Keys)}]");
        }

        public void InitPropertyDefs(List<PropertyDefSO> defs)
        {
            _propertyDefs = new Dictionary<string, PropertyDefSO>();

            if (defs == null) return;

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                if (_propertyDefs.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[AssetCatalog] PropertyDef: duplicate Id '{def.Id}' — skipping.");
                    continue;
                }
                _propertyDefs[def.Id] = def;
            }

            Debug.Log($"[AssetCatalog] PropertyDefs initialized: {_propertyDefs.Count} entries [{string.Join(", ", _propertyDefs.Keys)}]");
        }

        public void InitPropertyTrees(List<PropertyTreeSO> trees)
        {
            _propertyTrees = new Dictionary<string, PropertyTreeSO>();

            if (trees == null) return;

            foreach (var t in trees)
            {
                if (t == null) continue;
                var key = t.name;
                if (_propertyTrees.ContainsKey(key))
                {
                    Debug.LogWarning($"[AssetCatalog] PropertyTree: duplicate name '{key}' — skipping.");
                    continue;
                }
                _propertyTrees[key] = t;
            }

            Debug.Log($"[AssetCatalog] PropertyTrees initialized: {_propertyTrees.Count} entries [{string.Join(", ", _propertyTrees.Keys)}]");
        }

        public void InitAnimProfiles(List<CharacterAnimationProfileSO> profiles)
        {
            _animProfiles = new Dictionary<string, CharacterAnimationProfileSO>();
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
            if (_characters == null) return null;
            if (_characters.TryGetValue(key, out var def))
                return def;
            Debug.LogError($"[AssetCatalog] Character key '{key}' not found. Available ({_characters.Count}): [{string.Join(", ", _characters.Keys)}]");
            return null;
        }

        public PropertyPresetSO FindItem(string key)
        {
            if (_items == null) return null;
            if (_items.TryGetValue(key, out var preset))
                return preset;
            Debug.LogError($"[AssetCatalog] Item key '{key}' not found. Available ({_items.Count}): [{string.Join(", ", _items.Keys)}]");
            return null;
        }

        public T FindItem<T>(string key) where T : PropertyPresetSO
        {
            return FindItem(key) as T;
        }


        public AbilityTreeSO FindAbilityTree(string treeId)
        {
            if (_abilityTrees == null) return null;
            _abilityTrees.TryGetValue(treeId, out var tree);
            return tree;
        }

        /// <summary>
        /// 批量解析技能树 ID → AbilityTreeSO[]。未找到的 ID 记 warning，过滤掉。
        /// 这是所有"ID 数组 → SO 数组"的唯一入口，调用方不应自己写 Registry 遍历循环。
        /// </summary>
        public AbilityTreeSO[] ResolveAbilityTrees(string[] treeIds)
        {
            if (_abilityTrees == null) return System.Array.Empty<AbilityTreeSO>();
            if (treeIds == null || treeIds.Length == 0) return System.Array.Empty<AbilityTreeSO>();

            var list = new List<AbilityTreeSO>();
            foreach (var id in treeIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (_abilityTrees.TryGetValue(id, out var tree) && tree != null)
                    list.Add(tree);
                else
                    Debug.LogWarning($"[AssetCatalog] AbilityTree '{id}' not found.");
            }
            return list.ToArray();
        }


        public PropertyDefSO FindPropertyDef(string id)
        {
            if (_propertyDefs == null) return null;
            _propertyDefs.TryGetValue(id, out var def);
            return def;
        }


        public PropertyTreeSO FindPropertyTree(string treeId)
        {
            if (_propertyTrees == null) return null;
            _propertyTrees.TryGetValue(treeId, out var tree);
            return tree;
        }


        public CharacterAnimationProfileSO FindAnimProfile(string key)
        {
            if (_animProfiles == null) return null;
            _animProfiles.TryGetValue(key, out var profile);
            return profile;
        }

        public GroundSystemConfigSO FindGroundConfig(string key)
        {
            if (_groundConfigs == null) return null;
            _groundConfigs.TryGetValue(key, out var config);
            return config;
        }

        public CharacterAudioConfigSO FindAudioConfig(string key)
        {
            if (_audioConfigs == null) return null;
            _audioConfigs.TryGetValue(key, out var config);
            return config;
        }


        // ── Helpers ──

    }
}
