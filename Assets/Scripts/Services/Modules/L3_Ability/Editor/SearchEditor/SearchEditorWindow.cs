#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 独立 Search 编辑器。浏览、创建、编辑所有 AbilitySearchSO 资产。
    /// 对标 EffectEditorWindow 模式。
    /// </summary>
    public class SearchEditorWindow : EditorWindow
    {
        private const float Pad = 6f;
        private const float LeftWidth = 300f;

        // ── 内联 Model（扫描 SearchSO，按 searchType 构建虚拟文件夹树）──
        private List<AbilitySearchSO> _allSearches = new();
        private List<AbilityTreeNode> _treeRoots = new();
        private readonly Dictionary<string, AbilityTreeNode> _treeNodeIndex = new();

        // ── 状态 ──
        private bool _needsRefresh = true;
        private bool _hasChanges;
        private AbilitySearchSO _selectedSearch;
        private string _searchText = "";
        private SearchTypeFilter _filter = SearchTypeFilter.All;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        // ── EditorForm ──
        private EditorForm _baseForm;
        private EditorForm _typeForm;

        [MenuItem("RedDust/Search Editor", priority = 2)]
        private static void Open()
            => GetWindow<SearchEditorWindow>("Search Editor");

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
                EditorGUILayout.LabelField("Search Editor", EditorStyles.largeLabel,
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
                    CreateNewSearch();
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

                if (_selectedSearch != null)
                {
                    if (GUILayout.Button("Ping", GUILayout.Height(22)))
                        EditorGUIUtility.PingObject(_selectedSearch);
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

        // ── 左栏 ──
        private void DrawLeftColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                // 筛选标签
                EditorUIUtility.DrawCard(Pad, () =>
                {
                    var newFilter = EditorUIUtility.DrawFilterTabBar(_filter,
                        new SearchTypeFilter[] { SearchTypeFilter.All, SearchTypeFilter.Cone, SearchTypeFilter.Ray, SearchTypeFilter.Circle },
                        new[] { "All", "Cone", "Ray", "Circle" });
                    if (!EqualityComparer<SearchTypeFilter>.Default.Equals(newFilter, _filter))
                    { _filter = newFilter; OnFilterChanged(); }
                });

                EditorUIUtility.CardGap(Pad);

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
                    onLeafSelected: asset => SelectSearch(asset as AbilitySearchSO),
                    selectedSearch: _selectedSearch);
                EditorGUILayout.EndScrollView();
            });
        }

        // ── 右栏 ──
        private void DrawRightColumn()
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                if (_selectedSearch == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Select a search from the left panel.",
                        EditorUIUtility.GreyPlaceholder);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    return;
                }

                var title = $"Edit: {_selectedSearch.name}";
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

                var s = _selectedSearch;

                if (_baseForm?.Target != s)
                {
                    _baseForm = new EditorForm(s) { DefaultLabelWidth = 80 };

                    // Name → RawField（Object.name 非 SO 字段，回调走 RenameAsset）
                    _baseForm.RawField("Name", 80,
                        getValue: () => s.name,
                        setValue: v => { s.name = (string)v; },
                        drawFunc: v => EditorGUILayout.TextField((string)v),
                        equals: (a, b) => (string)a == (string)b)
                    .CustomOnChange((_, newVal) =>
                    {
                        var n = (string)newVal;
                        if (string.IsNullOrWhiteSpace(n)) return false;
                        RenameSearch(s, n);
                        return true;
                    });

                    // searchType → 只读
                    _baseForm.Enum<ESearchType>("searchType").ReadOnly();

                    // 可编辑字段
                    _baseForm.Float("range")
                             .RawField("targetMask", 80,
                                 getValue: () => s.targetMask.value,
                                 setValue: v => s.targetMask = (int)v,
                                 drawFunc: v => DrawLayerMaskField((int)v).value,
                                 equals: (a, b) => (int)a == (int)b,
                                 tooltip: (typeof(AbilitySearchSO).GetField("targetMask")
                                     ?.GetCustomAttribute<TooltipAttribute>())?.tooltip)
                             .Int("maxTargets")
                             .Enum<ETargetFilter>("targetFilter");

                    _baseForm.OnAnyChange += MarkDirty;
                }
                _baseForm?.Draw();
            });
        }

        /// <summary>LayerMask 绘制。Unity 不提供内置 EditorGUILayout.LayerMaskField。</summary>
        private static LayerMask DrawLayerMaskField(LayerMask mask)
        {
            // 用 MaskField 模拟：将所有 layer 名收集为选项
            var layers = new List<string>();
            for (var i = 0; i < 32; i++)
            {
                var name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    layers.Add($"{i}: {name}");
            }
            var newMask = EditorGUILayout.MaskField(mask.value, layers.ToArray());
            return newMask;
        }

        private void DrawTypeSpecificFields()
        {
            switch (_selectedSearch)
            {
                case ConeSearchSO cone:
                    DrawCardSection("Cone", () => DrawConeFields(cone));
                    break;
                case RaySearchSO ray:
                    DrawCardSection("Ray", () => DrawRayFields(ray));
                    break;
                case CircleSearchSO:
                    DrawCardSection("Circle", () =>
                    {
                        EditorGUILayout.LabelField("(no additional fields)",
                            EditorUIUtility.GreyPlaceholder);
                    });
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

        private void DrawConeFields(ConeSearchSO cone)
        {
            if (_typeForm?.Target != cone)
            {
                _typeForm = new EditorForm(cone) { DefaultLabelWidth = 100 };
                _typeForm.Slider("angle", 0f, 360f);
                _typeForm.OnAnyChange += MarkDirty;
            }
            _typeForm?.Draw();
        }

        private void DrawRayFields(RaySearchSO ray)
        {
            if (_typeForm?.Target != ray)
            {
                _typeForm = new EditorForm(ray) { DefaultLabelWidth = 100 };
                _typeForm.Toggle("requiresLineOfSight");
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

            int cone = 0, ray = 0, circle = 0;
            foreach (var s in _allSearches)
            {
                if (s.searchType == ESearchType.Cone) cone++;
                else if (s.searchType == ESearchType.RayLine) ray++;
                else if (s.searchType == ESearchType.Circle) circle++;
            }
            EditorGUILayout.LabelField(
                $"{_allSearches.Count} searches · {cone} Cone · {ray} Ray · {circle} Circle",
                EditorStyles.miniLabel);

            if (_selectedSearch != null)
            {
                GUILayout.FlexibleSpace();
                var typeName = AbilityEditorUtility.GetSearchTypeDisplayName(_selectedSearch.searchType);
                EditorGUILayout.LabelField(
                    $"{_selectedSearch.name} ({typeName})",
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
            _allSearches.Clear();
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            var guids = AssetDatabase.FindAssets("t:AbilitySearchSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var search = AssetDatabase.LoadAssetAtPath<AbilitySearchSO>(path);
                if (search != null) _allSearches.Add(search);
            }
            _allSearches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            BuildTree();
        }

        private void BuildTree()
        {
            _treeRoots.Clear();
            _treeNodeIndex.Clear();

            var filtered = _filter == SearchTypeFilter.All
                ? _allSearches
                : _allSearches.Where(s => AbilityEditorUtility.SearchMatchesFilter(s, _filter)).ToList();

            foreach (var search in filtered)
            {
                var folderName = AbilityEditorUtility.GetSearchTypeDisplayName(search.searchType);
                var folderPath = $"search_{folderName}";

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
                    DisplayName = search.name,
                    FullPath = $"{folderPath}/{search.name}",
                    Depth = 1,
                    IsFolder = false,
                    Search = search,
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
        private void SelectSearch(AbilitySearchSO search)
        {
            if (search == null) return;
            _selectedSearch = search;
            Repaint();
        }

        private void OnFilterChanged()
        {
            BuildTree();
            _foldouts.Clear();
            Repaint();
        }

        private void RenameSearch(AbilitySearchSO search, string newName)
        {
            var path = AssetDatabase.GetAssetPath(search);
            if (string.IsNullOrEmpty(path)) return;
            var result = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(result))
            {
                Debug.LogError($"[SearchEditor] Rename failed: {result}");
                return;
            }
            search.name = newName;
            EditorUtility.SetDirty(search);
            _hasChanges = true;
            _needsRefresh = true;
        }

        private void MarkDirty()
        {
            if (_selectedSearch == null) return;
            EditorUtility.SetDirty(_selectedSearch);
            _hasChanges = true;
        }

        private void RefreshAll()
        {
            _needsRefresh = true;
            _foldouts.Clear();
            _selectedSearch = null;
            Repaint();
        }

        // ═══════════════════════════════════════════════════
        // Create
        // ═══════════════════════════════════════════════════
        private void CreateNewSearch()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Cone"), false, () => CreateSearch<ConeSearchSO>("Cone", "Search_Cone_"));
            menu.AddItem(new GUIContent("Ray"), false, () => CreateSearch<RaySearchSO>("Ray", "Search_Ray_"));
            menu.AddItem(new GUIContent("Circle"), false, () => CreateSearch<CircleSearchSO>("Circle", "Search_Circle_"));
            menu.ShowAsContext();
        }

        private void CreateSearch<T>(string subDir, string prefix) where T : AbilitySearchSO
        {
            var dir = $"Assets/Data/Ability/Searches/{subDir}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{prefix}New.asset");
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();

            _needsRefresh = true;
            _hasChanges = true;
            _selectedSearch = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[SearchEditor] Created {path}");
        }
    }

    /// <summary>
    /// Search 类型过滤。All = 全部，其余按 ESearchType 过滤。
    /// </summary>
    public enum SearchTypeFilter
    {
        All,
        Cone,
        Ray,
        Circle,
    }
}
#endif
