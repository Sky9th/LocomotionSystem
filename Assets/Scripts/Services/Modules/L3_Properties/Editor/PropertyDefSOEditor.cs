using UnityEditor;
using UnityEngine;

namespace RedDust.Properties.Editor
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
                    EditorGUILayout.LabelField("Float", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Min"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Max"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultFloat"));
                    break;

                case PropertyType.Int:
                    EditorGUILayout.LabelField("Int", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MinInt"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxInt"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultInt"));
                    break;

                case PropertyType.Bool:
                    EditorGUILayout.LabelField("Bool", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultBool"));
                    break;

                case PropertyType.String:
                    EditorGUILayout.LabelField("String", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultString"));
                    break;

                case PropertyType.GameplayTag:
                case PropertyType.GameplayTagList:
                    EditorGUILayout.HelpBox("No default value — tags are empty unless overridden.", MessageType.None);
                    break;

                case PropertyType.AssetRef:
                    EditorGUILayout.LabelField("AssetRef", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultAssetGUID"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AssetTypeConstraint"));
                    break;

                case PropertyType.AssetRefList:
                    EditorGUILayout.HelpBox("No default value — array is empty unless overridden.", MessageType.None);
                    break;

                case PropertyType.Struct:
                    EditorGUILayout.LabelField("Struct", EditorStyles.boldLabel);
                    var structTypeProp = serializedObject.FindProperty("StructTypeName");
                    structTypeProp.stringValue = PropertyStructScanner.DrawDropdown(structTypeProp.stringValue);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultStructJson"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
