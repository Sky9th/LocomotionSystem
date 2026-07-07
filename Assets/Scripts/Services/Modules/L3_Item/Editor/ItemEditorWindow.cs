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

namespace RedDust.Items.Editor
{
    public class ItemEditorWindow : EditorWindow
    {
        private const float LeftWidth = 280f;
        private const float RightWidth = 200f;
        private const float LabelWidth = 110f;

        // ── State ──
        private PropertyPresetSO _selectedItem;
        private EditorTreeView _treeView;
        private List<EditorTreeNode> _treeRoots = new();
        private string _lastSearchFilter = "";
        private Dictionary<string, PropertyDefSO> _structure;
        private Dictionary<string, string> _overrides;          // parsed from OverridesJson (saved state)
        private Dictionary<string, string> _overrideValues;     // working edited state
        private Dictionary<string, float> _minOverrides;
        private Dictionary<string, float> _maxOverrides;
        private bool _hasChanges;
        private string _searchFilter = "";
        private Vector2 _rightScroll;


        // ═══════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════

        [MenuItem("RedDust/Item Editor")]
        private static void Open() => GetWindow<ItemEditorWindow>("Item Editor");

        private void OnEnable()
        {
            minSize = new Vector2(900, 500);
            _treeView = new EditorTreeView();
            RefreshItemList();
        }

        private void OnDisable()
        {
            if (_hasChanges && _selectedItem != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_selectedItem.name}' before closing?", "Save", "Discard"))
                    Save();
            }
        }

        private void OnGUI()
        {
            // Ctrl+S
            if (_hasChanges && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.S && Event.current.control)
            { Save(); Event.current.Use(); }

            // Outer margins
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
        //  [1] Header
        // ═══════════════════════════════════════════════════

        private void DrawHeader()
        {
            EditorCard.Draw(() =>
            {
                // Row 1: Title + Breadcrumb
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Item Editor", style: EditorTokens.HeaderTitleStyle);
                var subWidth = EditorTokens.BreadcrumbStyle.CalcSize(new GUIContent("L3_Item · Editor")).x;
                EditorLabel.Draw("L3_Item · Editor", subWidth, style: EditorTokens.BreadcrumbStyle);
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad);

                // Row 2: Toolbar buttons
                EditorGUILayout.BeginHorizontal();
                if (EditorButton.Default("Refresh", EditorButtonSize.Medium))
                    RefreshItemList();
                if (EditorButton.Default("Import/Export", EditorButtonSize.Medium))
                    Debug.Log("[ItemEditor] Import/Export — not yet implemented.");

                GUILayout.FlexibleSpace();

                // +Create dropdown
                if (EditorButton.Success("+ Create", EditorButtonSize.Medium))
                    CreateNewItem();

                // Save
                if (EditorButton.Primary(_hasChanges ? "Save *" : "Save",
                        EditorButtonSize.Medium, enabled: _hasChanges))
                    Save();

                // Delete
                if (EditorButton.Danger("Delete", EditorButtonSize.Medium, enabled: _selectedItem != null))
                    DeleteSelectedItem();

                EditorGUILayout.EndHorizontal();
            });
        }

        // ═══════════════════════════════════════════════════
        //  [2] Two-Column Body
        // ═══════════════════════════════════════════════════

        private void DrawTwoColumns()
        {
            EditorGUILayout.BeginHorizontal();

            // [2a] Left Panel — Item Tree
            EditorGUILayout.BeginHorizontal(GUILayout.Width(LeftWidth), GUILayout.ExpandHeight(true));
            EditorCard.Draw(DrawLeftPanel);
            EditorGUILayout.EndHorizontal();

            EditorCard.Gap(EditorTokens.Pad);

            // [2b] Center Panel — Editor (expand)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorCard.Gap(EditorTokens.Pad);

            // [2c] Right Panel — Preview (200px)
            EditorGUILayout.BeginHorizontal(GUILayout.Width(RightWidth), GUILayout.ExpandHeight(true));
            DrawPreviewPanel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  [2a] Left Panel — Item Tree
        // ═══════════════════════════════════════════════════

        private void DrawLeftPanel()
        {
            _searchFilter = EditorSearchBar.Draw(_searchFilter);

            EditorCard.Gap(EditorTokens.Pad);

            // Rebuild tree data when filter changes
            if (_searchFilter != _lastSearchFilter)
            {
                _lastSearchFilter = _searchFilter;
                ApplyTreeFilter();
            }

            if (_treeRoots.Count == 0)
            {
                EditorLabel.Draw("No items found.", style: EditorTokens.EmptyStateStyle);
                return;
            }

            // TreeView fills remaining space
            var treeRect = EditorGUILayout.GetControlRect(
                GUILayout.ExpandHeight(true), GUILayout.MinHeight(60f));

            if (treeRect.height > 0)
            {
                _treeView.OnGUI(treeRect);
            }
        }

        private void ApplyTreeFilter()
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

        private static List<EditorTreeNode> FilterTreeNodes(List<EditorTreeNode> roots, string q)
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

        private void OnTreeSelect(EditorTreeNode node)
        {
            if (node != null && !node.IsFolder)
            {
                var item = node.UserData as PropertyPresetSO;
                if (item != null) SelectItem(item);
            }
        }

        private void OnTreeDelete(EditorTreeNode node)
        {
            if (node == null || node.IsFolder) return;
            var item = node.UserData as PropertyPresetSO;
            if (item == null) return;

            var path = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrEmpty(path)) return;

            if (EditorUtility.DisplayDialog("Delete Item",
                $"Delete '{item.name}'?\nThis will delete the asset file permanently.",
                "Delete", "Cancel"))
            {
                if (_selectedItem == item) { _selectedItem = null; _structure = null; _overrideValues?.Clear(); }
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                RefreshItemList();
            }
        }

        // ═══════════════════════════════════════════════════
        //  [2b] Center Panel — Editor
        // ═══════════════════════════════════════════════════

        private void DrawRightPanel()
        {
            if (_selectedItem == null)
            {
                GUILayout.FlexibleSpace();
                EditorLabel.Draw("Select an item from the left panel.", style: EditorTokens.EmptyStateStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            // Scrollable editor content
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            DrawBasicSection();
            EditorCard.Gap(EditorTokens.Pad);
            DrawPropertyOverrides();

            EditorGUILayout.EndScrollView();
        }

        // ── [2b-1] Basic Section ──

        private void DrawBasicSection()
        {
            EditorCard.Draw("Basic", () =>
            {
                // Template
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Template", LabelWidth);
                GUILayout.Space(EditorTokens.Pad);
                var newTemplate = EditorInput.ObjectField<PropertyTreeSO>(_selectedItem.Template);
                if (newTemplate != _selectedItem.Template)
                {
                    if (_selectedItem.Template != null && _overrideValues.Count > 0)
                    {
                        if (!EditorUtility.DisplayDialog("Template Change",
                            "Changing the template will clear all property overrides. Continue?", "Change", "Cancel"))
                            return;
                    }
                    _selectedItem.Template = newTemplate;
                    _hasChanges = true;
                    SelectItem(_selectedItem); // reload structure
                }
                EditorGUILayout.EndHorizontal();

                EditorCard.Gap(EditorTokens.Pad / 2);

                // Prefab
                EditorGUILayout.BeginHorizontal();
                EditorLabel.Draw("Prefab", LabelWidth);
                GUILayout.Space(EditorTokens.Pad);
                var newPrefab = EditorInput.ObjectField<GameObject>(_selectedItem.Prefab);
                if (newPrefab != _selectedItem.Prefab)
                {
                    _selectedItem.Prefab = newPrefab;
                    _hasChanges = true;
                    
                    EditorUtility.SetDirty(_selectedItem);
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        // ── Prefab Preview ──

        private void DrawPreviewPanel()
        {
            if (_selectedItem == null) return;

            // Find icon path from structure
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
                // Icon
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

                // Prefab
                EditorCard.Draw("Prefab", () =>
                {
                    if (_selectedItem.Prefab != null)
                    {
                        var previewRect = GUILayoutUtility.GetRect(
                            RightWidth - EditorTokens.PadCard * 3, 160f,
                            GUILayout.ExpandHeight(false));
                        previewRect.height = Mathf.Min(previewRect.width, 160f);

                        var preview = AssetPreview.GetAssetPreview(_selectedItem.Prefab);
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

        private Object ResolveIconAsset(string iconPath)
        {
            if (_structure == null || !_structure.TryGetValue(iconPath, out var def)
                || def is not AssetRefPropertyDefSO ad)
                return null;

            if (_overrideValues.TryGetValue(iconPath, out var rawGuid))
                return AssetRefPropertyDefSO.Load(rawGuid, ad.AssetTypeConstraint);

            return AssetRefPropertyDefSO.Load(ad.DefaultAssetGUID, ad.AssetTypeConstraint);
        }

        // ── [2b-2] Property Overrides — grouped by top-level folder ──

        private void DrawPropertyOverrides()
        {
            if (_structure == null || _structure.Count == 0)
            {
                EditorCard.Draw("Properties", () =>
                    EditorLabel.Draw("No properties. Select a Template first.", style: EditorTokens.EmptyStateStyle));
                return;
            }

            // Group by top-level folder, preserving PropertyTree order
            var groups = new Dictionary<string, List<(string path, PropertyDefSO def)>>();
            var groupOrder = new List<string>(); // preserves first-seen order
            foreach (var kv in _structure)
            {
                // No filter — all properties from PropertyTree are shown

                var slash = kv.Key.IndexOf('/');
                var folder = slash > 0 ? kv.Key.Substring(0, slash) : kv.Key;
                if (!groups.TryGetValue(folder, out var list))
                {
                    groups[folder] = list = new List<(string, PropertyDefSO)>();
                    groupOrder.Add(folder);
                }
                list.Add((kv.Key, kv.Value));
            }
            for (int i = 0; i < groupOrder.Count; i++)
            {
                if (i > 0) EditorCard.Gap(EditorTokens.Pad);

                var folderName = groupOrder[i];
                var props = groups[folderName];

                EditorCard.Draw(folderName, () =>
                {
                    foreach (var (path, def) in props)
                    {
                        // Display name: strip the top-level folder prefix, show remaining path
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

        private void DrawPropertyRow(string path, string displayName, PropertyDefSO def,
            bool isOverride, string rawValue)
        {
            EditorGUILayout.BeginHorizontal();

            // Label — gray for default, white for override
            var oldColor = GUI.color;
            GUI.color = isOverride ? Color.white : Color.gray;
            EditorLabel.Draw(displayName, LabelWidth, tooltip: $"{def.Type} — {def.Description}");
            GUI.color = oldColor;

            GUILayout.Space(EditorTokens.Pad);

            // Value control — dispatch by type
            switch (def)
            {
                case FloatPropertyDefSO fd:
                    DrawFloatRow(path, fd, isOverride, rawValue);
                    break;
                case IntPropertyDefSO id:
                    DrawIntRow(path, id, isOverride, rawValue);
                    break;
                case BoolPropertyDefSO bd:
                    DrawBoolRow(path, bd, isOverride, rawValue);
                    break;
                case StringPropertyDefSO sd:
                    DrawStringRow(path, sd, isOverride, rawValue);
                    break;
                case RdTagPropertyDefSO rd:
                    DrawRdTagRow(path, rd, isOverride, rawValue);
                    break;
                case RdTagListPropertyDefSO rl:
                    DrawRdTagListRow(path, rl, isOverride, rawValue);
                    break;
                case AssetRefPropertyDefSO ad:
                    DrawAssetRefRow(path, ad, isOverride, rawValue);
                    break;
                case AssetRefListPropertyDefSO al:
                    DrawAssetRefListRow(path, isOverride, rawValue);
                    break;
                case StructPropertyDefSO st:
                    DrawStructRow(path, st, isOverride, rawValue);
                    break;
                default:
                    EditorLabel.Draw(rawValue ?? GetDefaultRawValue(def));
                    break;
            }

            // Reset to default button (only for overridden values)
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

        private void DrawFloatRow(string path, FloatPropertyDefSO def, bool isOverride, string rawValue)
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
            if (Math.Abs(newMin - currentMin) > 0.0001f)
            {
                _minOverrides[path] = newMin;
                _hasChanges = true;
            }
            if (Math.Abs(newMax - currentMax) > 0.0001f)
            {
                _maxOverrides[path] = newMax;
                _hasChanges = true;
            }
        }

        private void DrawIntRow(string path, IntPropertyDefSO def, bool isOverride, string rawValue)
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
            if (newMin != currentMin)
            {
                _minOverrides[path] = newMin;
                _hasChanges = true;
            }
            if (newMax != currentMax)
            {
                _maxOverrides[path] = newMax;
                _hasChanges = true;
            }
        }

        private void DrawBoolRow(string path, BoolPropertyDefSO def, bool isOverride, string rawValue)
        {
            bool current = isOverride ? ParseBool(rawValue, def.DefaultValue) : def.DefaultValue;
            bool next = EditorInput.Toggle(current);
            if (next != current)
                SetOverride(path, next ? "true" : "false", def.DefaultValue, next);
        }

        private void DrawStringRow(string path, StringPropertyDefSO def, bool isOverride, string rawValue)
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

        private void DrawRdTagRow(string path, RdTagPropertyDefSO def, bool isOverride, string rawValue)
        {
            string current = isOverride ? rawValue : def.DefaultValue;
            EditorGUILayout.BeginHorizontal();
            string next = EditorInput.TextField(current ?? "");
            var tagRect = GUILayoutUtility.GetLastRect();
            if (EditorButton.Default("Tag", EditorButtonSize.Small, width: 35f))
            {
                TagPicker.Show(tagRect, currentFullTag: current, onSelected: tagDef =>
                {
                    if (tagDef != null)
                    {
                        SetOverride(path, tagDef.FullTag, def.DefaultValue, tagDef.FullTag);
                    }
                });
            }
            if (next != current)
                SetOverride(path, next, def.DefaultValue, next);
            EditorGUILayout.EndHorizontal();
        }

        // ── Shared Tag Chips Component ──

        private static void DrawTagChips(string[] tags, Action<string[]> onChanged)
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

        // ── RdTagList Row ──

        private void DrawRdTagListRow(string path, RdTagListPropertyDefSO def, bool isOverride, string rawValue)
        {
            string[] tags = isOverride ? ParseTagList(rawValue) : Array.Empty<string>();
            DrawTagChips(tags, newTags => SaveTagList(path, newTags));
        }

        private void SaveTagList(string path, string[] tags)
        {
            var json = BuildJsonArray(tags);
            SetOverride(path, json, Array.Empty<string>(), tags);
        }

        private void DrawAssetRefRow(string path, AssetRefPropertyDefSO def, bool isOverride, string rawValue)
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

        private void DrawAssetRefListRow(string path, bool isOverride, string rawValue)
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

        private void SetOverrideForAssetRefList(string path, string[] guids)
        {
            var json = BuildJsonArray(guids);
            SetOverride(path, json, Array.Empty<string>(), guids);
        }

        private void DrawStructRow(string path, StructPropertyDefSO def, bool isOverride, string rawValue)
        {
            var typeName = def.StructTypeName ?? "";
            if (typeName == "SlotDef" || typeName.EndsWith(".SlotDef"))
            {
                DrawSlotDefEditor(path, def, isOverride, rawValue);
                return;
            }

            // Unknown struct type — show type name, no raw text editing
            var oldColor = GUI.color;
            GUI.color = Color.gray;
            EditorLabel.Draw($"(Struct: {typeName})", style: EditorStyles.label);
            GUI.color = oldColor;
        }

        private void DrawSlotDefEditor(string path, StructPropertyDefSO def, bool isOverride, string rawValue)
        {
            // SlotId is the leaf node name from the PropertyTree
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
            catch { /* use default */ }
            slot.SlotId = slotId; // always use tree node name

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical();

            EditorCard.Gap(EditorTokens.Pad / 2);
            EditorLabel.Draw("Tags", 55f, tooltip: "此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空 = 接受所有。");
            var capturedSlotId2 = slotId;
            DrawTagChips(slot.AcceptTags ?? Array.Empty<string>(), newTags =>
            {
                slot.AcceptTags = newTags;
                slot.SlotId = capturedSlotId2;
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

        private void SaveSlotDefOverride(string path, SlotDef slot)
        {
            var json = JsonUtility.ToJson(slot);
            SetOverride(path, json, "{}", json);
        }

        // ═══════════════════════════════════════════════════
        //  [3] Status Bar
        // ═══════════════════════════════════════════════════

        private void DrawStatusBar()
        {
            EditorCard.Draw(() =>
            {
                if (_selectedItem == null)
                {
                    EditorLabel.Draw("No item selected.", style: EditorTokens.DimLabelStyle);
                    return;
                }

                var typeName = _selectedItem.GetType().Name;
                var templateName = _selectedItem.Template != null ? _selectedItem.Template.name : "none";
                var propCount = _structure?.Count ?? 0;
                var overrideCount = _overrideValues?.Count ?? 0;
                var summary = $"Type: {typeName} · Template: {templateName} · {propCount} props ({overrideCount} overrides)";
                EditorLabel.Draw(summary, style: EditorTokens.DimLabelStyle);
            });
        }

        // ═══════════════════════════════════════════════════
        //  Item Operations
        // ═══════════════════════════════════════════════════

        private void SelectItem(PropertyPresetSO item)
        {
            if (_hasChanges && _selectedItem != null && _selectedItem != item)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{_selectedItem.name}' before switching?", "Save", "Discard"))
                    Save();
                else
                    _hasChanges = false;
            }

            _selectedItem = item;
            _hasChanges = false;
            _overrideValues = new Dictionary<string, string>();
            _minOverrides = new Dictionary<string, float>();
            _maxOverrides = new Dictionary<string, float>();

            if (item?.Template != null)
            {
                _structure = ResolveStructureEditor(item.Template);
                _overrides = ParseOverrides(item.OverridesJson, _minOverrides, _maxOverrides);

                // Copy overrides to working state
                foreach (var (k, v) in _overrides)
                    _overrideValues[k] = v;
            }
            else
            {
                _structure = null;
                _overrides = new Dictionary<string, string>();
            }

            
            Repaint();
        }

        private void Save()
        {
            if (_selectedItem == null) return;

            // Build override entries — paths match PropertyTree structure
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

            // Sort by path for consistency
            entries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

            var container = new OverrideContainer { Overrides = entries };
            _selectedItem.OverridesJson = JsonUtility.ToJson(container, true);

            EditorUtility.SetDirty(_selectedItem);
            AssetDatabase.SaveAssets();

            // Sync saved state
            _overrides = new Dictionary<string, string>(_overrideValues);
            _hasChanges = false;
            Repaint();
        }

        private void CreateNewItem()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Item"), false, () => CreateAsset<ItemDefSO>());
            menu.AddItem(new GUIContent("Melee Weapon"), false, () => CreateAsset<MeleeWeaponSO>());
            menu.AddItem(new GUIContent("Ranged Weapon"), false, () => CreateAsset<RangedWeaponSO>());
            menu.ShowAsContext();
        }

        private void CreateAsset<T>() where T : PropertyPresetSO
        {
            var dir = "Assets/Data/Items";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                AssetDatabase.CreateFolder(string.Join("/", parts.Take(parts.Length - 1)), parts.Last());
            }

            var asset = CreateInstance<T>();
            asset.name = $"New{typeof(T).Name.Replace("SO", "")}";

            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{asset.name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DataLabelTools.EnsureBootLabel(path);
            RefreshItemList();
            SelectItem(asset);
        }

        private void DeleteSelectedItem()
        {
            if (_selectedItem == null) return;

            var path = AssetDatabase.GetAssetPath(_selectedItem);
            if (string.IsNullOrEmpty(path)) return;

            if (!EditorUtility.DisplayDialog("Delete Item",
                $"Delete '{_selectedItem.name}'?\nThis will delete the asset file permanently.",
                "Delete", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            _selectedItem = null;
            _structure = null;
            _overrideValues.Clear();
            _hasChanges = false;
            RefreshItemList();
        }

        // ═══════════════════════════════════════════════════
        //  Left Tree Data
        // ═══════════════════════════════════════════════════

        private void RefreshItemList()
        {
            var items = new List<PropertyPresetSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ItemDefSO>(path);
                if (asset != null) items.Add(asset);
            }

            BuildItemTree(items);
            _lastSearchFilter = _searchFilter;
            ApplyTreeFilter();
            Repaint();
        }

        private void BuildItemTree(List<PropertyPresetSO> items)
        {
            _treeRoots.Clear();

            // Group by Template name
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

            // Build folder nodes for each template group
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

            // Items without template go directly at root
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
        //  Overrides Helpers
        // ═══════════════════════════════════════════════════

        private static Dictionary<string, string> ParseOverrides(string json,
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
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var min))
                            minOverrides[entry.Path] = min;
                        if (!string.IsNullOrEmpty(entry.Max) && float.TryParse(entry.Max,
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
                            maxOverrides[entry.Path] = max;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemEditor] Parse overrides failed: {e.Message}");
            }

            return result;
        }

        // ═══════════════════════════════════════════════════
        //  Value Helpers
        // ═══════════════════════════════════════════════════

        private void SetOverride<T>(string path, string rawValue, T defaultValue, T currentValue)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, defaultValue))
            {
                // Value equals default — remove override
                _overrideValues.Remove(path);
            }
            else
            {
                _overrideValues[path] = rawValue;
            }
            _hasChanges = true;
        }

        private static string GetDefaultRawValue(PropertyDefSO def)
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

        private static float ParseFloat(string raw, float fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
        }

        private static int ParseInt(string raw, int fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return int.TryParse(raw, out var i) ? i : fallback;
        }

        private static bool ParseBool(string raw, bool fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            return bool.TryParse(raw, out var b) ? b : fallback;
        }

        private static string[] ParseTagList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try
            {
                var wrapper = JsonUtility.FromJson<TagListWrap>($"{{\"Items\":{raw}}}");
                return wrapper?.Items ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        private static string[] ParseGuidList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try
            {
                var wrapper = JsonUtility.FromJson<GuidListWrap>($"{{\"Items\":{raw}}}");
                return wrapper?.Items ?? Array.Empty<string>();
            }
            catch { return Array.Empty<string>(); }
        }

        private static string BuildJsonArray(string[] items)
        {
            if (items == null || items.Length == 0) return "[]";
            var escaped = items.Select(s => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            return "[" + string.Join(",", escaped) + "]";
        }

        private static string Truncate(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…";
        }

        // ═══════════════════════════════════════════════════
        // ── Editor Structure Resolver (bypasses GameService for editor use) ──

        private static Dictionary<string, PropertyDefSO> ResolveStructureEditor(PropertyTreeSO tree)
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
                        Debug.LogWarning($"[ItemEditor] Orphan: '{child.NodeId}' parent '{nodeId}' is a leaf, not a folder. Skipping.");
                        continue;
                    }
                    BuildStructurePaths(child.NodeId, merged, childrenByParent, path, result, defLookup);
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  Serializable Helpers
        // ═══════════════════════════════════════════════════

        [Serializable] private class OverrideEntry { public string Path, Value, Min, Max; }
        [Serializable] private class OverrideContainer { public List<OverrideEntry> Overrides = new(); }
        [Serializable] private class TagListWrap { public string[] Items; }
        [Serializable] private class SlotListWrap { public SlotDef[] Items; }
        [Serializable] private class GuidListWrap { public string[] Items; }
    }
}
