#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 右栏子资产选择器。Picker 模式：搜索 + 列表 + Select/Cancel。
    /// Creator 模式：类型选择 + 表单 + Create/Cancel（Phase 2+）。
    /// </summary>
    public static class SubAssetPickerView
    {
        private const float Pad = 6f;
        private static readonly Dictionary<string, bool> _effectFoldouts = new();
        private static ScriptableObject _selectedAsset;

        public static void DrawPicker(
            AbilityEditorModel model,
            SubAssetSlot slot,
            ref string searchText,
            Action<ScriptableObject> onSelected,
            Action onCreateNew,
            Action onCancel)
        {
            var s = searchText;

            EditorUIUtility.DrawCard(Pad, () =>
            {
                var title = slot switch
                {
                    SubAssetSlot.Activation => "Select Activation",
                    SubAssetSlot.Search => "Select Search Shape",
                    SubAssetSlot.TargetEffects => "Add Target Effect",
                    SubAssetSlot.SelfEffects => "Add Self Effect",
                    SubAssetSlot.Noise => "Select Noise",
                    _ => "Select Asset",
                };
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.Space(Pad);

                // 搜索
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(50));
                s = EditorGUILayout.TextField(s, GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(s)
                    && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    s = "";
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(Pad);

                // 列表 — 点击高亮，不直接确认
                DrawAssetList(model, slot, s, asset => _selectedAsset = asset);

                GUILayout.Space(Pad);

                // 底部按钮
                var hasSelection = _selectedAsset != null;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Create New", GUILayout.Height(22)))
                    onCreateNew?.Invoke();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(60), GUILayout.Height(22)))
                {
                    _selectedAsset = null;
                    onCancel?.Invoke();
                }
                GUI.enabled = hasSelection;
                GUI.backgroundColor = hasSelection ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
                if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(22)))
                {
                    onSelected?.Invoke(_selectedAsset);
                    _selectedAsset = null;
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            });

            searchText = s;
        }

        private static void DrawAssetList(
            AbilityEditorModel model, SubAssetSlot slot, string searchText,
            Action<ScriptableObject> onSelected)
        {
            switch (slot)
            {
                case SubAssetSlot.Activation:
                    DrawTypedList(model.AllActivations.Cast<ScriptableObject>().ToList(),
                        searchText, GetActivationSummary, onSelected);
                    break;
                case SubAssetSlot.Search:
                    DrawTypedList(model.AllSearches.Cast<ScriptableObject>().ToList(),
                        searchText, GetSearchSummary, onSelected);
                    break;
                case SubAssetSlot.TargetEffects:
                case SubAssetSlot.SelfEffects:
                    DrawEffectTree(model, searchText, onSelected);
                    break;
                case SubAssetSlot.Noise:
                    DrawTypedList(model.AllNoises.Cast<ScriptableObject>().ToList(),
                        searchText, GetNoiseSummary, onSelected);
                    break;
            }
        }

        private static void DrawTypedList(
            List<ScriptableObject> assets, string searchText,
            Func<ScriptableObject, string> getSummary,
            Action<ScriptableObject> onSelected)
        {
            var q = string.IsNullOrEmpty(searchText) ? null : searchText.ToLowerInvariant();

            var filtered = assets;
            if (q != null)
                filtered = assets.Where(a => a.name.ToLowerInvariant().Contains(q)).ToList();

            if (filtered.Count == 0)
            {
                var grey = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = Color.grey }, alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField(
                    q != null ? "No matches. Create new?" : "No assets yet. Create new?", grey);
                return;
            }

            var scrollPos = Vector2.zero;
            var scroll = EditorGUILayout.BeginScrollView(
                scrollPos, GUILayout.ExpandHeight(true));
            for (var i = 0; i < filtered.Count; i++)
            {
                var asset = filtered[i];

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(2f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(Pad);

                EditorGUILayout.BeginVertical();

                var nameStyle = new GUIStyle(EditorStyles.label);
                if (GUILayout.Button(asset.name, nameStyle, GUILayout.ExpandWidth(true)))
                    onSelected?.Invoke(asset);

                var summary = getSummary(asset);
                if (!string.IsNullOrEmpty(summary))
                {
                    var s = new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = Color.grey } };
                    EditorGUILayout.LabelField(summary, s);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(Pad);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2f);
                EditorGUILayout.EndVertical();

                if (i < filtered.Count - 1) GUILayout.Space(2f);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawEffectTree(AbilityEditorModel model, string searchText,
            Action<ScriptableObject> onSelected)
        {
            var nullSO = (AbilitySO)null;
            AbilityTreeView.DrawTree(model.EffectTreeRoots, _effectFoldouts, ref nullSO,
                searchText, AbilityTypeFilter.All,
                onLeafSelected: asset => onSelected(asset),
                selectedEffect: _selectedAsset as EffectSO);
        }

        // ── 摘要 ──
        private static string GetActivationSummary(ScriptableObject a)
        {
            if (a is not AbilityActivationSO act) return null;
            return $"{act.activationType} · speed:{act.animationSpeed:F1}";
        }

        private static string GetSearchSummary(ScriptableObject s)
        {
            if (s is not AbilitySearchSO search) return null;
            return $"{search.searchType} · range:{search.range:F1} · max:{search.maxTargets}";
        }

        private static string GetEffectSummary(ScriptableObject e)
        {
            if (e is not EffectSO eff) return null;
            var type = eff.GetType().Name.Replace("EffectSO", "");
            if (eff is DamageEffectSO d)
                return $"Damage · {eff.effectTag?.FullTag ?? "-"} · base:{d.baseDamage:F0}";
            if (eff is ImpactEffectSO i)
                return $"Impact · {eff.effectTag?.FullTag ?? "-"} · stagger:{i.staggerValue:F0}";
            if (eff is ExecuteEffectSO x)
                return $"Execute · {eff.effectTag?.FullTag ?? "-"} · threshold:{x.hpThreshold:P0}";
            if (eff is CostEffectSO c)
                return $"Cost · {c.statDef?.name ?? "-"} · amount:{c.amount:F0}";
            return $"{type} · {eff.effectTag?.FullTag ?? "-"}";
        }

        private static string GetNoiseSummary(ScriptableObject n)
        {
            if (n is not NoiseEventSO noise) return null;
            return $"level:{noise.level:F0} · decay:{noise.decayRadius:F1}m";
        }
    }
}
#endif
