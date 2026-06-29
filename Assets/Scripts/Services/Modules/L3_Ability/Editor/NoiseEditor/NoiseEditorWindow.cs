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
        private const float LeftWidth = 300f;

        // ── 内联 Model ──
        private List<NoiseEventSO> _allNoises = new();
        private List<EditorTreeNode> _treeRoots = new();

        // ── TreeView ──
        private EditorTreeView _treeView;

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private NoiseEventSO _selectedNoise;
        private string _searchText = "";
        private Vector2 _rightScroll;

        // ── EditorForm ──
        private Rect _noiseTagButtonRect;

        [MenuItem("RedDust/Noise Editor", priority = 4)]
        public static void Open()
            => GetWindow<NoiseEditorWindow>("Noise Editor");

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
                EditorGUILayout.LabelField("Noise Editor", EditorStyles.largeLabel,
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
                    NoiseImportWindow.Open();

                if (EditorButton.Draw("+ Create", EditorButtonType.Success, EditorButtonSize.Medium))
                    CreateNewNoise();

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonType.Primary : EditorButtonType.Default, EditorButtonSize.Medium, enabled: _hasChanges))
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
                    SelectNoise(node.UserData as NoiseEventSO);
                },
                onDelete: node => DeleteNoise(node.UserData as NoiseEventSO));
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
            if (_selectedNoise == null)
            {
                EditorCard.Draw(() =>
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select a noise from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                });
                return;
            }

            EditorCard.Draw($"Edit: {_selectedNoise.name}", () =>
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
            var n = _selectedNoise;

            EditorCard.Draw("Noise", () =>
            {
                EditorForm.Draw(n, form =>
                {
                    EditorFormItem.RawField("Name", labelWidth: null,
                        getValue: () => n.name,
                        setValue: v =>
                        {
                            var newName = (string)v;
                            if (string.IsNullOrWhiteSpace(newName)) return;
                            RenameNoise(n, newName);
                        },
                        drawFunc: v => EditorGUILayout.TextField((string)v),
                        equals: (x, y) => (string)x == (string)y);
                    EditorFormItem.ObjectFieldWithTag<rTagDefSO>(
                        "noiseType", ref _noiseTagButtonRect, label: "Noise Type", rootFilter: TagDomainFilter.NOISE_TYPE);
                    EditorFormItem.Float("level");
                    EditorFormItem.Float("decayRadius");
                    form.OnChange += () =>
                    {
                        MarkDirty();
                        _needsRefresh = true;  // noiseType 变更需重建标签树
                    };
                });
            });
        }

        // ═══════════════════════════════════════════════════
        // StatusBar
        // ═══════════════════════════════════════════════════
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);

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

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // Model
        // ═══════════════════════════════════════════════════
        private void RefreshModel()
        {
            _allNoises.Clear();

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
            var nodeIndex = new Dictionary<string, EditorTreeNode>();

            foreach (var noise in _allNoises)
            {
                var tag = noise.noiseType;

                if (tag != null)
                {
                    var tagChain = new List<rTagDefSO>();
                    var t = tag;
                    while (t != null)
                    {
                        tagChain.Add(t);
                        t = t.Parent;
                    }
                    tagChain.Reverse();

                    EditorTreeNode parentNode = null;
                    var accumPath = "";
                    for (int i = 0; i < tagChain.Count; i++)
                    {
                        var ct = tagChain[i];
                        accumPath = i == 0 ? ct.LeafName : $"{accumPath}.{ct.LeafName}";

                        if (!nodeIndex.TryGetValue(accumPath, out var folderNode))
                        {
                            folderNode = new EditorTreeNode
                            {
                                DisplayName = ct.LeafName,
                                FullPath = accumPath,
                                Depth = i + 1,
                                IsFolder = true,
                                UserData = ct,
                                Parent = parentNode,
                            };
                            nodeIndex[accumPath] = folderNode;

                            if (parentNode != null)
                                parentNode.Children.Add(folderNode);
                            else
                                _treeRoots.Add(folderNode);
                        }
                        parentNode = folderNode;
                    }

                    var leaf = new EditorTreeNode
                    {
                        DisplayName = noise.name,
                        FullPath = $"{parentNode.FullPath}/{noise.name}",
                        Depth = parentNode.Depth + 1,
                        IsFolder = false,
                        UserData = noise,
                        Parent = parentNode,
                    };
                    parentNode.Children.Add(leaf);
                }
                else
                {
                    AddToFolder("Uncategorized", 0, noise, null, nodeIndex);
                }
            }

            EditorTree.SortTreeRecursive(_treeRoots);
            EditorTree.ComputeTreeCounts(_treeRoots);
        }

        private void AddToFolder(string folderName, int depth, NoiseEventSO noise, EditorTreeNode parent,
            Dictionary<string, EditorTreeNode> nodeIndex)
        {
            if (!nodeIndex.TryGetValue(folderName, out var folderNode))
            {
                folderNode = new EditorTreeNode
                {
                    DisplayName = folderName,
                    FullPath = folderName,
                    Depth = depth,
                    IsFolder = true,
                    Parent = parent,
                };
                nodeIndex[folderName] = folderNode;
                _treeRoots.Add(folderNode);
            }

            var leaf = new EditorTreeNode
            {
                DisplayName = noise.name,
                FullPath = $"{folderName}/{noise.name}",
                Depth = depth + 1,
                IsFolder = false,
                UserData = noise,
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
