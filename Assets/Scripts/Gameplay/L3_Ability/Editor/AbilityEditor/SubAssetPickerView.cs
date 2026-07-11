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
        private static ScriptableObject _selectedAsset;
        private static Vector2 _typedListScroll;
        private static EditorTreeView _effectTreeView;

        public static void DrawPicker(
            AbilityEditorModel model,
            SubAssetSlot slot,
            ref string searchText,
            Action<ScriptableObject> onSelected,
            Action onCreateNew,
            Action onCancel)
        {
            var s = searchText;

            if (slot == SubAssetSlot.None)
            {
                EditorCard.Draw(() =>
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorLabel.Draw("Click a sub-asset slot\nin the middle panel to assign.",
                        style: EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                });
                searchText = s;
                return;
            }

            EditorCard.Draw(() =>
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
                EditorLabel.Draw(title, style: EditorStyles.boldLabel);
                EditorCard.Gap(EditorTokens.Pad);

                // 搜索
                s = EditorSearchBar.Draw(s, labelWidth: 50f);

                EditorCard.Gap(EditorTokens.Pad);

                // 列表 — 点击高亮，不直接确认
                DrawAssetList(model, slot, s, asset => _selectedAsset = asset);

                EditorCard.Gap(EditorTokens.Pad);

                // 底部按钮
                var hasSelection = _selectedAsset != null;
                EditorGUILayout.BeginHorizontal();
                if (slot != SubAssetSlot.TargetEffects && slot != SubAssetSlot.SelfEffects)
                {
                    if (EditorButton.Draw("Edit in Editor"))
                        OpenStandaloneEditor(slot);
                    if (EditorButton.Draw("+ Create New"))
                        OpenStandaloneEditor(slot);
                }
                GUILayout.FlexibleSpace();
                if (EditorButton.Draw("Cancel", size: EditorButtonSize.Medium))
                {
                    _selectedAsset = null;
                    onCancel?.Invoke();
                }
                if (EditorButton.Draw("Select", hasSelection ? EditorButtonType.Primary : EditorButtonType.Default,
                        EditorButtonSize.Medium, enabled: hasSelection))
                {
                    onSelected?.Invoke(_selectedAsset);
                    _selectedAsset = null;
                }
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
                EditorLabel.Draw(
                    q != null ? "No matches. Create new?" : "No assets yet. Create new?",
                    style: EditorUIUtility.GreyPlaceholder);
                return;
            }

            _typedListScroll = EditorGUILayout.BeginScrollView(
                _typedListScroll, GUILayout.ExpandHeight(true));
            for (var i = 0; i < filtered.Count; i++)
            {
                var asset = filtered[i];

                EditorCard.Draw(() =>
                {
                    if (GUILayout.Button(asset.name, GUILayout.ExpandWidth(true)))
                        onSelected?.Invoke(asset);

                    var summary = getSummary(asset);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        EditorLabel.Draw(summary, style: EditorTokens.DimLabelStyle);
                    }
                });

                if (i < filtered.Count - 1) EditorCard.Gap(EditorTokens.Pad);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawEffectTree(AbilityEditorModel model, string searchText,
            Action<ScriptableObject> onSelected)
        {
            if (_effectTreeView == null)
                _effectTreeView = new EditorTreeView();

            _effectTreeView.SetData(model.EffectTreeRoots, onSelect: node =>
            {
                var asset = node.UserData as ScriptableObject;
                if (asset != null) onSelected(asset);
            });
            _effectTreeView.searchString = searchText;

            var rect = EditorGUILayout.GetControlRect(
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _effectTreeView.OnGUI(rect);
        }

        private static void OpenStandaloneEditor(SubAssetSlot slot)
        {
            switch (slot)
            {
                case SubAssetSlot.Activation:
                    ActivationEditorWindow.Open();
                    break;
                case SubAssetSlot.Search:
                    SearchEditorWindow.Open();
                    break;
                case SubAssetSlot.Noise:
                    NoiseEditorWindow.Open();
                    break;
                // Effect slots have their own tree picker, no standalone redirect needed
            }
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
