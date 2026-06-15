#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private const float LeftWidth = 450f;

        // ── Model ──
        private List<EffectSO> _allEffects = new();
        private List<EditorTreeNode> _treeRoots = new();

        // ── TreeView ──
        private EditorTreeView _treeView;

        // ── Filter ──
        private EffectTypeFilter _filter = EffectTypeFilter.All;
        private string _searchText = "";

        // ── 状态 ──
        private bool _hasChanges;
        private EffectSO _selectedEffect;
        private Vector2 _rightScroll;
        private Rect _effectTagButtonRect;
        private Rect _blockedTagButtonRect;
        private Rect _grantedTagButtonRect;

        [MenuItem("RedDust/Effect Editor", priority = 1)]
        private static void Open()
            => GetWindow<EffectEditorWindow>("Effect Editor");

        private void OnEnable()
        {
            RefreshModel();
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
                EditorGUILayout.LabelField("Effect Editor", EditorStyles.largeLabel,
                    GUILayout.ExpandWidth(true));
                var sub = EditorTokens.BreadcrumbStyle;
                EditorGUILayout.LabelField("L3_Ability · Editor", sub, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorTokens.Pad);

                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Draw("Refresh", size: EditorButtonSize.Medium))
                {
                    RefreshModel();
                    RebuildTree();
                }
                GUILayout.FlexibleSpace();

                if (EditorButton.Draw("Import/Export", size: EditorButtonSize.Medium))
                    EffectImportWindow.Open();

                if (EditorButton.Draw("+ Create", EditorButtonType.Success, EditorButtonSize.Medium))
                    CreateNewEffect();

                if (EditorButton.Draw(_hasChanges ? "Save *" : "Saved", _hasChanges ? EditorButtonType.Primary : EditorButtonType.Default, EditorButtonSize.Medium, enabled: _hasChanges))
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

            // 左栏：树形列表
            EditorGUILayout.BeginHorizontal(GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            DrawLeftColumn();
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(EditorTokens.Pad);

            // 右栏：编辑
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏：树形列表 ──
        private void DrawLeftColumn()
        {
            EditorCard.Draw(() =>
            {
                DrawFilterCard();
                EditorCard.GapTight();
                DrawSearchCard();
                EditorCard.Gap(EditorTokens.Pad);

                if (_treeView == null)
                {
                    _treeView = new EditorTreeView();
                    _treeView.SetData(
                        _treeRoots.Count > 0 ? _treeRoots : EditorTree.CreateDemoData(),
                        onSelect: node => SelectEffect(node.UserData as EffectSO),
                        onDelete: node => DeleteEffect(node.UserData as EffectSO));
                }

                var rect = EditorGUILayout.GetControlRect(
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _treeView.OnGUI(rect);
            });
        }

        private void DrawFilterCard()
        {
            EditorCard.Draw(() =>
            {
                var newFilter = EditorButtonGroup.Draw(_filter,
                    new[] { EffectTypeFilter.All, EffectTypeFilter.Damage, EffectTypeFilter.Impact, EffectTypeFilter.Execute, EffectTypeFilter.Cost, EffectTypeFilter.Buff },
                    new[] { "All", "Dmg", "Imp", "Exe", "Cost", "Buf" });
                if (!EqualityComparer<EffectTypeFilter>.Default.Equals(newFilter, _filter))
                {
                    _filter = newFilter;
                    RebuildTree();
                }
            });
        }

        private void DrawSearchCard()
        {
            EditorCard.Draw(() =>
            {
                var s = EditorSearchBar.Draw(_searchText, labelWidth: 42f);
                if (s != _searchText)
                {
                    _searchText = s;
                    RebuildTree();
                }
            });
        }

        private void RebuildTree()
        {
            BuildTree();
            _treeView?.SetData(
                _treeRoots,
                onSelect: node => SelectEffect(node.UserData as EffectSO),
                onDelete: node => DeleteEffect(node.UserData as EffectSO));
        }

        // ── 右栏：编辑 ──
        private void DrawRightColumn()
        {
            if (_selectedEffect == null)
            {
                EditorCard.Draw(() =>
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select an effect from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                });
                return;
            }

            EditorCard.Draw($"Edit: {_selectedEffect.name}", () =>
            {
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                DrawBaseFields();
                EditorCard.Gap(EditorTokens.Pad);
                DrawTypeSpecificFields();
                EditorGUILayout.EndScrollView();
            });
        }

        // ═══════════════════════════════════════════════════
        // 编辑表单
        // ═══════════════════════════════════════════════════

        private void DrawBaseFields()
        {
            EditorCard.Draw("Base", () =>
            {
                var e = _selectedEffect;

                EditorForm.Draw(e, form =>
                {
    
                    // Name → RawField
                    EditorFormItem.RawField("Name", null,
                        getValue: () => e.name,
                        setValue: v => { e.name = (string)v; },
                        drawFunc: v => EditorGUILayout.TextField((string)v),
                        equals: (a, b) => (string)a == (string)b);

                    // effectTag → ObjectField + TagPicker
                    EditorFormItem.ObjectFieldWithTag<GameplayTagDefinitionSO>("effectTag",
                        ref _effectTagButtonRect);

                    // 标准字段
                    EditorFormItem.Float("duration");
                    EditorFormItem.Toggle("stackable");
                    EditorFormItem.Int("maxStacks",
                        visibleWhen: () => e.stackable,
                        onBeforeSet: v => Mathf.Max(1, v));

                    form.OnChange += MarkDirty;

                    // Blocked Tags — tags that prevent this effect from applying
                    EditorFormItem.ArrayField<GameplayTagDefinitionSO>(
                        "Blocked Tags",
                        getValue: () => e.applicationBlockedTags,
                        setValue: v => e.applicationBlockedTags = v,
                        drawRow: (i, tag) =>
                        {
                            var arr = e.applicationBlockedTags;
                            var newTag = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                                tag, typeof(GameplayTagDefinitionSO), false);
                            if (newTag != tag)
                            {
                                arr[i] = newTag;
                                MarkDirty();
                            }
                            if (EditorButton.Default("Tag", EditorButtonSize.Small, width: 35))
                            {
                                var cap = tag;
                                TagPicker.Show(_blockedTagButtonRect, allowCreate: true,
                                    currentFullTag: cap?.FullTag,
                                    onSelected: t =>
                                    {
                                        var a = e.applicationBlockedTags;
                                        if (a == null) return;
                                        for (int k = 0; k < a.Length; k++)
                                            if (a[k] == cap) { a[k] = t; MarkDirty(); break; }
                                    });
                            }
                            if (Event.current.type == EventType.Repaint)
                                _blockedTagButtonRect = GUILayoutUtility.GetLastRect();
                        },
                        createDefault: () => null,
                        onChanged: (_, _) => MarkDirty(),
                        tooltip: "目标身上存在这些标签时，阻止此效果施加");
                });
            });
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
                case BuffEffectSO b:
                    DrawCardSection("Buff", () => DrawBuffFields(b));
                    break;
            }
        }

        private static void DrawCardSection(string title, Action draw)
        {
            EditorCard.Draw(title, draw);
        }

        private void DrawDamageFields(DamageEffectSO d)
        {
            EditorForm.Draw(d, form =>
            {
                EditorFormItem.Float("baseValue");
                EditorFormItem.Float("modAdd");
                EditorFormItem.Float("modMult");
                EditorFormItem.Int("priority");
                form.OnChange += MarkDirty;
            });
        }

        private void DrawImpactFields(ImpactEffectSO i)
        {
            EditorForm.Draw(i, form =>
            {
                EditorFormItem.Float("staggerValue");
                EditorFormItem.Float("knockbackForce");
                EditorFormItem.Enum<EKnockbackDirection>("knockbackDir");
                form.OnChange += MarkDirty;
            });
        }

        private void DrawExecuteFields(ExecuteEffectSO x)
        {
            EditorForm.Draw(x, form =>
            {
                EditorFormItem.Slider("hpThreshold", 0f, 1f);
                form.OnChange += MarkDirty;
            });
        }

        private void DrawCostFields(CostEffectSO c)
        {
            EditorForm.Draw(c, form =>
            {
                EditorFormItem.ObjectField<PropertyDefSO>("def");
                EditorFormItem.Float("amount");
                form.OnChange += MarkDirty;
            });
        }

        private void DrawBuffFields(BuffEffectSO b)
        {
            EditorForm.Draw(b, form =>
            {

                EditorFormItem.ArrayField<GameplayTagDefinitionSO>(
                    "Granted Tags",
                    getValue: () => b.grantedTags,
                    setValue: v => b.grantedTags = v,
                    drawRow: (i, t) =>
                    {
                        var tag = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                            t, typeof(GameplayTagDefinitionSO), false);
                        if (tag != t) { b.grantedTags[i] = tag; MarkDirty(); }
                        if (EditorButton.Default("Tag", EditorButtonSize.Small, width: 35))
                        {
                            var cap = t;
                            TagPicker.Show(_grantedTagButtonRect, allowCreate: true, currentFullTag: cap?.FullTag,
                                onSelected: tg =>
                                {
                                    b.grantedTags[i] = tg; MarkDirty();
                                });
                        }
                        if (Event.current.type == EventType.Repaint)
                            _grantedTagButtonRect = GUILayoutUtility.GetLastRect();
                    },
                    createDefault: () => null,
                    tooltip: "Buff 激活期间授予目标的标签");

                EditorFormItem.ArrayField<SBuffAdjunct>(
                    "Float Adjuncts",
                    getValue: () => b.adjuncts,
                    setValue: v => b.adjuncts = v,
                    drawRow: (i, a) =>
                    {
                        var adj = a;
                        Action flush = () => { b.adjuncts[i] = adj; MarkDirty(); };
                        EditorForm.Draw(null, row =>
                        {
                            row.BeginGroup(FormGroupLayout.Horizontal);
                            EditorFormItem.RawField("Property", 55,
                                getValue: () => adj.property,
                                setValue: v => { adj.property = (PropertyDefSO)v; flush(); },
                                drawFunc: v => EditorGUILayout.ObjectField((PropertyDefSO)v, typeof(PropertyDefSO), false, GUILayout.Width(100)),
                                equals: (x, y) => object.ReferenceEquals(x, y));
                            EditorFormItem.RawField("Add", 25,
                                getValue: () => adj.valueAdd,
                                setValue: v => { adj.valueAdd = (float)v; flush(); },
                                drawFunc: v => EditorGUILayout.FloatField((float)v, GUILayout.Width(50)),
                                equals: (x, y) => Mathf.Abs((float)x - (float)y) <= 0.001f);
                            EditorFormItem.RawField("Mult", 28,
                                getValue: () => adj.valueMultiply,
                                setValue: v => { adj.valueMultiply = (float)v; flush(); },
                                drawFunc: v => EditorGUILayout.FloatField((float)v, GUILayout.Width(50)),
                                equals: (x, y) => Mathf.Abs((float)x - (float)y) <= 0.001f);
                            row.EndGroup();
                        });
                    },
                    createDefault: () => new SBuffAdjunct { valueMultiply = 1f },
                    tooltip: "Buff 激活期间对属性值进行加减/乘算的浮点修正");

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

            int dmg = 0, imp = 0, exe = 0, cost = 0, buf = 0;
            foreach (var e in _allEffects)
            {
                if (e is DamageEffectSO) dmg++;
                else if (e is ImpactEffectSO) imp++;
                else if (e is ExecuteEffectSO) exe++;
                else if (e is CostEffectSO) cost++;
                else if (e is BuffEffectSO) buf++;
            }
            EditorGUILayout.LabelField(
                $"{_allEffects.Count} effects · {dmg} Dmg · {imp} Imp · {exe} Exe · {cost} Cost · {buf} Buf",
                EditorStyles.miniLabel);

            if (_selectedEffect != null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"{_selectedEffect.name} ({_selectedEffect.GetType().Name.Replace("EffectSO", "")})",
                    EditorStyles.miniLabel);
            }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        // Model：扫描 + 建树
        // ═══════════════════════════════════════════════════
        private void RefreshModel()
        {
            _allEffects.Clear();

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

        private readonly Dictionary<string, EditorTreeNode> _treeNodeIndex = new();

        private void BuildTree()
        {
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            var filtered = _filter == EffectTypeFilter.All
                ? _allEffects
                : _allEffects.Where(e => EffectMatchesFilter(e, _filter));

            if (!string.IsNullOrEmpty(_searchText))
            {
                var q = _searchText.ToLowerInvariant();
                filtered = filtered.Where(e => e.name.ToLowerInvariant().Contains(q));
            }

            foreach (var effect in filtered)
            {
                var tag = effect.effectTag;
                if (tag == null)
                {
                    AddEffectToFolder("Uncategorized", 0, effect, null);
                    continue;
                }

                var tagChain = new List<GameplayTagDefinitionSO>();
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

                    if (!_treeNodeIndex.TryGetValue(accumPath, out var folderNode))
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
                        _treeNodeIndex[accumPath] = folderNode;

                        if (parentNode != null)
                            parentNode.Children.Add(folderNode);
                        else
                            _treeRoots.Add(folderNode);
                    }
                    parentNode = folderNode;
                }

                var leaf = new EditorTreeNode
                {
                    DisplayName = effect.name,
                    FullPath = $"{parentNode.FullPath}/{effect.name}",
                    Depth = parentNode.Depth + 1,
                    IsFolder = false,
                    UserData = effect,
                    Parent = parentNode,
                };
                parentNode.Children.Add(leaf);
            }

            EditorTree.SortTreeRecursive(_treeRoots);
            EditorTree.ComputeTreeCounts(_treeRoots);
        }

        private void AddEffectToFolder(string folderName, int depth, EffectSO effect, EditorTreeNode parent)
        {
            if (!_treeNodeIndex.TryGetValue(folderName, out var folderNode))
            {
                folderNode = new EditorTreeNode
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

            var leaf = new EditorTreeNode
            {
                DisplayName = effect.name,
                FullPath = $"{folderName}/{effect.name}",
                Depth = depth + 1,
                IsFolder = false,
                UserData = effect,
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
            EffectTypeFilter.Buff => e is BuffEffectSO,
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
            RefreshModel();
            RebuildTree();
        }

        private void DeleteEffect(EffectSO effect)
        {
            if (!AbilityEditorUtility.DeleteAssetWithConfirm(effect, "Effect"))
                return;
            if (_selectedEffect == effect)
                _selectedEffect = null;
            RefreshModel();
            RebuildTree();
            Repaint();
        }

        private void MarkDirty()
        {
            if (_selectedEffect == null) return;
            EditorUtility.SetDirty(_selectedEffect);
            _hasChanges = true;
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
            menu.AddItem(new GUIContent("Buff"), false, () => CreateEffect<BuffEffectSO>("BuffEffect_", "Buff"));
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

            RefreshModel();
            RebuildTree();
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
        Buff,
    }
}
#endif
