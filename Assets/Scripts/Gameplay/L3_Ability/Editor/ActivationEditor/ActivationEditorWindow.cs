#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 独立 Activation 编辑器。浏览、创建、编辑所有 AbilityActivationSO 资产。
    /// 对标 SearchEditorWindow / EffectEditorWindow 模式。
    /// </summary>
    public class ActivationEditorWindow : EditorWindow
    {
        private const float LeftWidth = 300f;

        // ── 内联 Model ──
        private List<AbilityActivationSO> _allActivations = new();
        private List<EditorTreeNode> _treeRoots = new();

        // ── TreeView ──
        private EditorTreeView _treeView;

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private AbilityActivationSO _selectedActivation;
        private string _searchText = "";
        private Vector2 _rightScroll;

        [MenuItem("RedDust/Activation Editor", priority = 3)]
        public static void Open()
            => GetWindow<ActivationEditorWindow>("Activation Editor");

        private void OnEnable()
        {
            _treeView = new EditorTreeView();
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorCard.Gap(EditorTokens.Pad);
            DrawTwoColumns();
            EditorCard.Gap(EditorTokens.Pad);
            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
        }

        // ═══════════════════════════════════════════════════
        // Header
        // ═══════════════════════════════════════════════════
        private void DrawHeader()
        {
            EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Activation Editor", EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var sub = EditorTokens.BreadcrumbStyle;
                EditorGUILayout.LabelField("L3_Ability · Editor", sub, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorTokens.Pad);

                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("Refresh", size: EditorButtonSize.Medium))
                    RefreshAll();
                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("Import/Export", size: EditorButtonSize.Medium))
                    ActivationImportWindow.Open();

                if (EditorButton.Draw("+ Create", EditorButtonType.Success, EditorButtonSize.Medium))
                    CreateNewActivation();

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonType.Primary : EditorButtonType.Default, EditorButtonSize.Medium, enabled: _hasChanges))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }

                if (_selectedActivation != null)
                {
                    if (EditorButton.Draw("Ping", size: EditorButtonSize.Medium))
                        EditorGUIUtility.PingObject(_selectedActivation);
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        // ═══════════════════════════════════════════════════
        // 两栏布局
        // ═══════════════════════════════════════════════════
        private void DrawTwoColumns()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            DrawLeftColumn();
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(EditorTokens.Pad);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏 ──
        private void DrawLeftColumn()
        {
            if (_needsRefresh)
            {
                RefreshModel();
                _treeView.SetData(_treeRoots, onSelect: node =>
                {
                    SelectActivation(node.UserData as AbilityActivationSO);
                },
                onDelete: node => DeleteActivation(node.UserData as AbilityActivationSO));
                _needsRefresh = false;
            }
            _treeView.searchString = _searchText;

            EditorCard.Draw(() =>
            {
                EditorCard.Draw(() =>
                {
                    var s = EditorSearchBar.Draw(_searchText, labelWidth: 42f);
                    if (s != _searchText) { _searchText = s; }
                });

                EditorCard.Gap(EditorTokens.Pad);

                var rect = EditorGUILayout.GetControlRect(
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _treeView.OnGUI(rect);
            });
        }

        // ── 右栏 ──
        private void DrawRightColumn()
        {
            if (_selectedActivation == null)
            {
                EditorCard.Draw(() =>
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select an activation from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                });
                return;
            }

            EditorCard.Draw($"Edit: {_selectedActivation.name}", () =>
            {
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                DrawEditForm();
                EditorGUILayout.EndScrollView();
            });
        }

        // ═══════════════════════════════════════════════════
        // 编辑表单
        // ═══════════════════════════════════════════════════

        private void DrawEditForm()
        {
            var a = _selectedActivation;

            EditorForm.Draw(a, form =>
            {
                form.DefaultLabelWidth = 100;
                EditorFormItem.RawField("Name", 100,
                    getValue: () => a.name,
                    setValue: v =>
                    {
                        var n = (string)v;
                        if (string.IsNullOrWhiteSpace(n)) return;
                        RenameActivation(a, n);
                    },
                    drawFunc: v => EditorGUILayout.TextField((string)v),
                    equals: (x, y) => (string)x == (string)y);
                EditorFormItem.Enum<EActivationType>("activationType");
                EditorFormItem.Float("maxChargeTime");
                EditorFormItem.Toggle("autoReleaseAtFullCharge");
                EditorFormItem.ObjectField<AnimationClip>("animationClip");
                EditorFormItem.Enum<EAbilityAnimationLayer>("animationLayer");
                EditorFormItem.Slider("animationSpeed", 0.1f, 3f);
                EditorFormItem.Toggle("rootMotion");
                EditorFormItem.Float("windupDuration");
                EditorFormItem.Float("fireWindowDuration");
                EditorFormItem.Toggle("canCancelWindup");
                EditorFormItem.Toggle("canCancelRecovery");
                form.OnChange += MarkDirty;
            });
        }

        // ═══════════════════════════════════════════════════
        // StatusBar
        // ═══════════════════════════════════════════════════
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

            int instant = 0, charged = 0, channel = 0;
            foreach (var a in _allActivations)
            {
                if (a.activationType == EActivationType.Instant) instant++;
                else if (a.activationType == EActivationType.Charged) charged++;
                else if (a.activationType == EActivationType.Channel) channel++;
            }
            EditorGUILayout.LabelField(
                $"{_allActivations.Count} activations · {instant} Instant · {charged} Charged · {channel} Channel",
                EditorStyles.miniLabel);

            if (_selectedActivation != null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"{_selectedActivation.name} ({_selectedActivation.activationType})",
                    EditorStyles.miniLabel);
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // Model
        // ═══════════════════════════════════════════════════
        private void RefreshModel()
        {
            _allActivations.Clear();

            var guids = AssetDatabase.FindAssets("t:AbilityActivationSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<AbilityActivationSO>(path);
                if (a != null) _allActivations.Add(a);
            }
            _allActivations.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildTree();
        }

        private void BuildTree()
        {
            _treeRoots.Clear();
            var folderIndex = new Dictionary<string, EditorTreeNode>();

            foreach (var activation in _allActivations)
            {
                var folderName = activation.activationType.ToString();
                var folderPath = $"act_{folderName}";

                if (!folderIndex.TryGetValue(folderPath, out var folderNode))
                {
                    folderNode = new EditorTreeNode
                    {
                        DisplayName = folderName,
                        FullPath = folderPath,
                        Depth = 0,
                        IsFolder = true,
                    };
                    folderIndex[folderPath] = folderNode;
                    _treeRoots.Add(folderNode);
                }

                var leaf = new EditorTreeNode
                {
                    DisplayName = activation.name,
                    FullPath = $"{folderPath}/{activation.name}",
                    Depth = 1,
                    IsFolder = false,
                    UserData = activation,
                    Parent = folderNode,
                };
                folderNode.Children.Add(leaf);
            }

            EditorTree.SortTreeRecursive(_treeRoots);
            EditorTree.ComputeTreeCounts(_treeRoots);
        }

        // ═══════════════════════════════════════════════════
        // Actions
        // ═══════════════════════════════════════════════════
        private void SelectActivation(AbilityActivationSO activation)
        {
            if (activation == null) return;
            _selectedActivation = activation;
            Repaint();
        }

        private void RenameActivation(AbilityActivationSO activation, string newName)
        {
            var path = AssetDatabase.GetAssetPath(activation);
            if (string.IsNullOrEmpty(path)) return;
            var result = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"[ActivationEditor] Rename failed: {result}");
                return;
            }
            activation.name = newName;
            EditorUtility.SetDirty(activation);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void MarkDirty()
        {
            if (_selectedActivation == null) return;
            EditorUtility.SetDirty(_selectedActivation);
            _hasChanges = true;
        }

        private void DeleteActivation(AbilityActivationSO activation)
        {
            if (!AbilityEditorUtility.DeleteAssetWithConfirm(activation, "Activation"))
                return;
            if (_selectedActivation == activation)
                _selectedActivation = null;
            _needsRefresh = true;
            _hasChanges = false;
            Repaint();
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _selectedActivation = null;
            Repaint();
        }

        // ═══════════════════════════════════════════════════
        // Create
        // ═══════════════════════════════════════════════════
        private void CreateNewActivation()
        {
            var dir = "Assets/Data/Ability/Activations";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/Activation_New.asset");
            var instance = ScriptableObject.CreateInstance<AbilityActivationSO>();
            instance.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            _needsRefresh = true;
            _hasChanges = true;
            _selectedActivation = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[ActivationEditor] Created {path}");
        }
    }
}
#endif
