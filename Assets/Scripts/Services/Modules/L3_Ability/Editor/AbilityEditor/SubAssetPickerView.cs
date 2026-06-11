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
        private static Vector2 _typedListScroll;

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
                s = EditorUIUtility.DrawSearchRow(s, labelWidth: 50f);

                GUILayout.Space(Pad);

                // 列表 — 点击高亮，不直接确认
                DrawAssetList(model, slot, s, asset => _selectedAsset = asset);

                GUILayout.Space(Pad);

                // 底部按钮
                var hasSelection = _selectedAsset != null;
                EditorGUILayout.BeginHorizontal();
                // 仅非 Effect 槽位显示 Create
                if (slot != SubAssetSlot.TargetEffects && slot != SubAssetSlot.SelfEffects
                    && GUILayout.Button("+ Create New", GUILayout.Height(22)))
                    onCreateNew?.Invoke();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(60), GUILayout.Height(22)))
                {
                    _selectedAsset = null;
                    onCancel?.Invoke();
                }
                GUI.enabled = hasSelection;
                GUI.backgroundColor = hasSelection ? EditorUIUtility.ColorGreen : Color.white;
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
                EditorGUILayout.LabelField(
                    q != null ? "No matches. Create new?" : "No assets yet. Create new?",
                    EditorUIUtility.GreyPlaceholder);
                return;
            }

            _typedListScroll = EditorGUILayout.BeginScrollView(
                _typedListScroll, GUILayout.ExpandHeight(true));
            for (var i = 0; i < filtered.Count; i++)
            {
                var asset = filtered[i];

                EditorUIUtility.DrawCard(Pad, () =>
                {
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
                });

                if (i < filtered.Count - 1) EditorUIUtility.CardGap(Pad);
            }
            EditorGUILayout.EndScrollView();
        }

        private static Vector2 _effectScroll = Vector2.zero;

        private static void DrawEffectTree(AbilityEditorModel model, string searchText,
            Action<ScriptableObject> onSelected)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                _effectScroll = EditorGUILayout.BeginScrollView(_effectScroll,
                    GUILayout.ExpandHeight(true));
                var nullSO = (AbilitySO)null;
                AbilityTreeView.DrawTree(model.EffectTreeRoots, _effectFoldouts, ref nullSO,
                    searchText, AbilityTypeFilter.All,
                    onLeafSelected: asset => onSelected(asset),
                    selectedEffect: _selectedAsset as EffectSO);
                EditorGUILayout.EndScrollView();
            });
        }

        // ── 摘要（委托给 AbilityEditorUtility）──
        private static string GetActivationSummary(ScriptableObject a)
            => a is AbilityActivationSO act ? AbilityEditorUtility.GetActivationSummary(act) : null;

        private static string GetSearchSummary(ScriptableObject s)
            => s is AbilitySearchSO search ? AbilityEditorUtility.GetSearchSummary(search) : null;

        private static string GetEffectSummary(ScriptableObject e)
            => e is EffectSO eff ? AbilityEditorUtility.GetEffectSummary(eff, includeDuration: false) : null;

        private static string GetNoiseSummary(ScriptableObject n)
            => n is NoiseEventSO noise ? AbilityEditorUtility.GetNoiseSummary(noise) : null;
    }
}
#endif
