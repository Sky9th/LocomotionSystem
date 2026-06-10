using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Color ColorCreate = new(0.4f, 0.8f, 0.4f);

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
                    GUI.backgroundColor = ColorCreate;
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_name));
                    if (GUILayout.Button("Create", GUILayout.Height(24))) { _cb?.Invoke(_name, _parent); Close(); }
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Cancel", GUILayout.Height(24))) Close();
                    EditorGUILayout.EndHorizontal();
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
                w.minSize = new Vector2(320, 200);
                w.maxSize = new Vector2(420, 400);
                w.ShowUtility();
            }

            private class CreateDefPopup : EditorWindow
            {
                public Action<PropertyDefSO> _onCreated;
                private string _id = "";
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
                    }

                    GUILayout.Space(pad);
                    _isDeprecated = EditorGUILayout.Toggle("Deprecated", _isDeprecated);
                    GUILayout.Space(pad);

                    EditorGUILayout.BeginHorizontal();
                    bool valid = !string.IsNullOrWhiteSpace(_id);
                    GUI.backgroundColor = ColorCreate;
                    EditorGUI.BeginDisabledGroup(!valid);
                    if (GUILayout.Button("Create", GUILayout.Height(24)))
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
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Cancel", GUILayout.Height(24))) Close();
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
                    var def = CreateInstance<PropertyDefSO>();
                    def.Id = _id;
                    def.Type = _type;
                    def.IsDeprecated = _isDeprecated;
                    def.Min = _min;
                    def.Max = _max;
                    def.DefaultFloat = _defaultFloat;
                    def.MinInt = _minInt;
                    def.MaxInt = _maxInt;
                    def.DefaultInt = _defaultInt;
                    def.DefaultBool = _defaultBool;
                    def.DefaultString = _defaultString;
                    def.AssetTypeConstraint = _assetTypeConstraint;
                    AssetDatabase.CreateAsset(def, $"{dir}/{_id}.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return def;
                }
            }
        }
    }
}
