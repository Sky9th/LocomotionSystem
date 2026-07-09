#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RedDust.Container;
using RedDust.Core;
using RedDust.Core.Editor;
using RedDust.Properties;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Entities.Editor
{
    /// <summary>
    /// EntityEditor 抽象基类。提供通用的 PropertyPresetSO 编辑 UI（三栏布局、属性表单、保存/新建/删除）。
    /// 每个 Entity 大类（Character/Weapon/Prop/Building/SceneItem）继承此类，覆写类别特定行为。
    /// </summary>
    public abstract class EntityEditorWindow : EditorWindow
    {
        protected const float LeftWidth = 280f;
        protected const float RightWidth = 200f;
        protected const float LabelWidth = 110f;

        // ── State ──
        protected PropertyPresetSO _selectedPreset;
        protected EditorTreeView _treeView;
        protected List<EditorTreeNode> _treeRoots = new();
        protected string _lastSearchFilter = "";
        protected Dictionary<string, PropertyDefSO> _structure;
        protected Dictionary<string, string> _overrideValues = new();
        protected Dictionary<string, float> _minOverrides = new();
        protected Dictionary<string, float> _maxOverrides = new();
        protected bool _hasChanges;
        protected Dictionary<string, List<(string path, PropertyDefSO def)>> _propertyGroups;
        protected List<string> _propertyGroupOrder;
        protected string _searchFilter = "";
        protected Vector2 _centerScroll;

        // ═══════════════════════════════════════════════════
        //  Abstract — Subclasses MUST override
        // ═══════════════════════════════════════════════════

        /// <summary>目标 Preset 类型，用于 AssetDatabase 过滤。</summary>
        protected abstract Type GetTargetType();

        /// <summary>窗口标题。</summary>
        protected abstract string GetEditorTitle();

        /// <summary>面包屑路径。</summary>
        protected abstract string GetBreadcrumb();

        /// <summary>新建资产的菜单项 (label, SO type)。</summary>
        protected abstract (string label, Type soType)[] GetCreateMenuItems();

        /// <summary>新建资产的默认目录。</summary>
        protected abstract string GetDefaultAssetDir();

        /// <summary>按 SO 类型返回资产目录。默认回退到 GetDefaultAssetDir()。</summary>
        protected virtual string GetAssetDirForType(Type soType) => GetDefaultAssetDir();

        /// <summary>AssetDatabase.FindAssets 过滤字符串（如 "t:WeaponDefSO"）。</summary>
        protected abstract string GetAssetFilter();

        // ═══════════════════════════════════════════════════
        //  Virtual — Subclasses MAY override
        // ═══════════════════════════════════════════════════

        /// <summary>工具栏上额外的按钮。</summary>
        protected virtual void DrawExtraToolbarButtons() { }

        /// <summary>插入 Center 面板中 Basic Card 和 Properties 之间的类别特定内容。</summary>
        protected virtual void DrawCategorySpecificSection() { }

        /// <summary>状态栏摘要，默认用 Preset 类型 + Template + 属性数。</summary>
        protected virtual string GetStatusSummary()
        {
            if (_selectedPreset == null) return "No entity selected.";
            var typeName = _selectedPreset.GetType().Name;
            var templateName = _selectedPreset.Template != null ? _selectedPreset.Template.name : "none";
            var propCount = _structure?.Count ?? 0;
            var overrideCount = _overrideValues?.Count ?? 0;
            return $"Type: {typeName} · Template: {templateName} · {propCount} props ({overrideCount} overrides)";
        }

        /// <summary>打开当前类别对应的 Import/Export 窗口（null = 无按钮）。</summary>
        protected virtual Action OpenImportWindow() => null;

        /// <summary>预设模板下拉列表。selectedType 为当前实体的 C# 类型，可用于按子类过滤。</summary>
        protected virtual (string label, string assetName)[] GetTemplatePresets(Type selectedType) => null;

        // ═══════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════

        protected virtual void OnEnable()
        {
            minSize = new Vector2(900, 500);
            _treeView = new EditorTreeView();
            RefreshAssetList();
        }

        protected virtual void OnDisable()
        {
            if (_hasChanges && _selectedPreset != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_selectedPreset.name}' before closing?", "Save", "Discard"))
                    Save();
            }
        }

        protected virtual void OnGUI()
        {
            if (_hasChanges && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.S && Event.current.control)
            { Save(); Event.current.Use(); }

            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.BeginVertical();

            DrawHeader();
            EditorCard.Gap(EditorTokens.Pad);
            DrawThreeColumnBody();
            EditorCard.Gap(EditorTokens.Pad);
            DrawStatusBar();

            EditorGUILayout.EndVertical();
            GUILayout.Space(EditorTokens.Pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(EditorTokens.Pad);
        }

        // ═══════════════════════════════════════════════════
        //  [1] Header
        // ═══════════════════════════════════════════════════

        protected virtual void DrawHeader()
        {
            EditorCard.Draw(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw(GetEditorTitle(), style: EditorTokens.HeaderTitleStyle);
                var subWidth = EditorTokens.BreadcrumbStyle.CalcSize(new GUIContent(GetBreadcrumb())).x;
                EditorLabel.Draw(GetBreadcrumb(), subWidth, style: EditorTokens.BreadcrumbStyle);
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad);

                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Default("Refresh", EditorButtonSize.Medium))
                    RefreshAssetList();

                var openImport = OpenImportWindow();
                if (openImport != null)
                {
                    if (EditorButton.Default("Import/Export", EditorButtonSize.Medium))
                        openImport();
                }

                DrawExtraToolbarButtons();

                GUILayout.FlexibleSpace();

                if (EditorButton.Success("+ Create", EditorButtonSize.Medium))
                    ShowCreateMenu();

                if (EditorButton.Primary(_hasChanges ? "Save *" : "Save",
                        EditorButtonSize.Medium, enabled: _hasChanges))
                    Save();

                if (EditorButton.Danger("Delete", EditorButtonSize.Medium, enabled: _selectedPreset != null))
                    DeleteSelectedPreset();

                EditorGUILayout.EndHorizontal();
            });
        }

        // ═══════════════════════════════════════════════════
        //  [2] Three-Column Body
        // ═══════════════════════════════════════════════════

        protected virtual void DrawThreeColumnBody()
        {
            EditorGUILayout.BeginHorizontal();

            // [2a] Left
            EditorGUILayout.BeginHorizontal(GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            EditorCard.Draw(DrawLeftPanel);
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(EditorTokens.Pad);

            // [2b] Center (expand)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawCenterPanel();
            EditorGUILayout.EndVertical();

            EditorCard.Gap(EditorTokens.Pad);

            // [2c] Right (200px)
            EditorGUILayout.BeginHorizontal(GUILayout.Width(RightWidth), GUILayout.ExpandHeight(true));
            DrawPreviewPanel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  [2a] Left Panel — Asset Tree
        // ═══════════════════════════════════════════════════

        protected virtual void DrawLeftPanel()
        {
            _searchFilter = EditorSearchBar.Draw(_searchFilter);

            EditorCard.Gap(EditorTokens.Pad);

            if (_searchFilter != _lastSearchFilter)
            {
                _lastSearchFilter = _searchFilter;
                ApplyTreeFilter();
            }

            if (_treeRoots.Count == 0)
            {
                EditorLabel.Draw("No entities found.", style: EditorTokens.EmptyStateStyle);
                return;
            }

            var treeRect = EditorGUILayout.GetControlRect(
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(60f));

            if (treeRect.height > 0)
                _treeView.OnGUI(treeRect);
        }

        protected virtual void ApplyTreeFilter()
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                _treeView.SetData(_treeRoots, OnTreeSelect, OnTreeDelete);
                return;
            }

            var q = _searchFilter.ToLowerInvariant();
            var filtered = FilterTreeNodes(_treeRoots, q);
            _treeView.SetData(filtered, OnTreeSelect, OnTreeDelete);
        }

        protected static List<EditorTreeNode> FilterTreeNodes(List<EditorTreeNode> roots, string q)
        {
            var result = new List<EditorTreeNode>();
            foreach (var root in roots)
            {
                if (root.IsFolder)
                {
                    var filteredChildren = FilterTreeNodes(root.Children, q);
                    if (filteredChildren.Count > 0 || root.DisplayName.ToLowerInvariant().Contains(q))
                    {
                        var folder = new EditorTreeNode
                        {
                            DisplayName = root.DisplayName,
                            FullPath = root.FullPath,
                            IsFolder = true,
                            Depth = root.Depth,
                            LeafCount = filteredChildren.Count,
                        };
                        folder.Children.AddRange(filteredChildren);
                        result.Add(folder);
                    }
                }
                else if (root.DisplayName.ToLowerInvariant().Contains(q))
                {
                    result.Add(root);
                }
            }
            return result;
        }

        protected virtual void OnTreeSelect(EditorTreeNode node)
        {
            if (node != null && !node.IsFolder)
            {
                var preset = node.UserData as PropertyPresetSO;
                if (preset != null) SelectPreset(preset);
            }
        }

        protected virtual void OnTreeDelete(EditorTreeNode node)
        {
            if (node == null || node.IsFolder) return;
            var preset = node.UserData as PropertyPresetSO;
            if (preset == null) return;

            var path = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(path)) return;

            if (EditorUtility.DisplayDialog("Delete Entity",
                $"Delete '{preset.name}'?\nThis will delete the asset file permanently.",
                "Delete", "Cancel"))
            {
                if (_selectedPreset == preset) { _selectedPreset = null; _structure = null; _overrideValues?.Clear(); }
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                RefreshAssetList();
            }
        }

        // ═══════════════════════════════════════════════════
        //  [2b] Center Panel — Editor
        // ═══════════════════════════════════════════════════

        protected virtual void DrawCenterPanel()
        {
            if (_selectedPreset == null)
            {
                GUILayout.FlexibleSpace();
                EditorLabel.Draw("Select an entity from the left panel.", style: EditorTokens.EmptyStateStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll);

            DrawBasicSection();
            EditorCard.Gap(EditorTokens.Pad);
            DrawCategorySpecificSection();
            DrawPropertyOverrides();

            EditorGUILayout.EndScrollView();
        }

        // ── Basic Section ──

        protected virtual void DrawBasicSection()
        {
            EditorCard.Draw("Basic", () =>
            {
                // ── Content Id (read-only, 由 Category + Id 派生) ──
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Content Id", LabelWidth);
                GUILayout.Space(EditorTokens.Pad);
                _overrideValues.TryGetValue("Common/Category", out var cat);
                _overrideValues.TryGetValue("Common/Id", out var cid);
                bool hasId = !string.IsNullOrEmpty(cid);
                string idForPreview = hasId ? cid : AssetNameToSnakeCase(_selectedPreset.name);
                string preview;
                if (!string.IsNullOrEmpty(cat))
                    preview = $"{cat}.{idForPreview}";
                else if (hasId)
                    preview = "(Category not set)";
                else
                    preview = $"(fallback: {_selectedPreset.name})";
                EditorGUI.BeginDisabledGroup(true);
                EditorInput.TextField(preview);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad / 2);

                // ── Template ──
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Template", LabelWidth);
                GUILayout.Space(EditorTokens.Pad);
                DrawTemplateField();
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad / 2);

                // ── Prefab ──
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Prefab", LabelWidth);
                GUILayout.Space(EditorTokens.Pad);
                var newPrefab = EditorInput.ObjectField<GameObject>(_selectedPreset.Prefab);
                if (newPrefab != _selectedPreset.Prefab)
                {
                    _selectedPreset.Prefab = newPrefab;
                    _hasChanges = true;
                    EditorUtility.SetDirty(_selectedPreset);
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private static string AssetNameToSnakeCase(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return "";
            // 移除 "New" 前缀 (CreateAsset 的默认命名)
            var name = assetName.StartsWith("New") ? assetName[3..] : assetName;
            // 移除 SO 后缀 (如 "MeleeWeapon" 中的 "Weapon" 等保留)
            // 处理驼峰命名转 snake_case
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }

        /// <summary>Template 字段：有预设列表用下拉按钮，否则回退 ObjectField。</summary>
        private void DrawTemplateField()
        {
            var presets = GetTemplatePresets(_selectedPreset?.GetType());
            if (presets == null || presets.Length == 0)
            {
                // Fallback: raw ObjectField
                var newTemplate = EditorInput.ObjectField<PropertyTreeSO>(_selectedPreset.Template);
                if (newTemplate != _selectedPreset.Template)
                    ApplyTemplateChange(newTemplate);
                return;
            }

            // Resolve preset names → SO references (cached)
            var presetSOs = ResolvePresetSOs(presets);

            // Find current selection index
            int currentIndex = -1;
            var currentName = _selectedPreset.Template != null ? _selectedPreset.Template.name : null;
            for (int i = 0; i < presetSOs.Length; i++)
            {
                if (presetSOs[i] != null && presetSOs[i].name == currentName)
                { currentIndex = i; break; }
            }

            var displayLabel = currentIndex >= 0 ? presets[currentIndex].label : "None";
            if (GUILayout.Button(displayLabel, EditorStyles.popup, GUILayout.MinWidth(120f)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None"), currentIndex < 0, () => ApplyTemplateChange(null));
                menu.AddSeparator("");
                for (int i = 0; i < presets.Length; i++)
                {
                    var capturedIndex = i;
                    var so = presetSOs[i];
                    var label = so != null ? presets[i].label : presets[i].label + " (missing)";
                    if (so != null)
                        menu.AddItem(new GUIContent(label), currentIndex == capturedIndex,
                            () => ApplyTemplateChange(so));
                    else
                        menu.AddDisabledItem(new GUIContent(label));
                }
                menu.ShowAsContext();
            }
        }

        private void ApplyTemplateChange(PropertyTreeSO newTemplate)
        {
            if (newTemplate == _selectedPreset.Template) return;

            if (_selectedPreset.Template != null && _overrideValues.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Template Change",
                    "Changing the template will clear all property overrides. Continue?", "Change", "Cancel"))
                    return;
            }
            _selectedPreset.Template = newTemplate;
            _hasChanges = true;
            SelectPreset(_selectedPreset);
        }

        private static Dictionary<string, PropertyTreeSO> s_templateCache;
        private static float s_lastTemplateCacheRefresh;

        private static Dictionary<string, PropertyTreeSO> GetTemplateCache()
        {
            var now = (float)EditorApplication.timeSinceStartup;
            if (s_templateCache != null && now - s_lastTemplateCacheRefresh < 5f)
                return s_templateCache;

            s_lastTemplateCacheRefresh = now;
            var dict = new Dictionary<string, PropertyTreeSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyTreeSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<PropertyTreeSO>(path);
                if (so != null && !dict.ContainsKey(so.name))
                    dict[so.name] = so;
            }
            s_templateCache = dict;
            return dict;
        }

        private static PropertyTreeSO[] ResolvePresetSOs((string label, string assetName)[] presets)
        {
            var result = new PropertyTreeSO[presets.Length];
            var nameToSO = GetTemplateCache();
            for (int i = 0; i < presets.Length; i++)
                nameToSO.TryGetValue(presets[i].assetName, out result[i]);
            return result;
        }

        // ── Property Overrides ──

        protected virtual void DrawPropertyOverrides()
        {
            if (_propertyGroups == null || _propertyGroups.Count == 0)
            {
                EditorCard.Draw("Properties", () =>
                    EditorLabel.Draw("No properties. Select a Template first.", style: EditorTokens.EmptyStateStyle));
                return;
            }

            for (int i = 0; i < _propertyGroupOrder.Count; i++)
            {
                if (i > 0) EditorCard.Gap(EditorTokens.Pad);

                var folderName = _propertyGroupOrder[i];
                var props = _propertyGroups[folderName];

                EditorCard.Draw(folderName, () =>
                {
                    foreach (var (path, def) in props)
                    {
                        var relativePath = path.Substring(folderName.Length + 1);
                        var displayName = relativePath.Contains('/')
                            ? relativePath.Replace("/", " / ")
                            : relativePath;

                        var isOverride = _overrideValues.TryGetValue(path, out var rawValue);

                        EditorCard.Gap(EditorTokens.Pad / 2);
                        DrawPropertyRow(path, displayName, def, isOverride, rawValue);
                    }
                });
            }
        }

        // ═══════════════════════════════════════════════════
        //  Property Row Dispatch
        // ═══════════════════════════════════════════════════

        protected void DrawPropertyRow(string path, string displayName, PropertyDefSO def,
            bool isOverride, string rawValue)
        {
            EditorGUILayout.BeginHorizontal();

            var oldColor = GUI.color;
            GUI.color = isOverride ? Color.white : Color.gray;
            EditorLabel.Draw(displayName, LabelWidth, tooltip: $"{def.Type} — {def.Description}");
            GUI.color = oldColor;

            GUILayout.Space(EditorTokens.Pad);

            switch (def)
            {
                case FloatPropertyDefSO fd: DrawFloatRow(path, fd, isOverride, rawValue); break;
                case IntPropertyDefSO id: DrawIntRow(path, id, isOverride, rawValue); break;
                case BoolPropertyDefSO bd: DrawBoolRow(path, bd, isOverride, rawValue); break;
                case StringPropertyDefSO sd: DrawStringRow(path, sd, isOverride, rawValue); break;
                case RdTagPropertyDefSO rd: DrawRdTagRow(path, rd, isOverride, rawValue); break;
                case RdTagListPropertyDefSO rl: DrawRdTagListRow(path, rl, isOverride, rawValue); break;
                case AssetRefPropertyDefSO ad: DrawAssetRefRow(path, ad, isOverride, rawValue); break;
                case AssetRefListPropertyDefSO al: DrawAssetRefListRow(path, isOverride, rawValue); break;
                case StructPropertyDefSO st: DrawStructRow(path, st, isOverride, rawValue); break;
                default: EditorLabel.Draw(rawValue ?? GetDefaultRawValue(def)); break;
            }

            if (isOverride)
            {
                if (EditorButton.Delete())
                {
                    _overrideValues.Remove(path);
                    _hasChanges = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Per-Type Row Renderers ──

        protected void DrawFloatRow(string path, FloatPropertyDefSO def, bool isOverride, string rawValue)
        {
            float currentVal = ParseFloat(rawValue, def.DefaultValue);
            float currentMin = _minOverrides.TryGetValue(path, out var min) ? min : def.Min;
            float currentMax = _maxOverrides.TryGetValue(path, out var max) ? max : def.Max;

            EditorGUILayout.BeginHorizontal();
            var newVal = EditorInput.FloatField(currentVal, width: 55f);
            GUILayout.Space(EditorTokens.Pad / 2);
            EditorLabel.Draw("Min", 25f);
            var newMin = EditorInput.FloatField(currentMin, width: 45f);
            GUILayout.Space(EditorTokens.Pad / 2);
            EditorLabel.Draw("Max", 28f);
            var newMax = EditorInput.FloatField(currentMax, width: 45f);
            EditorGUILayout.EndHorizontal();

            if (Math.Abs(newVal - currentVal) > 0.0001f)
                SetOverride(path, newVal.ToString(CultureInfo.InvariantCulture), def.DefaultValue, newVal);
            if (Math.Abs(newMin - currentMin) > 0.0001f) { _minOverrides[path] = newMin; _hasChanges = true; }
            if (Math.Abs(newMax - currentMax) > 0.0001f) { _maxOverrides[path] = newMax; _hasChanges = true; }
        }

        protected void DrawIntRow(string path, IntPropertyDefSO def, bool isOverride, string rawValue)
        {
            int currentVal = ParseInt(rawValue, def.DefaultValue);
            int currentMin = _minOverrides.TryGetValue(path, out var min) ? (int)min : def.Min;
            int currentMax = _maxOverrides.TryGetValue(path, out var max) ? (int)max : def.Max;

            EditorGUILayout.BeginHorizontal();
            var newVal = EditorInput.IntField(currentVal, width: 55f);
            GUILayout.Space(EditorTokens.Pad / 2);
            EditorLabel.Draw("Min", 25f);
            var newMin = EditorInput.IntField(currentMin, width: 45f);
            GUILayout.Space(EditorTokens.Pad / 2);
            EditorLabel.Draw("Max", 28f);
            var newMax = EditorInput.IntField(currentMax, width: 45f);
            EditorGUILayout.EndHorizontal();

            if (newVal != currentVal)
                SetOverride(path, newVal.ToString(), def.DefaultValue, newVal);
            if (newMin != currentMin) { _minOverrides[path] = newMin; _hasChanges = true; }
            if (newMax != currentMax) { _maxOverrides[path] = newMax; _hasChanges = true; }
        }

        protected void DrawBoolRow(string path, BoolPropertyDefSO def, bool isOverride, string rawValue)
        {
            bool current = isOverride ? ParseBool(rawValue, def.DefaultValue) : def.DefaultValue;
            bool next = EditorInput.Toggle(current);
            if (next != current)
                SetOverride(path, next ? "true" : "false", def.DefaultValue, next);
        }

        protected void DrawStringRow(string path, StringPropertyDefSO def, bool isOverride, string rawValue)
        {
            string current = isOverride ? rawValue : def.DefaultValue;
            bool isDescription = path.EndsWith("/Description") || path == "Description";

            string next;
            if (isDescription)
            {
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                next = EditorGUILayout.TextArea(current ?? "", style,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight * 3), GUILayout.ExpandWidth(true));
            }
            else
            {
                next = EditorInput.TextField(current ?? "");
            }

            if (next != current)
                SetOverride(path, next, def.DefaultValue, next);
        }

        protected void DrawRdTagRow(string path, RdTagPropertyDefSO def, bool isOverride, string rawValue)
        {
            string current = isOverride ? rawValue : def.DefaultValue;
            EditorGUILayout.BeginHorizontal();
            string next = EditorInput.TextField(current ?? "");
            if (EditorButton.Default("Tag", EditorButtonSize.Small, width: 35f))
            {
                TagPicker.Show(GUILayoutUtility.GetLastRect(), currentFullTag: current, onSelected: tagDef =>
                {
                    if (tagDef != null)
                        SetOverride(path, tagDef.FullTag, def.DefaultValue, tagDef.FullTag);
                });
            }
            if (next != current)
                SetOverride(path, next, def.DefaultValue, next);
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawRdTagListRow(string path, RdTagListPropertyDefSO def, bool isOverride, string rawValue)
        {
            string[] tags = isOverride ? ParseTagList(rawValue) : Array.Empty<string>();
            DrawTagChips(tags, newTags => SaveTagList(path, newTags));
        }

        protected void DrawAssetRefRow(string path, AssetRefPropertyDefSO def, bool isOverride, string rawValue)
        {
            Object current = isOverride
                ? AssetRefPropertyDefSO.Load(rawValue, def.AssetTypeConstraint)
                : AssetRefPropertyDefSO.Load(def.DefaultAssetGUID, def.AssetTypeConstraint);
            Object next = EditorInput.ObjectField(current, typeof(Object), false);
            if (next != current)
            {
                var guid = next != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(next)) : "";
                SetOverride(path, guid, def.DefaultAssetGUID, guid);
            }
        }

        protected void DrawAssetRefListRow(string path, bool isOverride, string rawValue)
        {
            string[] guids = isOverride ? ParseGuidList(rawValue) : Array.Empty<string>();
            EditorGUILayout.BeginVertical();

            for (int i = 0; i < guids.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var asset = !string.IsNullOrEmpty(guids[i])
                    ? AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[i]))
                    : null;
                var next = EditorInput.ObjectField(asset, typeof(Object), false);
                if (next != asset)
                {
                    guids[i] = next != null
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(next)) : "";
                    SetOverrideForAssetRefList(path, guids);
                }
                if (EditorButton.Delete())
                {
                    var list = guids.ToList(); list.RemoveAt(i);
                    SetOverrideForAssetRefList(path, list.ToArray());
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            if (EditorButton.Default("+ Add Ref", EditorButtonSize.Small))
            {
                var list = guids.ToList(); list.Add("");
                SetOverrideForAssetRefList(path, list.ToArray());
            }

            EditorGUILayout.EndVertical();
        }

        protected void DrawStructRow(string path, StructPropertyDefSO def, bool isOverride, string rawValue)
        {
            var typeName = def.StructTypeName ?? "";
            if (typeName == "SlotDef" || typeName.EndsWith(".SlotDef"))
            {
                DrawSlotDefEditor(path, def, isOverride, rawValue);
                return;
            }

            var oldColor = GUI.color;
            GUI.color = Color.gray;
            EditorLabel.Draw($"(Struct: {typeName})", style: EditorStyles.label);
            GUI.color = oldColor;
        }

        // ═══════════════════════════════════════════════════
        //  Tag Chips + SlotDef Editor
        // ═══════════════════════════════════════════════════

        protected static void DrawTagChips(string[] tags, Action<string[]> onChanged)
        {
            EditorGUILayout.BeginHorizontal();

            if (tags.Length == 0)
            {
                EditorLabel.Draw("(none)", style: EditorTokens.DimLabelStyle);
            }
            else
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    if (i > 0) GUILayout.Space(EditorTokens.Pad / 2);
                    var shortName = tags[i].Contains('.') ? tags[i].Substring(tags[i].LastIndexOf('.') + 1) : tags[i];
                    EditorButton.Draw(shortName, EditorButtonType.Default, EditorButtonSize.Small,
                        width: 80f, tooltip: tags[i]);
                    var capturedIndex = i;
                    if (EditorButton.Delete())
                    {
                        var list = tags.ToList(); list.RemoveAt(capturedIndex);
                        onChanged(list.ToArray());
                    }
                }
            }

            GUILayout.FlexibleSpace();
            if (EditorButton.Default("+", EditorButtonSize.Small, width: 22f))
            {
                var captured = tags;
                TagPicker.Show(GUILayoutUtility.GetLastRect(), onSelected: tagDef =>
                {
                    if (tagDef != null)
                    {
                        var list = captured.ToList();
                        if (!list.Contains(tagDef.FullTag)) list.Add(tagDef.FullTag);
                        onChanged(list.ToArray());
                    }
                });
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void DrawSlotDefEditor(string path, StructPropertyDefSO def, bool isOverride, string rawValue)
        {
            var slotId = path.Substring(path.LastIndexOf('/') + 1);

            var json = isOverride ? rawValue : def.DefaultJson;
            if (string.IsNullOrEmpty(json)) json = "{}";
            SlotDef slot = default;
            try
            {
                if (!string.IsNullOrEmpty(json) && json.TrimStart().StartsWith("{"))
                {
                    var wrapper = JsonUtility.FromJson<SlotListWrap>(json);
                    if (wrapper?.Items is { Length: > 0 })
                        slot = wrapper.Items[0];
                    else
                        slot = JsonUtility.FromJson<SlotDef>(json);
                }
            }
            catch { }
            slot.SlotId = slotId;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical();

            EditorCard.Gap(EditorTokens.Pad / 2);
            EditorLabel.Draw("Tags", 55f, tooltip: "此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空 = 接受所有。");
            var capturedSlotId = slotId;
            DrawTagChips(slot.AcceptTags ?? Array.Empty<string>(), newTags =>
            {
                slot.AcceptTags = newTags;
                slot.SlotId = capturedSlotId;
                SaveSlotDefOverride(path, slot);
            });

            EditorCard.Gap(EditorTokens.Pad / 2);
            EditorGUILayout.BeginHorizontal();
            EditorLabel.Draw("Cap", 55f, tooltip: "槽位容量（物品数量上限）。");
            GUILayout.Space(EditorTokens.Pad);
            var newCap = EditorInput.IntField(slot.Capacity, width: 60f);
            EditorLabel.Draw("Wt", 30f, tooltip: "此槽位内物品的总重量上限。0 = 无限制。");
            GUILayout.Space(EditorTokens.Pad / 2);
            var newWt = EditorInput.FloatField(slot.WeightLimit, width: 60f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                slot.SlotId = slotId;
                slot.Capacity = newCap;
                slot.WeightLimit = newWt;
                SaveSlotDefOverride(path, slot);
            }
        }

        // ═══════════════════════════════════════════════════
        //  [2c] Right Panel — Preview
        // ═══════════════════════════════════════════════════

        protected virtual void DrawPreviewPanel()
        {
            if (_selectedPreset == null) return;

            string iconPath = null;
            if (_structure != null)
            {
                foreach (var kv in _structure)
                {
                    if (kv.Key.EndsWith("/Icon") || kv.Key == "Icon")
                    { iconPath = kv.Key; break; }
                }
            }

            EditorCard.Draw(() =>
            {
                if (iconPath != null)
                {
                    Object iconAsset = ResolveIconAsset(iconPath);
                    EditorCard.Draw("Icon", () =>
                    {
                        if (iconAsset != null)
                        {
                            var previewRect = GUILayoutUtility.GetRect(
                                RightWidth - EditorTokens.PadCard * 3, 80f,
                                GUILayout.ExpandHeight(false));
                            previewRect.height = Mathf.Min(previewRect.width, 80f);
                            var tex = AssetPreview.GetAssetPreview(iconAsset)
                                ?? AssetPreview.GetMiniThumbnail(iconAsset);
                            if (tex != null)
                                GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
                        }
                        else
                        {
                            EditorLabel.Draw("No icon assigned.", style: EditorTokens.EmptyStateStyle);
                        }
                    });
                    EditorCard.Gap(EditorTokens.Pad);
                }

                EditorCard.Draw("Prefab", () =>
                {
                    if (_selectedPreset.Prefab != null)
                    {
                        var previewRect = GUILayoutUtility.GetRect(
                            RightWidth - EditorTokens.PadCard * 3, 160f,
                            GUILayout.ExpandHeight(false));
                        previewRect.height = Mathf.Min(previewRect.width, 160f);

                        var preview = AssetPreview.GetAssetPreview(_selectedPreset.Prefab);
                        if (preview != null)
                        {
                            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                        }
                        else
                        {
                            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                            EditorLabel.Draw("Loading preview...", style: EditorTokens.EmptyStateStyle);
                        }
                    }
                    else
                    {
                        EditorLabel.Draw("No Prefab assigned.", style: EditorTokens.EmptyStateStyle);
                    }
                });
            });
        }

        protected Object ResolveIconAsset(string iconPath)
        {
            if (_structure == null || !_structure.TryGetValue(iconPath, out var def)
                || def is not AssetRefPropertyDefSO ad)
                return null;

            if (_overrideValues.TryGetValue(iconPath, out var rawGuid))
                return AssetRefPropertyDefSO.Load(rawGuid, ad.AssetTypeConstraint);

            return AssetRefPropertyDefSO.Load(ad.DefaultAssetGUID, ad.AssetTypeConstraint);
        }

        // ═══════════════════════════════════════════════════
        //  [3] Status Bar
        // ═══════════════════════════════════════════════════

        protected virtual void DrawStatusBar()
        {
            EditorCard.Draw(() =>
            {
                EditorLabel.Draw(GetStatusSummary(), style: EditorTokens.DimLabelStyle);
            });
        }

        // ═══════════════════════════════════════════════════
        //  Operations
        // ═══════════════════════════════════════════════════

        protected void SelectPreset(PropertyPresetSO preset)
        {
            if (_hasChanges && _selectedPreset != null && _selectedPreset != preset)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_selectedPreset.name}' before switching?", "Save", "Discard"))
                    Save();
                else
                    _hasChanges = false;
            }

            _selectedPreset = preset;
            _hasChanges = false;
            _overrideValues = new Dictionary<string, string>();
            _minOverrides = new Dictionary<string, float>();
            _maxOverrides = new Dictionary<string, float>();

            if (preset?.Template != null)
            {
                _structure = ResolveStructureEditor(preset.Template);
                var parsed = ParseOverrides(preset.OverridesJson, _minOverrides, _maxOverrides);
                foreach (var (k, v) in parsed)
                    _overrideValues[k] = v;

                // 预计算属性分组（避免 DrawPropertyOverrides 每帧重建）
                _propertyGroups = new Dictionary<string, List<(string path, PropertyDefSO def)>>();
                _propertyGroupOrder = new List<string>();
                foreach (var kv in _structure)
                {
                    var slash = kv.Key.IndexOf('/');
                    var folder = slash > 0 ? kv.Key.Substring(0, slash) : kv.Key;
                    if (!_propertyGroups.TryGetValue(folder, out var list))
                    {
                        list = new List<(string, PropertyDefSO)>();
                        _propertyGroups[folder] = list;
                        _propertyGroupOrder.Add(folder);
                    }
                    list.Add((kv.Key, kv.Value));
                }
            }
            else
            {
                _structure = null;
                _propertyGroups = null;
                _propertyGroupOrder = null;
            }

            Repaint();
        }

        protected void Save()
        {
            if (_selectedPreset == null) return;

            // 首次保存：Id 未设置时从 asset name 自动推导（跳过默认 "New*" 名称）
            if (!_overrideValues.ContainsKey("Common/Id")
                && !string.IsNullOrEmpty(_selectedPreset.name)
                && !_selectedPreset.name.StartsWith("New"))
            {
                var autoId = AssetNameToSnakeCase(_selectedPreset.name);
                if (!string.IsNullOrEmpty(autoId))
                    _overrideValues["Common/Id"] = autoId;
            }

            var entries = new List<OverrideEntry>();
            foreach (var (path, value) in _overrideValues)
            {
                if (string.IsNullOrEmpty(value)) continue;
                var entry = new OverrideEntry { Path = path, Value = value };
                if (_minOverrides.TryGetValue(path, out var min))
                    entry.Min = min.ToString(CultureInfo.InvariantCulture);
                if (_maxOverrides.TryGetValue(path, out var max))
                    entry.Max = max.ToString(CultureInfo.InvariantCulture);
                entries.Add(entry);
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

            var container = new OverrideContainer { Overrides = entries };
            _selectedPreset.OverridesJson = JsonUtility.ToJson(container, true);

            EditorUtility.SetDirty(_selectedPreset);
            AssetDatabase.SaveAssets();

            _hasChanges = false;
            Repaint();
        }

        protected virtual void ShowCreateMenu()
        {
            var menu = new GenericMenu();
            foreach (var (label, soType) in GetCreateMenuItems())
            {
                var capturedType = soType;
                menu.AddItem(new GUIContent(label), false,
                    () => CreateAsset(capturedType));
            }
            menu.ShowAsContext();
        }

        protected void CreateAsset(Type soType)
        {
            var dir = GetAssetDirForType(soType);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                AssetDatabase.CreateFolder(string.Join("/", parts.Take(parts.Length - 1)), parts.Last());
            }

            var asset = CreateInstance(soType);
            asset.name = $"New{soType.Name.Replace("SO", "")}";

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{asset.name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DataLabelTools.EnsureBootLabel(path);
            RefreshAssetList();
            SelectPreset((PropertyPresetSO)asset);
        }

        protected void DeleteSelectedPreset()
        {
            if (_selectedPreset == null) return;

            var path = AssetDatabase.GetAssetPath(_selectedPreset);
            if (string.IsNullOrEmpty(path)) return;

            if (!EditorUtility.DisplayDialog("Delete Entity",
                $"Delete '{_selectedPreset.name}'?\nThis will delete the asset file permanently.",
                "Delete", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            _selectedPreset = null;
            _structure = null;
            _overrideValues.Clear();
            _hasChanges = false;
            RefreshAssetList();
        }

        // ═══════════════════════════════════════════════════
        //  Tree Data
        // ═══════════════════════════════════════════════════

        protected void RefreshAssetList()
        {
            var items = new List<PropertyPresetSO>();
            var filterType = GetTargetType();
            var searchFilter = GetAssetFilter();

            foreach (var guid in AssetDatabase.FindAssets(searchFilter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<PropertyPresetSO>(path);
                if (asset != null && filterType.IsInstanceOfType(asset))
                    items.Add(asset);
            }

            BuildTree(items);
            _lastSearchFilter = _searchFilter;
            ApplyTreeFilter();
            Repaint();
        }

        protected void BuildTree(List<PropertyPresetSO> items)
        {
            _treeRoots.Clear();

            var groups = new Dictionary<string, List<PropertyPresetSO>>();
            var noTemplate = new List<PropertyPresetSO>();

            foreach (var item in items)
            {
                if (item.Template != null)
                {
                    var key = item.Template.name;
                    if (!groups.TryGetValue(key, out var list))
                        groups[key] = list = new List<PropertyPresetSO>();
                    list.Add(item);
                }
                else
                {
                    noTemplate.Add(item);
                }
            }

            foreach (var (templateName, groupItems) in groups.OrderBy(kv => kv.Key))
            {
                var folder = new EditorTreeNode
                {
                    DisplayName = templateName,
                    FullPath = templateName,
                    IsFolder = true,
                    Depth = 0,
                };

                foreach (var item in groupItems.OrderBy(i => i.name))
                {
                    folder.Children.Add(new EditorTreeNode
                    {
                        DisplayName = item.name,
                        FullPath = $"{templateName}/{item.name}",
                        IsFolder = false,
                        UserData = item,
                        Depth = 1,
                    });
                }

                folder.LeafCount = folder.Children.Count;
                _treeRoots.Add(folder);
            }

            foreach (var item in noTemplate.OrderBy(i => i.name))
            {
                _treeRoots.Add(new EditorTreeNode
                {
                    DisplayName = item.name,
                    FullPath = item.name,
                    IsFolder = false,
                    UserData = item,
                    Depth = 0,
                });
            }
        }

        // ═══════════════════════════════════════════════════
        //  Override Helpers
        // ═══════════════════════════════════════════════════

        protected static Dictionary<string, string> ParseOverrides(string json,
            Dictionary<string, float> minOverrides, Dictionary<string, float> maxOverrides)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            try
            {
                var container = JsonUtility.FromJson<OverrideContainer>(json);
                if (container?.Overrides != null)
                {
                    foreach (var entry in container.Overrides)
                    {
                        if (string.IsNullOrEmpty(entry.Path)) continue;
                        if (entry.Value != null) result[entry.Path] = entry.Value;

                        if (!string.IsNullOrEmpty(entry.Min) && float.TryParse(entry.Min,
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var m))
                            minOverrides[entry.Path] = m;
                        if (!string.IsNullOrEmpty(entry.Max) && float.TryParse(entry.Max,
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                            maxOverrides[entry.Path] = x;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EntityEditor] Parse overrides failed: {e.Message}");
            }

            return result;
        }

        // ═══════════════════════════════════════════════════
        //  Value Helpers
        // ═══════════════════════════════════════════════════

        protected void SetOverride<T>(string path, string rawValue, T defaultValue, T currentValue)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, defaultValue))
                _overrideValues.Remove(path);
            else
                _overrideValues[path] = rawValue;
            _hasChanges = true;
        }

        protected void SaveSlotDefOverride(string path, SlotDef slot)
        {
            var json = JsonUtility.ToJson(slot);
            SetOverride(path, json, "{}", json);
        }

        protected void SaveTagList(string path, string[] tags)
        {
            var json = BuildJsonArray(tags);
            SetOverride(path, json, Array.Empty<string>(), tags);
        }

        protected void SetOverrideForAssetRefList(string path, string[] guids)
        {
            var json = BuildJsonArray(guids);
            SetOverride(path, json, Array.Empty<string>(), guids);
        }

        protected static string GetDefaultRawValue(PropertyDefSO def)
        {
            switch (def)
            {
                case FloatPropertyDefSO fd: return fd.DefaultValue.ToString(CultureInfo.InvariantCulture);
                case IntPropertyDefSO id: return id.DefaultValue.ToString();
                case BoolPropertyDefSO bd: return bd.DefaultValue ? "true" : "false";
                case StringPropertyDefSO sd: return sd.DefaultValue ?? "";
                case RdTagPropertyDefSO rd: return rd.DefaultValue ?? "";
                case AssetRefPropertyDefSO ad: return ad.DefaultAssetGUID ?? "";
                default: return "";
            }
        }

        protected static float ParseFloat(string raw, float fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
        }

        protected static int ParseInt(string raw, int fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return int.TryParse(raw, out var i) ? i : fallback;
        }

        protected static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return bool.TryParse(raw, out var b) ? b : fallback;
        }

        protected static string[] ParseTagList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try
            {
                var wrapper = JsonUtility.FromJson<TagListWrap>($"{{\"Items\":{raw}}}");
                return wrapper?.Items ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        protected static string[] ParseGuidList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try
            {
                var wrapper = JsonUtility.FromJson<GuidListWrap>($"{{\"Items\":{raw}}}");
                return wrapper?.Items ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        protected static string BuildJsonArray(string[] items)
        {
            if (items == null || items.Length == 0) return "[]";
            var escaped = items.Select(s => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            return "[" + string.Join(",", escaped) + "]";
        }

        protected static string Truncate(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "...";
        }

        // ═══════════════════════════════════════════════════
        //  Editor Structure Resolver
        // ═══════════════════════════════════════════════════

        protected static Dictionary<string, PropertyDefSO> ResolveStructureEditor(PropertyTreeSO tree)
        {
            var defLookup = new Dictionary<string, PropertyDefSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:PropertyDefSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(p);
                if (def != null && !string.IsNullOrEmpty(def.Id))
                    defLookup[def.Id] = def;
            }

            var allNodes = tree.ResolveAllNodes();
            var childrenByParent = new Dictionary<string, List<PropertyNode>>();
            foreach (var node in allNodes.Values)
            {
                var key = node.ParentId ?? "";
                if (!childrenByParent.TryGetValue(key, out var list))
                    childrenByParent[key] = list = new List<PropertyNode>();
                list.Add(node);
            }

            var result = new Dictionary<string, PropertyDefSO>();
            var roots = allNodes.Values.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

            foreach (var root in roots)
                BuildStructurePaths(root.NodeId, allNodes, childrenByParent, "", result, defLookup);

            return result;
        }

        private static void BuildStructurePaths(string nodeId, Dictionary<string, PropertyNode> merged,
            Dictionary<string, List<PropertyNode>> childrenByParent, string parentPath,
            Dictionary<string, PropertyDefSO> result, Dictionary<string, PropertyDefSO> defLookup)
        {
            if (!merged.TryGetValue(nodeId, out var node)) return;
            var path = string.IsNullOrEmpty(parentPath) ? node.NodeId : $"{parentPath}/{node.NodeId}";

            bool isLeaf = !string.IsNullOrEmpty(node.DefId);

            if (isLeaf && defLookup.TryGetValue(node.DefId, out var def))
                result[path] = def;

            if (childrenByParent.TryGetValue(nodeId, out var children))
            {
                foreach (var child in children)
                {
                    if (isLeaf)
                    {
                        Debug.LogWarning($"[EntityEditor] Orphan: '{child.NodeId}' parent '{nodeId}' is a leaf, not a folder. Skipping.");
                        continue;
                    }
                    BuildStructurePaths(child.NodeId, merged, childrenByParent, path, result, defLookup);
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  Serializable Helpers (shared across all editors)
        // ═══════════════════════════════════════════════════

        [Serializable] protected class OverrideEntry { public string Path, Value, Min, Max; }
        [Serializable] protected class OverrideContainer { public List<OverrideEntry> Overrides = new(); }
        [Serializable] protected class TagListWrap { public string[] Items; }
        [Serializable] protected class SlotListWrap { public SlotDef[] Items; }
        [Serializable] protected class GuidListWrap { public string[] Items; }
    }
}
#endif
