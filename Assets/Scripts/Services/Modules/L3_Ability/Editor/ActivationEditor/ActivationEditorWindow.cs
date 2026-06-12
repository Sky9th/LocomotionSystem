#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 独立 Activation 编辑器。浏览、创建、编辑所有 AbilityActivationSO 资产。
    /// 对标 SearchEditorWindow / EffectEditorWindow 模式。
    /// </summary>
    public class ActivationEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float LeftWidth = 300f;

        // ── 内联 Model ──
        private List<AbilityActivationSO> _allActivations = new();
        private List<AbilityTreeNode> _treeRoots = new();
        private readonly Dictionary<string, AbilityTreeNode> _treeNodeIndex = new();

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private AbilityActivationSO _selectedActivation;
        private string _searchText = "";
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        // ── EditorForm ──
        private EditorForm _form;

        [MenuItem("RedDust/Activation Editor", priority = 3)]
        private static void Open()
            => GetWindow<ActivationEditorWindow>("Activation Editor");

        private void OnEnable()
        {
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            if (_needsRefresh) { RefreshModel(); _needsRefresh = false; }

            GUILayout.Space(Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorUIUtility.CardGap(Pad);
            DrawTwoColumns();
            EditorUIUtility.CardGap(Pad);
            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(Pad);
        }

        // ═══════════════════════════════════════════════════
        // Header
        // ═══════════════════════════════════════════════════
        private void DrawHeader()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Activation Editor", EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var sub = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField("L3_Ability · Editor", sub, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(Pad);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh", GUILayout.Height(22)))
                    RefreshAll();
                GUILayout.FlexibleSpace();

                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = EditorUIUtility.ColorGreenDark;
                if (GUILayout.Button("+ Create", GUILayout.Width(70), GUILayout.Height(22)))
                    CreateNewActivation();
                GUI.backgroundColor = oldBg;

                GUI.enabled = _hasChanges;
                GUI.backgroundColor = _hasChanges ? EditorUIUtility.ColorGreen : Color.white;
                if (GUILayout.Button(_hasChanges ? "Save *" : "Saved", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }
                GUI.enabled = true;
                GUI.backgroundColor = oldBg;

                if (_selectedActivation != null)
                {
                    if (GUILayout.Button("Ping", GUILayout.Height(22)))
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

            EditorUIUtility.CardGap(Pad);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏 ──
        private void DrawLeftColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                // 搜索框
                EditorUIUtility.DrawCard(Pad, () =>
                {
                    var s = EditorUIUtility.DrawSearchRow(_searchText, labelWidth: 42f);
                    if (s != _searchText) { _searchText = s; }
                });

                EditorUIUtility.CardGap(Pad);

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                var nullSO = (AbilitySO)null;
                AbilityTreeView.DrawTree(_treeRoots, _foldouts, ref nullSO,
                    _searchText, AbilityTypeFilter.All,
                    onLeafSelected: asset => SelectActivation(asset as AbilityActivationSO));
                EditorGUILayout.EndScrollView();
            });
        }

        // ── 右栏 ──
        private void DrawRightColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                if (_selectedActivation == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select an activation from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    return;
                }

                var title = $"Edit: {_selectedActivation.name}";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.Space(Pad);

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

            if (EditorForm.NeedsRebuild(_form, a))
            {
                _form = new EditorForm(a) { DefaultLabelWidth = 100 };
                _form.Enum<EActivationType>("activationType")
                     .Float("maxChargeTime")
                     .Toggle("autoReleaseAtFullCharge")
                     .ObjectField<StringAsset>("animationAsset")
                     .Enum<EAbilityAnimationLayer>("animationLayer")
                     .Slider("animationSpeed", 0.1f, 3f)
                     .Toggle("rootMotion")
                     .Float("windupDuration")
                     .Float("fireWindowDuration")
                     .Toggle("canCancelWindup")
                     .Toggle("canCancelRecovery");
                _form.OnAnyChange += MarkDirty;
            }
            _form?.Draw();
        }

        // ═══════════════════════════════════════════════════
        // StatusBar
        // ═══════════════════════════════════════════════════
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

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

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // Model
        // ═══════════════════════════════════════════════════
        private void RefreshModel()
        {
            _allActivations.Clear();
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

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
            _treeNodeIndex.Clear();

            foreach (var activation in _allActivations)
            {
                var folderName = activation.activationType.ToString();
                var folderPath = $"act_{folderName}";

                if (!_treeNodeIndex.TryGetValue(folderPath, out var folderNode))
                {
                    folderNode = new AbilityTreeNode
                    {
                        DisplayName = folderName,
                        FullPath = folderPath,
                        Depth = 0,
                        IsFolder = true,
                    };
                    _treeNodeIndex[folderPath] = folderNode;
                    _treeRoots.Add(folderNode);
                }

                var leaf = new AbilityTreeNode
                {
                    DisplayName = activation.name,
                    FullPath = $"{folderPath}/{activation.name}",
                    Depth = 1,
                    IsFolder = false,
                    Ability = null,
                    Parent = folderNode,
                };
                folderNode.Children.Add(leaf);
            }

            AbilityEditorUtility.SortTreeRecursive(_treeRoots);
            AbilityEditorUtility.ComputeTreeCounts(_treeRoots);
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

        private void MarkDirty()
        {
            if (_selectedActivation == null) return;
            EditorUtility.SetDirty(_selectedActivation);
            _hasChanges = true;
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _foldouts.Clear();
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
