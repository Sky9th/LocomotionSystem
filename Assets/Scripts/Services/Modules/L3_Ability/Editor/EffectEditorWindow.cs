#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedDust.Core;
using RedDust.Core.Editor;
using RedDust.Shared.EditorUI;
using RedDust.Stats;
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
                EditorGUILayout.LabelField("Effect Editor", EditorStyles.largeLabel,
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

                if (GUILayout.Button("Export", GUILayout.Width(60), GUILayout.Height(22)))
                    ExportFile();

                GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
                if (GUILayout.Button("+ Create", GUILayout.Width(70), GUILayout.Height(22)))
                    CreateNewEffect();
                GUI.backgroundColor = Color.white;

                GUI.enabled = _hasChanges;
                GUI.backgroundColor = _hasChanges ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
                if (GUILayout.Button(_hasChanges ? "Save *" : "Saved", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    AssetDatabase.SaveAssets();
                    _hasChanges = false;
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                if (_selectedEffect != null)
                {
                    if (GUILayout.Button("Ping", GUILayout.Height(22)))
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

            EditorUIUtility.CardGap(Pad);

            // 右栏：编辑
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightColumn();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左栏：列表 ──
        private void DrawLeftColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                DrawFilterCard();
                EditorUIUtility.CardGap(Pad);
                DrawSearchCard();
                EditorUIUtility.CardGap(Pad);

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                var nullSO = (AbilitySO)null;
                AbilityTreeView.DrawTree(_treeRoots, _foldouts, ref nullSO,
                    _searchText, AbilityTypeFilter.All,
                    onLeafSelected: asset => SelectEffect(asset as EffectSO),
                    selectedEffect: _selectedEffect);
                EditorGUILayout.EndScrollView();
            });
        }

        private void DrawFilterCard()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                var tabs = new[] { EffectTypeFilter.All, EffectTypeFilter.Damage, EffectTypeFilter.Impact, EffectTypeFilter.Execute, EffectTypeFilter.Cost };
                var labels = new[] { "All", "Dmg", "Imp", "Exe", "Cost" };
                for (var i = 0; i < tabs.Length; i++)
                {
                    var sel = _filter == tabs[i];
                    GUI.backgroundColor = sel ? new Color(0.3f, 0.6f, 0.9f) : Color.white;
                    if (GUILayout.Button(labels[i], EditorStyles.miniButtonMid, GUILayout.Height(20)))
                    { _filter = tabs[i]; OnFilterChanged(); }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawSearchCard()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(42));
                var s = EditorGUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(s) && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                { s = ""; GUI.FocusControl(null); }
                EditorGUILayout.EndHorizontal();
                if (s != _searchText) { _searchText = s; }
            });
        }

        // ── 右栏：编辑 ──
        private void DrawRightColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                if (_selectedEffect == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    var grey = new GUIStyle(EditorStyles.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = 13, normal = { textColor = Color.grey } };
                    EditorGUILayout.LabelField("Select an effect from the left panel.", grey);
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
                EditorUIUtility.CardGap(Pad);
                DrawTypeSpecificFields();
                EditorGUILayout.EndScrollView();
            });
        }

        // ═══════════════════════════════════════════════════
        // 编辑表单
        // ═══════════════════════════════════════════════════

        private void DrawBaseFields()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                GUILayout.Space(Pad);

                var e = _selectedEffect;

                // name
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", EditorStyles.label, GUILayout.Width(80));
                var newName = EditorGUILayout.TextField(e.name);
                if (newName != e.name && !string.IsNullOrWhiteSpace(newName))
                    RenameEffect(e, newName);
                EditorGUILayout.EndHorizontal();

                // effectTag
                EditorGUILayout.BeginHorizontal();
                EditorUIUtility.LabelWithTooltip(e, "effectTag", 80, "effectTag");
                var tag = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                    e.effectTag, typeof(GameplayTagDefinitionSO), false);
                if (tag != e.effectTag) { e.effectTag = tag; SetDirty(); }
                if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                    TagPicker.Show(r, allowCreate: true, currentFullTag: e.effectTag?.FullTag,
                        onSelected: t => { if (e.effectTag != t) { e.effectTag = t; SetDirty(); } });
                }
                EditorGUILayout.EndHorizontal();

                // duration
                EditorGUILayout.BeginHorizontal();
                EditorUIUtility.LabelWithTooltip(e, "duration", 80);
                var dur = EditorGUILayout.FloatField(e.duration);
                if (Mathf.Abs(dur - e.duration) > 0.001f) { e.duration = dur; SetDirty(); }
                EditorGUILayout.EndHorizontal();

                // stackable
                EditorGUILayout.BeginHorizontal();
                EditorUIUtility.LabelWithTooltip(e, "stackable", 80);
                var st = EditorGUILayout.Toggle(e.stackable);
                if (st != e.stackable) { e.stackable = st; SetDirty(); }
                EditorGUILayout.EndHorizontal();

                // maxStacks (only when stackable)
                if (e.stackable)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorUIUtility.LabelWithTooltip(e, "maxStacks", 80);
                    var ms = EditorGUILayout.IntField(e.maxStacks);
                    if (ms != e.maxStacks) { e.maxStacks = Mathf.Max(1, ms); SetDirty(); }
                    EditorGUILayout.EndHorizontal();
                }

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
                    SetDirty();
                }
                if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    var idx = i; // capture
                    var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                    TagPicker.Show(r, allowCreate: true, currentFullTag: tags[i]?.FullTag,
                        onSelected: t =>
                        {
                            if (t != e.applicationBlockedTags[idx])
                            {
                                var a = e.applicationBlockedTags;
                                a[idx] = t;
                                e.applicationBlockedTags = a;
                                SetDirty();
                            }
                        });
                }
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                    removeAt = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0)
            {
                var newArr = new GameplayTagDefinitionSO[tags.Length - 1];
                for (int i = 0, j = 0; i < tags.Length; i++)
                    if (i != removeAt) newArr[j++] = tags[i];
                e.applicationBlockedTags = newArr;
                SetDirty();
            }

            GUILayout.Space(2);
            if (GUILayout.Button("+ Add Blocked Tag", GUILayout.Height(20)))
            {
                var newArr = new GameplayTagDefinitionSO[tags.Length + 1];
                Array.Copy(tags, newArr, tags.Length);
                e.applicationBlockedTags = newArr;
                SetDirty();
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
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.Space(Pad);
                draw();
            });
        }

        private void DrawDamageFields(DamageEffectSO d)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(d, "baseDamage", 100);
            var v = EditorGUILayout.FloatField(d.baseDamage);
            if (Mathf.Abs(v - d.baseDamage) > 0.001f) { d.baseDamage = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(d, "armorPenetration", 100);
            v = EditorGUILayout.FloatField(d.armorPenetration);
            if (Mathf.Abs(v - d.armorPenetration) > 0.001f) { d.armorPenetration = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(d, "shieldPenetration", 100);
            v = EditorGUILayout.Slider(d.shieldPenetration, 0f, 1f);
            if (Mathf.Abs(v - d.shieldPenetration) > 0.001f) { d.shieldPenetration = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(d, "minDamage", 100);
            v = EditorGUILayout.FloatField(d.minDamage);
            if (Mathf.Abs(v - d.minDamage) > 0.001f) { d.minDamage = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(d, "maxDamage", 100);
            v = EditorGUILayout.FloatField(d.maxDamage);
            if (Mathf.Abs(v - d.maxDamage) > 0.001f) { d.maxDamage = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawImpactFields(ImpactEffectSO i)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(i, "staggerValue", 100);
            var v = EditorGUILayout.FloatField(i.staggerValue);
            if (Mathf.Abs(v - i.staggerValue) > 0.001f) { i.staggerValue = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(i, "knockbackForce", 100);
            v = EditorGUILayout.FloatField(i.knockbackForce);
            if (Mathf.Abs(v - i.knockbackForce) > 0.001f) { i.knockbackForce = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(i, "knockbackDir", 100, "knockbackDir");
            var kd = (EKnockbackDirection)EditorGUILayout.EnumPopup(i.knockbackDir);
            if (kd != i.knockbackDir) { i.knockbackDir = kd; SetDirty(); }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawExecuteFields(ExecuteEffectSO x)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(x, "hpThreshold", 100);
            var v = EditorGUILayout.Slider(x.hpThreshold, 0f, 1f);
            if (Mathf.Abs(v - x.hpThreshold) > 0.001f) { x.hpThreshold = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCostFields(CostEffectSO c)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(c, "statDef", 100);
            var sd = (StatDefinitionSO)EditorGUILayout.ObjectField(
                c.statDef, typeof(StatDefinitionSO), false);
            if (sd != c.statDef) { c.statDef = sd; SetDirty(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(c, "amount", 100);
            var v = EditorGUILayout.FloatField(c.amount);
            if (Mathf.Abs(v - c.amount) > 0.001f) { c.amount = v; SetDirty(); }
            EditorGUILayout.EndHorizontal();
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

            // sort: folders first alpha, then leaves alpha
            void SortRecursive(List<AbilityTreeNode> nodes)
            {
                nodes.Sort((a, b) =>
                {
                    if (a.IsFolder != b.IsFolder) return a.IsFolder ? -1 : 1;
                    return string.CompareOrdinal(a.DisplayName, b.DisplayName);
                });
                foreach (var n in nodes) SortRecursive(n.Children);
            }
            SortRecursive(_treeRoots);

            // count
            int CountRecursive(AbilityTreeNode node)
            {
                if (!node.IsFolder) return 1;
                var total = 0;
                foreach (var c in node.Children) total += CountRecursive(c);
                node.AbilityCount = total;
                return total;
            }
            foreach (var root in _treeRoots) CountRecursive(root);
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

        private void SetDirty()
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
