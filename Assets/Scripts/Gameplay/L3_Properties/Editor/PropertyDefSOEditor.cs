using RedDust.Core.RdTag;
using UnityEditor;
using UnityEngine;

namespace RedDust.Gameplay.Properties.Editor
{
    /// <summary>
    /// Custom Inspector for PropertyDefSO. Shows only the field groups
    /// relevant to the selected PropertyType.
    /// </summary>
    [CustomEditor(typeof(PropertyDefSO))]
    public class PropertyDefSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var def = (PropertyDefSO)target;
            serializedObject.Update();

            // Identity — always visible
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsDeprecated"));

            EditorGUILayout.Space();

            // Type-specific fields
            switch (def.Type)
            {
                case PropertyType.Float:
                    if (def is FloatPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("Float", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Min"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Max"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"));
                    }
                    break;

                case PropertyType.Int:
                    if (def is IntPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("Int", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Min"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("Max"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"));
                    }
                    break;

                case PropertyType.Bool:
                    if (def is BoolPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("Bool", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"));
                    }
                    break;

                case PropertyType.String:
                    if (def is StringPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("String", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"));
                    }
                    break;

                case PropertyType.RdTag:
                    if (def is RdTagPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("RdTag", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"));
                    }
                    break;

                case PropertyType.RdTagList:
                    EditorGUILayout.HelpBox("RdTag List — no default value (always empty array).", MessageType.None);
                    break;

                case PropertyType.AssetRef:
                    if (def is AssetRefPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("AssetRef", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultAssetGUID"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("AssetTypeConstraint"));
                    }
                    break;

                case PropertyType.AssetRefList:
                    if (def is AssetRefListPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("AssetRefList", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("AssetTypeConstraint"));
                    }
                    break;

                case PropertyType.Struct:
                    if (def is StructPropertyDefSO)
                    {
                        EditorGUILayout.LabelField("Struct", EditorStyles.boldLabel);
                        var structTypeProp = serializedObject.FindProperty("StructTypeName");
                        structTypeProp.stringValue = PropertyStructScanner.DrawDropdown(structTypeProp.stringValue);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultJson"));
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
