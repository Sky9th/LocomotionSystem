#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using RedDust.Core.Editor;
using RedDust.Properties;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 独立 Effect 编辑器。浏览、创建、编辑所有 EffectSO 资产。
    /// 后续 Phase 可嵌入回 Ability Editor。
    /// </summary>
    public class EffectEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float LeftWidth = 300f;

        // ── 简易 Model（内联，扫描 EffectSO + 构建 effectTag 树）──
        private List<EffectSO> _allEffects = new();
        private List<AbilityTreeNode> _treeRoots = new();
        private readonly Dictionary<string, AbilityTreeNode> _treeNodeIndex = new();

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private EffectSO _selectedEffect;
        private string _searchText = "";

        // ── EditorForm ──
        private EditorForm _baseForm;
        private EditorForm _typeForm;
        private EffectTypeFilter _filter = EffectTypeFilter.All;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        [MenuItem("RedDust/Effect Editor", priority = 1)]
        private static void Open()
            => GetWindow<EffectEditorWindow>("Effect Editor");

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
                EditorGUILayout.LabelField("Effect Editor", EditorStyles.largeLabel,
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
                    EffectImportWindow.Open();

                if (EditorButton.Draw("+ Create", EditorButtonStyle.Success, EditorButtonSize.Medium))
                    CreateNewEffect();

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonStyle.Primary : EditorButtonStyle.Default, EditorButtonSize.Medium, enabled: _hasChanges))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }

                if (_selectedEffect != null)
                {
                    if (EditorButton.Draw("Ping", size: EditorButtonSize.Medium))
                        EditorGUIUtility.PingObject(_selectedEffect);
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

            // 左栏：树
            EditorGUILayout.BeginHorizontal(GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            DrawLeftColumn();
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(Pad);

            // 右栏：编辑
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏：列表 ──
        private void DrawLeftColumn()
        {
            EditorCard.Draw(Pad, () =>
            {
                DrawFilterCard();
                EditorCard.GapTight();
                DrawSearchCard();
                EditorCard.Gap(Pad);

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                var nullSO = (AbilitySO)null;
                AbilityTreeView.DrawTree(_treeRoots, _foldouts, ref nullSO,
                    _searchText, AbilityTypeFilter.All,
                    onLeafSelected: asset => SelectEffect(asset as EffectSO),
                    selectedEffect: _selectedEffect,
                    onDeleteLeaf: asset => DeleteEffect(asset as EffectSO));
                EditorGUILayout.EndScrollView();
            });
        }

        private void DrawFilterCard()
        {
            EditorCard.DrawLight(Pad, () =>
            {
                var newFilter = EditorUIUtility.DrawFilterTabBar(_filter,
                    new[] { EffectTypeFilter.All, EffectTypeFilter.Damage, EffectTypeFilter.Impact, EffectTypeFilter.Execute, EffectTypeFilter.Cost },
                    new[] { "All", "Dmg", "Imp", "Exe", "Cost" });
                if (!EqualityComparer<EffectTypeFilter>.Default.Equals(newFilter, _filter))
                { _filter = newFilter; OnFilterChanged(); }
            });
        }

        private void DrawSearchCard()
        {
            EditorCard.DrawLight(Pad, () =>
            {
                var s = EditorUIUtility.DrawSearchRow(_searchText, labelWidth: 42f);
                if (s != _searchText) { _searchText = s; }
            });
        }

        // ── 右栏：编辑 ──
        private void DrawRightColumn()
        {
            EditorCard.Draw(Pad, () =>
            {
                if (_selectedEffect == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select an effect from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    return;
                }

                var title = $"Edit: {_selectedEffect.name}";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.Space(Pad);

                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                DrawBaseFields();
                EditorCard.Gap(Pad);
                DrawTypeSpecificFields();
                EditorGUILayout.EndScrollView();
            });
        }

        // ═══════════════════════════════════════════════════
        // 编辑表单
        // ═══════════════════════════════════════════════════

        private void DrawBaseFields()
        {
            EditorCard.Draw(Pad, "Base", () =>
            {
                var e = _selectedEffect;

                if (EditorForm.NeedsRebuild(_baseForm, e))
                {
                    _baseForm = new EditorForm(e) { DefaultLabelWidth = 80 };

                    // Name → RawField（Object.name 非 SO 字段）
                    _baseForm.RawField("Name", 80,
                        getValue: () => e.name,
                        setValue: v => { e.name = (string)v; },
                        drawFunc: v => EditorGUILayout.TextField((string)v),
                        equals: (a, b) => (string)a == (string)b)
                    .CustomOnChange((_, newVal) =>
                    {
                        var n = (string)newVal;
                        if (string.IsNullOrWhiteSpace(n)) return false;
                        RenameEffect(e, n);
                        return true;
                    });

                    // effectTag → ObjectField + TagPicker 按钮
                    _baseForm.ObjectField<GameplayTagDefinitionSO>("effectTag")
                        .PostInput(() =>
                        {
                            if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
                            {
                                var rect = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                                TagPicker.Show(rect, allowCreate: true,
                                    currentFullTag: e.effectTag?.FullTag,
                                    onSelected: t =>
                                    {
                                        if (e.effectTag != t)
                                        {
                                            e.effectTag = t;
                                            EditorUtility.SetDirty(e);
                                            _hasChanges = true;
                                            _needsRefresh = true;
                                            _baseForm = null; // force rebuild
                                        }
                                    });
                            }
                        });

                    // 标准字段
                    _baseForm.Float("duration")
                             .Toggle("stackable")
                             .Int("maxStacks",
                                 visibleWhen: () => e.stackable,
                                 onBeforeSet: v => Mathf.Max(1, v));

                    _baseForm.OnAnyChange += MarkDirty;
                }
                _baseForm?.Draw();

                // applicationBlockedTags
                GUILayout.Space(Pad);
                DrawBlockedTags(e);
            });
        }

        private void DrawBlockedTags(EffectSO e)
        {
            EditorGUILayout.LabelField(
                $"applicationBlockedTags [{(e.applicationBlockedTags != null ? e.applicationBlockedTags.Length : 0)}]",
                EditorStyles.miniBoldLabel);

            var tags = e.applicationBlockedTags ?? Array.Empty<GameplayTagDefinitionSO>();
            int removeAt = -1;

            for (var i = 0; i < tags.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var t = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                    tags[i], typeof(GameplayTagDefinitionSO), false);
                if (t != tags[i])
                {
                    var arr = new GameplayTagDefinitionSO[tags.Length];
                    Array.Copy(tags, arr, tags.Length);
                    arr[i] = t;
                    e.applicationBlockedTags = arr;
                    MarkDirty();
                }
                if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    var currentTag = tags[i]; // capture tag reference
                    var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                    TagPicker.Show(r, allowCreate: true, currentFullTag: currentTag?.FullTag,
                        onSelected: t =>
                        {
                            var arr = e.applicationBlockedTags;
                            if (arr == null) return;
                            for (int k = 0; k < arr.Length; k++)
                            {
                                if (arr[k] == currentTag)
                                {
                                    arr[k] = t;
                                    e.applicationBlockedTags = arr;
                                    MarkDirty();
                                    break;
                                }
                            }
                        });
                }
                if (EditorUIUtility.DeleteButton())
                    removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
            {
                e.applicationBlockedTags = AbilityEditorUtility.RemoveAt(tags, removeAt);
                MarkDirty();
            }

            GUILayout.Space(2);
            if (EditorButton.Draw("+ Add Blocked Tag", size: EditorButtonSize.Small))
            {
                e.applicationBlockedTags = AbilityEditorUtility.Append(tags, null);
                MarkDirty();
            }
        }

        private void DrawTypeSpecificFields()
        {
            var e = _selectedEffect;

            switch (e)
            {
                case DamageEffectSO d:
                    DrawCardSection("Damage", () => DrawDamageFields(d));
                    break;
                case ImpactEffectSO i:
                    DrawCardSection("Impact", () => DrawImpactFields(i));
                    break;
                case ExecuteEffectSO x:
                    DrawCardSection("Execute", () => DrawExecuteFields(x));
                    break;
                case CostEffectSO c:
                    DrawCardSection("Cost", () => DrawCostFields(c));
                    break;
            }
        }

        private static void DrawCardSection(string title, Action draw)
        {
            EditorCard.Draw(Pad, title, draw);
        }

        private void DrawDamageFields(DamageEffectSO d)
        {
            if (EditorForm.NeedsRebuild(_typeForm, d))
            {
                _typeForm = new EditorForm(d) { DefaultLabelWidth = 100 };
                _typeForm.Float("baseValue");
                _typeForm.OnAnyChange += MarkDirty;
            }
            _typeForm?.Draw();
        }

        private void DrawImpactFields(ImpactEffectSO i)
        {
            if (EditorForm.NeedsRebuild(_typeForm, i))
            {
                _typeForm = new EditorForm(i) { DefaultLabelWidth = 100 };
                _typeForm.Float("staggerValue")
                         .Float("knockbackForce")
                         .Enum<EKnockbackDirection>("knockbackDir");
                _typeForm.OnAnyChange += MarkDirty;
            }
            _typeForm?.Draw();
        }

        private void DrawExecuteFields(ExecuteEffectSO x)
        {
            if (EditorForm.NeedsRebuild(_typeForm, x))
            {
                _typeForm = new EditorForm(x) { DefaultLabelWidth = 100 };
                _typeForm.Slider("hpThreshold", 0f, 1f);
                _typeForm.OnAnyChange += MarkDirty;
            }
            _typeForm?.Draw();
        }

        private void DrawCostFields(CostEffectSO c)
        {
            if (EditorForm.NeedsRebuild(_typeForm, c))
            {
                _typeForm = new EditorForm(c) { DefaultLabelWidth = 100 };
                _typeForm.ObjectField<PropertyDefSO>("def")
                         .Float("amount");
                _typeForm.OnAnyChange += MarkDirty;
            }
            _typeForm?.Draw();
        }

        // ═══════════════════════════════════════════════════
        // StatusBar
        // ═══════════════════════════════════════════════════
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Pad);

            int dmg = 0, imp = 0, exe = 0, cost = 0;
            foreach (var e in _allEffects)
            {
                if (e is DamageEffectSO) dmg++;
                else if (e is ImpactEffectSO) imp++;
                else if (e is ExecuteEffectSO) exe++;
                else if (e is CostEffectSO) cost++;
            }
            EditorGUILayout.LabelField(
                $"{_allEffects.Count} effects · {dmg} Dmg · {imp} Imp · {exe} Exe · {cost} Cost",
                EditorStyles.miniLabel);

            if (_selectedEffect != null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"{_selectedEffect.name} ({_selectedEffect.GetType().Name.Replace("EffectSO", "")})",
                    EditorStyles.miniLabel);
            }

            GUILayout.Space(Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // Model：扫描 + 建树
        // ═══════════════════════════════════════════════════
        private void RefreshModel()
        {
            _allEffects.Clear();
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            // 扫描所有 EffectSO
            var guids = AssetDatabase.FindAssets("t:EffectSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var effect = AssetDatabase.LoadAssetAtPath<EffectSO>(path);
                if (effect != null) _allEffects.Add(effect);
            }
            _allEffects.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildTree();
        }

        private void BuildTree()
        {
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            var filtered = _filter == EffectTypeFilter.All
                ? _allEffects
                : _allEffects.Where(e => EffectMatchesFilter(e, _filter)).ToList();

            foreach (var effect in filtered)
            {
                var tag = effect.effectTag;
                if (tag == null)
                {
                    AddEffectToFolder("Uncategorized", 0, effect, null);
                    continue;
                }

                // walk tag parent chain
                var tagChain = new List<GameplayTagDefinitionSO>();
                var t = tag;
                while (t != null)
                {
                    tagChain.Add(t);
                    t = t.Parent;
                }
                tagChain.Reverse(); // root → leaf

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
                    DisplayName = effect.name,
                    FullPath = $"{parentNode.FullPath}/{effect.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    Effect = effect,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leaf);
            }

            AbilityEditorUtility.SortTreeRecursive(_treeRoots);
            AbilityEditorUtility.ComputeTreeCounts(_treeRoots);
        }

        private void AddEffectToFolder(string folderName, int depth, EffectSO effect, AbilityTreeNode parent)
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
                DisplayName = effect.name,
                FullPath = $"{folderName}/{effect.name}",
                Depth = depth + 1,
                IsFolder = false,
                Effect = effect,
                Parent = folderNode,
            };
            folderNode.Children.Add(leaf);
        }

        private static bool EffectMatchesFilter(EffectSO e, EffectTypeFilter f) => f switch
        {
            EffectTypeFilter.Damage => e is DamageEffectSO,
            EffectTypeFilter.Impact => e is ImpactEffectSO,
            EffectTypeFilter.Execute => e is ExecuteEffectSO,
            EffectTypeFilter.Cost => e is CostEffectSO,
            _ => true,
        };

        // ═══════════════════════════════════════════════════
        // Actions
        // ═══════════════════════════════════════════════════
        private void SelectEffect(EffectSO effect)
        {
            if (effect == null) return;
            _selectedEffect = effect;
            Repaint();
        }

        private void OnFilterChanged()
        {
            BuildTree();
            _foldouts.Clear();
            Repaint();
        }

        private void RenameEffect(EffectSO effect, string newName)
        {
            var path = AssetDatabase.GetAssetPath(effect);
            if (string.IsNullOrEmpty(path)) return;
            var result = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"[EffectEditor] Rename failed: {result}");
                return;
            }
            effect.name = newName;
            EditorUtility.SetDirty(effect);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void DeleteEffect(EffectSO effect)
        {
            if (!AbilityEditorUtility.DeleteAssetWithConfirm(effect, "Effect"))
                return;
            if (_selectedEffect == effect)
                _selectedEffect = null;
            _needsRefresh = true;
            _hasChanges = false;
            Repaint();
        }

        private void MarkDirty()
        {
            if (_selectedEffect == null) return;
            EditorUtility.SetDirty(_selectedEffect);
            _hasChanges = true;
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _foldouts.Clear();
            _selectedEffect = null;
            Repaint();
        }

        // ═══════════════════════════════════════════════════
        // Import / Export
        // ═══════════════════════════════════════════════════
        private void ExportFile()
        {
            var path = EditorUtility.SaveFilePanel("Export Effects", "Assets/Data/Ability/Effects", "effects_export", "json");
            if (string.IsNullOrEmpty(path)) return;
            var json = EffectImporter.ExportToJson();
            File.WriteAllText(path, json);
            Debug.Log($"[EffectEditor] Exported to {path}");
        }

        // ═══════════════════════════════════════════════════
        // Create
        // ═══════════════════════════════════════════════════
        private void CreateNewEffect()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Damage"), false, () => CreateEffect<DamageEffectSO>("DamageEffect_", "Damage"));
            menu.AddItem(new GUIContent("Impact"), false, () => CreateEffect<ImpactEffectSO>("ImpactEffect_", "Impact"));
            menu.AddItem(new GUIContent("Execute"), false, () => CreateEffect<ExecuteEffectSO>("ExecuteEffect_", "Execute"));
            menu.AddItem(new GUIContent("Cost"), false, () => CreateEffect<CostEffectSO>("CostEffect_", "Cost"));
            menu.ShowAsContext();
        }

        private void CreateEffect<T>(string prefix, string subDir) where T : EffectSO
        {
            var dir = $"Assets/Data/Ability/Effects/{subDir}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{prefix}New.asset");
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            _needsRefresh = true;
            _hasChanges = true;
            _selectedEffect = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[EffectEditor] Created {path}");
        }
    }

    /// <summary>
    /// Effect 类型过滤。All = 全部，其余按具体 EffectSO 子类过滤。
    /// </summary>
    public enum EffectTypeFilter
    {
        All,
        Damage,
        Impact,
        Execute,
        Cost,
    }
}
#endif
