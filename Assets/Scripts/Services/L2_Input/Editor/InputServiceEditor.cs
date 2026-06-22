using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput.Editor
{
    [CustomEditor(typeof(InputService))]
    public class InputServiceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var service = (InputService)target;
            var events = service.InputEvents;

            // ── Always: scan project SOs vs inputEvents[] ──
            var coveredIds = new HashSet<string>();
            foreach (var evt in events)
            {
                if (evt == null) continue;
                var actionRef = evt.InputActionRef;
                if (actionRef != null && actionRef.action != null)
                    coveredIds.Add(actionRef.action.id.ToString());
            }

            var missingSOs = new List<string>();
            var guids = AssetDatabase.FindAssets("t:InputEventBase");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<InputEventBase>(path);
                if (so == null) continue;

                var actionRef = so.InputActionRef;
                var actionId = actionRef != null && actionRef.action != null
                    ? actionRef.action.id.ToString() : null;
                var actionName = actionRef != null && actionRef.action != null
                    ? actionRef.action.name : "(no action)";

                if (actionId != null && !coveredIds.Contains(actionId))
                    missingSOs.Add(string.Format("{0}  →  {1}  ({2})", so.name, actionName, path));
            }

            if (missingSOs.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "SOs not in inputEvents (" + missingSOs.Count + "):\n• " +
                    string.Join("\n• ", missingSOs),
                    MessageType.Warning);
            }

            // ── If InputActionAsset assigned: compare asset actions ↔ inputEvents[] ──
            var asset = service.InputActionAsset;
            if (asset != null)
            {
                var missingActions = new List<string>();
                foreach (var map in asset.actionMaps)
                {
                    foreach (var action in map.actions)
                    {
                        if (!coveredIds.Contains(action.id.ToString()))
                            missingActions.Add(action.name + " (" + map.name + ")");
                    }
                }

                EditorGUILayout.Space(4);
                if (missingActions.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        "Actions without SO (" + missingActions.Count + "):\n• " +
                        string.Join("\n• ", missingActions),
                        MessageType.Warning);
                }
                else if (missingSOs.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "All " + coveredIds.Count + " actions covered.",
                        MessageType.Info);
                }
            }
            else if (missingSOs.Count == 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "All " + coveredIds.Count + " project SO(s) covered. Assign InputActionAsset for action-level check.",
                    MessageType.Info);
            }
        }
    }
}
