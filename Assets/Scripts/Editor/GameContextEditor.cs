#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector that surfaces GameContext runtime registries so designers
/// can inspect which services and snapshot structs are currently cached.
/// </summary>
[CustomEditor(typeof(GameContext))]
public class GameContextEditor : Editor
{
    private bool showServices = true;
    private bool showSnapshots = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var context = (GameContext)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Overview", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Is Initialized", context.IsInitialized);
            EditorGUILayout.IntField("Service Count", context.RegisteredServiceCount);
            EditorGUILayout.IntField("Snapshot Count", context.SnapshotCount);
        }

        EditorGUILayout.Space();
        DrawTypeList(ref showServices,
            $"Service Registry ({context.RegisteredServiceCount})",
            context.RegisteredServiceTypes);

        EditorGUILayout.Space();
        DrawTypeList(ref showSnapshots,
            $"Snapshot Cache ({context.SnapshotCount})",
            context.SnapshotStructTypes);
    }

    private void DrawTypeList(ref bool foldout, string label, IEnumerable<Type> types)
    {
        foldout = EditorGUILayout.Foldout(foldout, label, true);
        if (!foldout)
        {
            return;
        }

        EditorGUI.indentLevel++;
        bool hasEntries = false;
        foreach (var type in types)
        {
            hasEntries = true;
            EditorGUILayout.LabelField(type.FullName);
        }

        if (!hasEntries)
        {
            EditorGUILayout.LabelField("None");
        }

        EditorGUI.indentLevel--;
    }
}
#endif
