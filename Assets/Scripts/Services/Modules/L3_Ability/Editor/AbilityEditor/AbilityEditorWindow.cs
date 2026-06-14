#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    public class AbilityEditorWindow : EditorWindow
    {
        private const float LeftWidth = 280f;
        private const float RightWidth = 360f;

        private AbilityEditorModel _model;
        private bool _needsRefresh = true;
        private bool _hasChanges;

        private string _searchText = "";
        private AbilityTypeFilter _filter = AbilityTypeFilter.All;
        private AbilitySO _selectedAbility;
        private Vector2 _leftScroll;
        private Vector2 _middleScroll;
        private SubAssetSlot _activeSlot;
        private string _rightSearchText = "";
        private readonly Dictionary<string, bool> _foldouts = new();

        [MenuItem("RedDust/Ability Editor", priority = 0)]
        private static void Open()
            => GetWindow<AbilityEditorWindow>("Ability Editor");

        private void OnEnable()
        {
            _model = new AbilityEditorModel();
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            if (_needsRefresh) { _model.Refresh(); _needsRefresh = false; }

            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorCard.Gap(EditorTokens.Pad);

            DrawThreeColumns();
            EditorCard.Gap(EditorTokens.Pad);

            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(EditorTokens.Pad);
        }

        // ── Header ──
        private void DrawHeader()
        {
            EditorCard.Draw(EditorTokens.Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ability Editor", EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var rightStyle = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField("L3_Ability · Editor", rightStyle,
                    GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorTokens.Pad);

                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("Refresh", size: EditorButtonSize.Medium))
                    RefreshAll();
                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("+ Create New", EditorButtonType.Primary, EditorButtonSize.Medium))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Active Ability"), false,
                        () => AbilityListView.CreateAbility<AbilityDefSO>("Ability_New", "Assets/Data/Ability/Actives", a => { _selectedAbility = a; _needsRefresh = true; _hasChanges = true; EditorGUIUtility.PingObject(a); }));
                    menu.AddItem(new GUIContent("Passive Ability"), false,
                        () => AbilityListView.CreateAbility<PassiveAbilitySO>("Passive_New", "Assets/Data/Ability/Passives", a => { _selectedAbility = a; _needsRefresh = true; _hasChanges = true; EditorGUIUtility.PingObject(a); }));
                    menu.ShowAsContext();
                }

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonType.Primary : EditorButtonType.Default, EditorButtonSize.Medium, enabled: _hasChanges))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }

                if (_selectedAbility != null)
                {
                    if (EditorButton.Draw("Ping", size: EditorButtonSize.Medium))
                        EditorGUIUtility.PingObject(_selectedAbility);
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        // ── 三栏 ──
        private void DrawThreeColumns()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLeftColumn();
            EditorCard.Gap(EditorTokens.Pad);
            DrawMiddleColumn();

            EditorCard.Gap(EditorTokens.Pad);
            DrawRightColumn();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏：Ability 列表 ──
        private void DrawLeftColumn()
        {
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            EditorCard.Draw(EditorTokens.Pad, () =>
            {
                // filter
                AbilityListView.DrawFilterCard(_filter, f => _filter = f);
                EditorCard.Gap(EditorTokens.Pad);

                // search
                AbilityListView.DrawSearchCard(_searchText, s => _searchText = s);
                EditorCard.Gap(EditorTokens.Pad);

                // tree
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                AbilityTreeView.DrawTree(_model.TreeRoots, _foldouts, ref _selectedAbility,
                    _searchText, _filter);
                EditorGUILayout.EndScrollView();
            });
            EditorGUILayout.EndHorizontal();
        }

        // ── 中间栏：Ability 编辑 ──
        private void DrawMiddleColumn()
        {
            EditorGUILayout.BeginHorizontal(
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorCard.Draw(EditorTokens.Pad, () =>
            {
                var midTitle = _selectedAbility != null
                    ? $"Edit: {_selectedAbility.displayName ?? _selectedAbility.name}"
                    : "Properties";
                EditorGUILayout.LabelField(midTitle, EditorStyles.boldLabel);

                GUILayout.Space(EditorTokens.Pad);

                _middleScroll = EditorGUILayout.BeginScrollView(_middleScroll);
                if (_selectedAbility == null)
                    AbilityEditorMiddlePanel.DrawEmpty();
                else
                    AbilityEditorMiddlePanel.DrawEdit(_selectedAbility,
                        onEditSubAsset: slot =>
                        { _activeSlot = slot; _rightSearchText = ""; },
                        onClearSubAsset: slot => ClearSubAsset(slot),
                        onRemoveEffect: (index, isTarget) => RemoveEffectFromAbility(index, isTarget),
                        onChanged: () => _hasChanges = true);
                EditorGUILayout.EndScrollView();
            });
            EditorGUILayout.EndHorizontal();
        }

        // ── 右栏：子资产 Picker / Creator ──
        private void DrawRightColumn()
        {
            EditorGUILayout.BeginHorizontal(
                GUILayout.Width(RightWidth), GUILayout.ExpandHeight(true));
            SubAssetPickerView.DrawPicker(_model, _activeSlot, ref _rightSearchText,
                onSelected: asset => AssignSubAsset(_activeSlot, asset),
                onCreateNew: () => Debug.Log("[AbilityEditor] Create sub-asset — Phase 3"),
                onCancel: () => { _rightSearchText = ""; _activeSlot = SubAssetSlot.None; });
            EditorGUILayout.EndHorizontal();
        }

        private void AssignSubAsset(SubAssetSlot slot, ScriptableObject asset)
        {
            if (_selectedAbility == null) return;

            switch (slot)
            {
                case SubAssetSlot.Activation:
                    if (_selectedAbility is AbilityDefSO def && asset is AbilityActivationSO act)
                    { def.activation = act; EditorUtility.SetDirty(def); }
                    break;
                case SubAssetSlot.Search:
                    if (_selectedAbility is AbilityDefSO def2 && asset is AbilitySearchSO search)
                    { def2.search = search; EditorUtility.SetDirty(def2); }
                    break;
                case SubAssetSlot.Noise:
                    if (_selectedAbility is AbilityDefSO def3 && asset is NoiseEventSO noise)
                    { def3.noise = noise; EditorUtility.SetDirty(def3); }
                    break;
                case SubAssetSlot.TargetEffects:
                    if (asset is EffectSO effect)
                        AddEffect(ref _selectedAbility.targetEffects, effect);
                    break;
                case SubAssetSlot.SelfEffects:
                    if (asset is EffectSO effect2)
                        AddEffect(ref _selectedAbility.selfEffects, effect2);
                    break;
            }

            _rightSearchText = "";
            _activeSlot = SubAssetSlot.None;
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void ClearSubAsset(SubAssetSlot slot)
        {
            if (_selectedAbility is not AbilityDefSO def) return;
            switch (slot)
            {
                case SubAssetSlot.Activation: def.activation = null; break;
                case SubAssetSlot.Search: def.search = null; break;
                case SubAssetSlot.Noise: def.noise = null; break;
            }
            EditorUtility.SetDirty(def);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void RemoveEffectFromAbility(int index, bool isTargetEffects)
        {
            if (_selectedAbility == null) return;
            ref var effects = ref isTargetEffects
                ? ref _selectedAbility.targetEffects
                : ref _selectedAbility.selfEffects;
            if (effects == null || index < 0 || index >= effects.Length) return;

            var newArr = new EffectSO[effects.Length - 1];
            for (int i = 0, j = 0; i < effects.Length; i++)
                if (i != index) newArr[j++] = effects[i];
            effects = newArr;
            EditorUtility.SetDirty(_selectedAbility);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private static void AddEffect(ref EffectSO[] effects, EffectSO effect)
        {
            var arr = effects ?? Array.Empty<EffectSO>();
            if (Array.IndexOf(arr, effect) >= 0)
            {
                EditorUtility.DisplayDialog("Duplicate Effect",
                    $"'{effect.name}' is already in this list.", "OK");
                return;
            }
            var newArr = new EffectSO[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[arr.Length] = effect;
            effects = newArr;
        }

        // ── StatusBar ──
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            EditorGUILayout.LabelField(
                $"{_model.TotalCount} abilities · {_model.AllDefs.Count} active · {_model.AllPassives.Count} passive",
                EditorStyles.miniLabel);

            if (_selectedAbility != null)
            {
                GUILayout.FlexibleSpace();
                var t = _selectedAbility is AbilityDefSO ? "Active" : "Passive";
                EditorGUILayout.LabelField(
                    $"{_selectedAbility.name} ({t})", EditorStyles.miniLabel);
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _foldouts.Clear();
            Repaint();
        }
    }
}
#endif
