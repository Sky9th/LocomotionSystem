#if UNITY_EDITOR
using System;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    public static class AbilityListView
    {
        private const float Pad = 6f;

        public static void DrawFilterCard(AbilityTypeFilter current, Action<AbilityTypeFilter> onChanged)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                var tabs = new[] { AbilityTypeFilter.All, AbilityTypeFilter.Active, AbilityTypeFilter.Passive };
                var labels = new[] { "All", "Active", "Passive" };
                for (var i = 0; i < tabs.Length; i++)
                {
                    var isSelected = current == tabs[i];
                    GUI.backgroundColor = isSelected ? new Color(0.3f, 0.6f, 0.9f) : Color.white;
                    if (GUILayout.Button(labels[i], EditorStyles.miniButtonLeft, GUILayout.Height(20)))
                        onChanged(tabs[i]);
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        public static void DrawSearchCard(string current, Action<string> onChanged)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(45));
                var s = EditorGUILayout.TextField(current, GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(s)
                    && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    s = "";
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();
                if (s != current) onChanged(s);
            });
        }

        public static void DrawCreateCard(Action onCreateNew)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("+ Create New", GUILayout.Width(160), GUILayout.Height(24)))
                    onCreateNew?.Invoke();
                GUI.backgroundColor = Color.white;
            });
        }
    }
}
#endif
