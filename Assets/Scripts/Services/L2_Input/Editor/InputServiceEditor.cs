using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.Services.Input.Editor
{
    [CustomEditor(typeof(InputService))]
    public class InputServiceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var channelsProp = serializedObject.FindProperty("eventChannels");
            var assetProp = serializedObject.FindProperty("inputActionAsset");

            var channelCount = channelsProp?.arraySize ?? 0;
            var channelNames = new HashSet<string>();
            for (int i = 0; i < channelCount; i++)
            {
                var ch = channelsProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (ch != null) channelNames.Add(ch.name);
            }

            var actionCount = 0;
            var matchedCount = 0;
            var unmatch = new List<string>();
            var asset = assetProp?.objectReferenceValue as InputActionAsset;
            if (asset != null)
            {
                foreach (var map in asset.actionMaps)
                foreach (var action in map.actions)
                {
                    actionCount++;
                    if (channelNames.Contains(action.name))
                        matchedCount++;
                    else
                        unmatch.Add(action.name);
                }
            }

            EditorGUILayout.Space(4);
            if (actionCount == 0)
            {
                EditorGUILayout.HelpBox("Assign an InputActionAsset.", MessageType.Info);
            }
            else if (unmatch.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Matched {matchedCount}/{actionCount} actions. Unmatched:\n• {string.Join("\n• ", unmatch)}",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"All {actionCount} actions matched. {channelCount} channel(s) total.",
                    MessageType.Info);
            }
        }
    }
}
