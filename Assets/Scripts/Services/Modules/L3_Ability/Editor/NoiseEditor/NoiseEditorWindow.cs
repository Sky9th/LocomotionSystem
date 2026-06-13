#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using RedDust.Core.Editor;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 独立 Noise 编辑器。浏览、创建、编辑所有 NoiseEventSO 资产。
    /// 对标 ActivationEditorWindow 模式。
    /// </summary>
    public class NoiseEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float LeftWidth = 300f;

        // ── 内联 Model ──
        private List<NoiseEventSO> _allNoises = new();
        private List<AbilityTreeNode> _treeRoots = new();
        private readonly Dictionary<string, AbilityTreeNode> _treeNodeIndex = new();

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private NoiseEventSO _selectedNoise;
        private string _searchText = "";
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        // ── EditorForm ──
        private EditorForm _form;
        private Rect _noiseTagButtonRect;

        [MenuItem("RedDust/Noise Editor", priority = 4)]
        public static void Open()
            => GetWindow<NoiseEditorWindow>("Noise Editor");

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
            EditorCard.Gap(Pad);
            DrawTwoColumns();
            EditorCard.Gap(Pad);
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
            EditorCard.Draw(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Noise Editor", EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var sub = new GUIStyle(EditorStyles.label)
                    { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField("L3_Ability · Editor", sub, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(Pad);

                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("Refresh", size: EditorButtonSize.Medium))
                    RefreshAll();
                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("Import/Export", size: EditorButtonSize.Medium))
                    NoiseImportWindow.Open();

                if (EditorButton.Draw("+ Create", EditorButtonStyle.Success, EditorButtonSize.Medium))
                    CreateNewNoise();

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonStyle.Primary : EditorButtonStyle.Default, EditorButtonSize.Medium, enabled: _hasChanges))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }

                if (_selectedNoise != null)
                {
                    if (EditorButton.Draw("Ping", size: EditorButtonSize.Medium))
                        EditorGUIUtility.PingObject(_selectedNoise);
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

            EditorCard.Gap(Pad);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏 ──
        private void DrawLeftColumn()
        {
            EditorCard.Draw(Pad, () =>
            {
                // 搜索框
                EditorCard.DrawLight(Pad, () =>
                {
                    var s = EditorUIUtility.DrawSearchRow(_searchText, labelWidth: 42f);
                    if (s != _searchText) { _searchText = s; }
                });

                EditorCard.Gap(Pad);

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                var nullSO = (AbilitySO)null;
                AbilityTreeView.DrawTree(_treeRoots, _foldouts, ref nullSO,
                    _searchText, AbilityTypeFilter.All,
                    onLeafSelected: asset => SelectNoise(asset as NoiseEventSO),
                    onDeleteLeaf: asset => DeleteNoise(asset as NoiseEventSO));
                EditorGUILayout.EndScrollView();
            });
        }

        // ── 右栏 ──
        private void DrawRightColumn()
        {
            EditorCard.Draw(Pad, () =>
            {
                if (_selectedNoise == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select a noise from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    return;
                }

                var title = $"Edit: {_selectedNoise.name}";
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
            var n = _selectedNoise;

            if (EditorForm.NeedsRebuild(_form, n))
            {
                _form = new EditorForm(n) { DefaultLabelWidth = 100 };
                _form.RawField("Name", 100,
                        getValue: () => n.name,
                        setValue: v => { n.name = (string)v; },
                        drawFunc: v => EditorGUILayout.TextField((string)v),
                        equals: (x, y) => (string)x == (string)y)
                    .CustomOnChange((_, newVal) =>
                    {
                        var newName = (string)newVal;
                        if (string.IsNullOrWhiteSpace(newName)) return false;
                        RenameNoise(n, newName);
                        return true;
                    })
                     .ObjectField<GameplayTagDefinitionSO>("noiseType", label: "Noise Type")
                        .PostInput(() =>
                        {
                            if (EditorButton.Draw("Tag", size: EditorButtonSize.Small, width: 35f))
                            {
                                TagPicker.Show(_noiseTagButtonRect, allowCreate: true,
                                    currentFullTag: n.noiseType?.FullTag,
                                    onSelected: t =>
                                    {
                                        if (n.noiseType != t)
                                        {
                                            n.noiseType = t;
                                            EditorUtility.SetDirty(n);
                                            _hasChanges = true;
                                            _needsRefresh = true;
                                            _form = null;
                                        }
                                    });
                            }
                            if (Event.current.type == EventType.Repaint)
                                _noiseTagButtonRect = GUILayoutUtility.GetLastRect();
                        })
                     .Float("level")
                     .Float("decayRadius");
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

            EditorGUILayout.LabelField(
                $"{_allNoises.Count} noises",
                EditorStyles.miniLabel);

            if (_selectedNoise != null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"{_selectedNoise.name} (level:{_selectedNoise.level:F0})",
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
            _allNoises.Clear();
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            var guids = AssetDatabase.FindAssets("t:NoiseEventSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var n = AssetDatabase.LoadAssetAtPath<NoiseEventSO>(path);
                if (n != null) _allNoises.Add(n);
            }
            _allNoises.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildTree();
        }

        private void BuildTree()
        {
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            foreach (var noise in _allNoises)
            {
                var tag = noise.noiseType;

                // 按 noiseType 的 parent 链构建文件夹
                if (tag != null)
                {
                    var tagChain = new List<GameplayTagDefinitionSO>();
                    var t = tag;
                    while (t != null)
                    {
                        tagChain.Add(t);
                        t = t.Parent;
                    }
                    tagChain.Reverse();

                    AbilityTreeNode parentNode = null;
                    var accumPath = "";
                    for (int i = 0; i < tagChain.Count; i++)
                    {
                        var ct = tagChain[i];
                        accumPath = i == 0 ? ct.LeafName : $"{accumPath}.{ct.LeafName}";

                        if (!_treeNodeIndex.TryGetValue(accumPath, out var folderNode))
                        {
                            folderNode = new AbilityTreeNode
                            {
                                DisplayName = ct.LeafName,
                                FullPath = accumPath,
                                Depth = i + 1,
                                IsFolder = true,
                                Tag = ct,
                                Parent = parentNode,
                            };
                            _treeNodeIndex[accumPath] = folderNode;

                            if (parentNode != null)
                                parentNode.Children.Add(folderNode);
                            else
                                _treeRoots.Add(folderNode);
                        }
                        parentNode = folderNode;
                    }

                    var leaf = new AbilityTreeNode
                    {
                        DisplayName = noise.name,
                        FullPath = $"{parentNode.FullPath}/{noise.name}",
                        Depth = parentNode.Depth + 1,
                        IsFolder = false,
                        Noise = noise,
                        Parent = parentNode,
                    };
                    parentNode.Children.Add(leaf);
                }
                else
                {
                    // 无 noiseType → Uncategorized
                    AddToFolder("Uncategorized", 0, noise, null);
                }
            }

            AbilityEditorUtility.SortTreeRecursive(_treeRoots);
            AbilityEditorUtility.ComputeTreeCounts(_treeRoots);
        }

        private void AddToFolder(string folderName, int depth, NoiseEventSO noise, AbilityTreeNode parent)
        {
            if (!_treeNodeIndex.TryGetValue(folderName, out var folderNode))
            {
                folderNode = new AbilityTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                _treeNodeIndex[folderName] = folderNode;
                _treeRoots.Add(folderNode);
            }

            var leaf = new AbilityTreeNode
            {
                DisplayName = noise.name,
                FullPath = $"{folderName}/{noise.name}",
                Depth = depth + 1,
                IsFolder = false,
                Noise = noise,
                Parent = folderNode,
            };
            folderNode.Children.Add(leaf);
        }

        // ═══════════════════════════════════════════════════
        // Actions
        // ═══════════════════════════════════════════════════
        private void SelectNoise(NoiseEventSO noise)
        {
            if (noise == null) return;
            _selectedNoise = noise;
            Repaint();
        }

        private void RenameNoise(NoiseEventSO noise, string newName)
        {
            var path = AssetDatabase.GetAssetPath(noise);
            if (string.IsNullOrEmpty(path)) return;
            var result = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"[NoiseEditor] Rename failed: {result}");
                return;
            }
            noise.name = newName;
            EditorUtility.SetDirty(noise);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void MarkDirty()
        {
            if (_selectedNoise == null) return;
            EditorUtility.SetDirty(_selectedNoise);
            _hasChanges = true;
        }

        private void DeleteNoise(NoiseEventSO noise)
        {
            if (!AbilityEditorUtility.DeleteAssetWithConfirm(noise, "Noise"))
                return;
            if (_selectedNoise == noise)
                _selectedNoise = null;
            _needsRefresh = true;
            _hasChanges = false;
            Repaint();
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _foldouts.Clear();
            _selectedNoise = null;
            Repaint();
        }

        // ═══════════════════════════════════════════════════
        // Create
        // ═══════════════════════════════════════════════════
        private void CreateNewNoise()
        {
            var dir = "Assets/Data/Ability/Noises";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/Noise_New.asset");
            var instance = ScriptableObject.CreateInstance<NoiseEventSO>();
            instance.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            _needsRefresh = true;
            _hasChanges = true;
            _selectedNoise = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[NoiseEditor] Created {path}");
        }
    }
}
#endif
