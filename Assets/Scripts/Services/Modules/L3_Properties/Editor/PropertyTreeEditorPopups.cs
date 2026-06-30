using System;
using System.Collections.Generic;
using System.Linq;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
{
    /// <summary>
    /// Popup dialogs for the Property Tree Editor: NewTree + CreateDef.
    /// Extracted from PropertyTreeEditorWindow to keep the main window focused.
    /// </summary>
    public static class PropertyTreeEditorPopups
    {
        // ── NewTree ──

        public static class NewTreeDialog
        {
            public static void Show(Action<string, PropertyTreeSO> cb, PropertyTreeSO parent = null)
            {
                var w = ScriptableObject.CreateInstance<NewTreePopup>();
                w._cb = cb;
                w._parent = parent;
                w.minSize = new Vector2(300, 100);
                w.maxSize = new Vector2(400, 140);
                w.ShowUtility();
            }

            private class NewTreePopup : EditorWindow
            {
                public Action<string, PropertyTreeSO> _cb;
                private string _name = "";
                public PropertyTreeSO _parent;

                private void OnGUI()
                {
                    EditorGUILayout.LabelField("New PropertyTree", EditorStyles.boldLabel);
                    _name = EditorGUILayout.TextField("Name", _name);
                    _parent = (PropertyTreeSO)EditorGUILayout.ObjectField("InheritsFrom", _parent, typeof(PropertyTreeSO), false);
                    EditorGUILayout.BeginHorizontal();
                    var hasName = !string.IsNullOrWhiteSpace(_name);
                    if (EditorButton.Draw("Create", EditorButtonType.Success, EditorButtonSize.Small, enabled: hasName))
                    { _cb?.Invoke(_name, _parent); Close(); }
                    if (EditorButton.Draw("Cancel", size: EditorButtonSize.Small)) Close();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // ── DefDetail (read-only popup) ──

        public static class DefDetailPopup
        {
            private const float LabelWidth = 100f;
            private static readonly Color ColorLabel = new(0.55f, 0.55f, 0.55f);
            private static readonly Color ColorValue = Color.white;

            private static GUIStyle _labelStyle;
            private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ColorLabel },
                fontSize = EditorStyles.label.fontSize,
            };
            private static GUIStyle _valueStyle;
            private static GUIStyle ValueStyle => _valueStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ColorValue },
                fontSize = EditorStyles.label.fontSize,
                wordWrap = true,
            };
            private static GUIStyle _headerStyle;
            private static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
            };

            public static void Show(PropertyDefSO def)
            {
                var w = ScriptableObject.CreateInstance<DefDetailWindow>();
                w._def = def;
                w.minSize = new Vector2(380, 240);
                w.maxSize = new Vector2(520, 600);
                w.titleContent = new GUIContent($"Def: {def.Id}");
                w.ShowUtility();
            }

            private class DefDetailWindow : EditorWindow
            {
                public PropertyDefSO _def;
                private Vector2 _scroll;

                private void OnGUI()
                {
                    if (_def == null) { Close(); return; }
                    var pad = 6f;
                    GUILayout.Space(pad);
                    EditorGUILayout.BeginHorizontal(); GUILayout.Space(pad);
                    EditorGUILayout.BeginVertical();

                    _scroll = EditorGUILayout.BeginScrollView(_scroll);

                    // Header card
                    DrawCard(pad, () =>
                    {
                        EditorGUILayout.LabelField(_def.Id, HeaderStyle);
                        GUILayout.Space(2);
                        DrawFieldRow("Type", _def.Type.ToString());
                        if (_def.IsDeprecated)
                            DrawFieldRow("Status", "⚠ Deprecated");
                    });

                    GUILayout.Space(pad);

                    // Description — always shown, even if empty
                    DrawCard(pad, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Description", LabelStyle, GUILayout.Width(LabelWidth));
                        var desc = string.IsNullOrEmpty(_def.Description) ? "(none)" : _def.Description;
                        EditorGUILayout.LabelField(desc, ValueStyle, GUILayout.ExpandWidth(true));
                        EditorGUILayout.EndHorizontal();
                    });
                    GUILayout.Space(pad);

                    // Type-specific constraints
                    DrawCard(pad, () => DrawTypeFields(pad));

                    if (_def.IsDeprecated)
                    {
                        GUILayout.Space(pad);
                        var warnStyle = new GUIStyle(EditorStyles.helpBox)
                        {
                            normal = { background = MakeColorTex(new Color(0.5f, 0.3f, 0.1f, 0.4f)) },
                            padding = new RectOffset(8, 8, 6, 6),
                        };
                        EditorGUILayout.LabelField("⚠ This definition is marked as Deprecated. It should not be used in new trees.", warnStyle);
                    }

                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(pad);
                    if (EditorButton.Draw("Close", size: EditorButtonSize.Small)) Close();

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(pad); EditorGUILayout.EndHorizontal();
                    GUILayout.Space(pad);
                }

                private void DrawTypeFields(float pad)
                {
                    EditorGUILayout.LabelField("Constraints", EditorStyles.boldLabel);
                    GUILayout.Space(4);

                    switch (_def.Type)
                    {
                        case PropertyType.Float:
                            var fd = (FloatPropertyDefSO)_def;
                            DrawFieldRow("Min", fd.Min.ToString("G"));
                            DrawFieldRow("Max", fd.Max.ToString("G"));
                            DrawFieldRow("Default", fd.DefaultValue.ToString("G"));
                            break;
                        case PropertyType.Int:
                            var id = (IntPropertyDefSO)_def;
                            DrawFieldRow("Min", id.Min.ToString());
                            DrawFieldRow("Max", id.Max.ToString());
                            DrawFieldRow("Default", id.DefaultValue.ToString());
                            break;
                        case PropertyType.Bool:
                            DrawFieldRow("Default", ((BoolPropertyDefSO)_def).DefaultValue ? "true" : "false");
                            break;
                        case PropertyType.String:
                            var sv = ((StringPropertyDefSO)_def).DefaultValue;
                            DrawFieldRow("Default", string.IsNullOrEmpty(sv) ? "(empty)" : sv);
                            break;
                        case PropertyType.rTag:
                            var tv = ((RTagPropertyDefSO)_def).DefaultValue;
                            DrawFieldRow("Default", string.IsNullOrEmpty(tv) ? "(empty)" : tv);
                            break;
                        case PropertyType.rTagList:
                            EditorGUILayout.LabelField("rTag array reference. No numeric constraints.", ValueStyle);
                            break;
                        case PropertyType.AssetRef:
                            var ac = ((AssetRefPropertyDefSO)_def).AssetTypeConstraint;
                            DrawFieldRow("Asset Type", string.IsNullOrEmpty(ac) ? "(any)" : ac);
                            break;
                        case PropertyType.AssetRefList:
                            var alc = ((AssetRefListPropertyDefSO)_def).AssetTypeConstraint;
                            DrawFieldRow("Asset Type", string.IsNullOrEmpty(alc) ? "(any)" : alc);
                            break;
                        case PropertyType.Struct:
                        {
                            var current = ((StructPropertyDefSO)_def).StructTypeName;
                            DrawFieldRow("Struct Type", string.IsNullOrEmpty(current) ? "(unset)" : current);
                            break;
                        }
                    }
                }

                private void DrawFieldRow(string label, string value)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label, LabelStyle, GUILayout.Width(LabelWidth));
                    EditorGUILayout.LabelField(value, ValueStyle, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                }

                private static void DrawCard(float pad, Action content)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(pad);
                    EditorGUILayout.BeginHorizontal(); GUILayout.Space(pad);
                    EditorGUILayout.BeginVertical();
                    content();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(pad); EditorGUILayout.EndHorizontal();
                    GUILayout.Space(pad);
                    EditorGUILayout.EndVertical();
                }

                private static Texture2D MakeColorTex(Color color)
                {
                    var tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, color);
                    tex.Apply();
                    return tex;
                }
            }
        }

        // ── CreateDef ──

        public static class CreateDefDialog
        {
            public static void Show(Action<PropertyDefSO> onCreated)
            {
                var w = ScriptableObject.CreateInstance<CreateDefPopup>();
                w._onCreated = onCreated;
                w.minSize = new Vector2(360, 320);
                w.maxSize = new Vector2(480, 600);
                w.ShowUtility();
            }

            private class CreateDefPopup : EditorWindow
            {
                public Action<PropertyDefSO> _onCreated;
                private string _id = "";
                private string _description = "";
                private PropertyType _type = PropertyType.Float;
                private bool _isDeprecated;

                // Float
                private float _min;
                private float _max = 100f;
                private float _defaultFloat = 100f;

                // Int
                private int _minInt;
                private int _maxInt = 100;
                private int _defaultInt;

                // Bool
                private bool _defaultBool;

                // String
                private string _defaultString = "";

                // AssetRef
                private string _assetTypeConstraint = "";

                // Struct
                private string _structTypeName = "";
                private string _defaultStructJson = "";

                private void OnGUI()
                {
                    var pad = 6f;
                    GUILayout.Space(pad);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(pad);
                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.LabelField("Create Property Definition", EditorStyles.boldLabel);
                    GUILayout.Space(pad);

                    _id = EditorGUILayout.TextField("Id", _id);
                    _type = (PropertyType)EditorGUILayout.EnumPopup("Type", _type);
                    GUILayout.Space(pad);
                    _description = EditorGUILayout.TextField("Description", _description, GUILayout.Height(40));
                    GUILayout.Space(pad);

                    // Type-specific fields
                    switch (_type)
                    {
                        case PropertyType.Float:
                            _min = EditorGUILayout.FloatField("Min", _min);
                            _max = EditorGUILayout.FloatField("Max", _max);
                            _defaultFloat = EditorGUILayout.FloatField("Default", _defaultFloat);
                            break;
                        case PropertyType.Int:
                            _minInt = EditorGUILayout.IntField("Min", _minInt);
                            _maxInt = EditorGUILayout.IntField("Max", _maxInt);
                            _defaultInt = EditorGUILayout.IntField("Default", _defaultInt);
                            break;
                        case PropertyType.Bool:
                            _defaultBool = EditorGUILayout.Toggle("Default", _defaultBool);
                            break;
                        case PropertyType.String:
                            _defaultString = EditorGUILayout.TextField("Default", _defaultString);
                            break;
                        case PropertyType.AssetRef:
                            _assetTypeConstraint = EditorGUILayout.TextField("Asset Type Constraint", _assetTypeConstraint);
                            break;
                        case PropertyType.Struct:
                            _structTypeName = PropertyStructScanner.DrawDropdown(_structTypeName);
                            _defaultStructJson = EditorGUILayout.TextField("Default JSON", _defaultStructJson);
                            break;
                    }

                    GUILayout.Space(pad);
                    _isDeprecated = EditorGUILayout.Toggle("Deprecated", _isDeprecated);
                    GUILayout.Space(pad);

                    EditorGUILayout.BeginHorizontal();
                    bool valid = !string.IsNullOrWhiteSpace(_id);
                    if (EditorButton.Draw("Create", EditorButtonType.Success, EditorButtonSize.Small, enabled: valid))
                    {
                        var existing = AssetDatabase.LoadAssetAtPath<PropertyDefSO>($"Assets/Data/Properties/Definitions/{_id}.asset");
                        if (existing != null)
                        {
                            EditorUtility.DisplayDialog("Duplicate ID",
                                $"A PropertyDefinition with Id '{_id}' already exists.", "OK");
                        }
                        else
                        {
                            var def = CreateDef();
                            _onCreated?.Invoke(def);
                            Close();
                        }
                    }
                    if (EditorButton.Draw("Cancel", size: EditorButtonSize.Small)) Close();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(pad);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(pad);
                }

                private PropertyDefSO CreateDef()
                {
                    var dir = "Assets/Data/Properties/Definitions";
                    if (!AssetDatabase.IsValidFolder(dir))
                    {
                        var parts = dir.Split('/');
                        AssetDatabase.CreateFolder(string.Join("/", parts.Take(parts.Length - 1)), parts.Last());
                    }
                    var def = PropertyDefSO.Create(_type);
                    def.Id = _id;
                    def.Description = _description;
                    def.IsDeprecated = _isDeprecated;

                    if (def is FloatPropertyDefSO fd) { fd.Min = _min; fd.Max = _max; fd.DefaultValue = _defaultFloat; }
                    else if (def is IntPropertyDefSO id) { id.Min = _minInt; id.Max = _maxInt; id.DefaultValue = _defaultInt; }
                    else if (def is BoolPropertyDefSO bd) { bd.DefaultValue = _defaultBool; }
                    else if (def is StringPropertyDefSO sd) { sd.DefaultValue = _defaultString; }
                    else if (def is RTagPropertyDefSO rd) { rd.DefaultValue = _defaultString; }
                    else if (def is AssetRefPropertyDefSO ad) { ad.AssetTypeConstraint = _assetTypeConstraint; }
                    else if (def is AssetRefListPropertyDefSO ald) { ald.AssetTypeConstraint = _assetTypeConstraint; }
                    else if (def is StructPropertyDefSO std) { std.StructTypeName = _structTypeName; std.DefaultJson = _defaultStructJson ?? "[]"; }

                    AssetDatabase.CreateAsset(def, $"{dir}/{_id}.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return def;
                }            }
        }
    }
}
