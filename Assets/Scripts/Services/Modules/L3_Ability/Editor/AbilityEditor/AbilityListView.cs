#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
                var next = EditorUIUtility.DrawFilterTabBar(current,
                    new[] { AbilityTypeFilter.All, AbilityTypeFilter.Active, AbilityTypeFilter.Passive },
                    new[] { "All", "Active", "Passive" });
                if (!EqualityComparer<AbilityTypeFilter>.Default.Equals(next, current))
                    onChanged(next);
            });
        }

        public static void DrawSearchCard(string current, Action<string> onChanged)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                var s = EditorUIUtility.DrawSearchRow(current, labelWidth: 45f);
                if (s != current) onChanged(s);
            });
        }

        public static void DrawCreateCard(Action onCreateNew)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                GUI.backgroundColor = EditorUIUtility.ColorGreen;
                if (GUILayout.Button("+ Create New", GUILayout.Width(160), GUILayout.Height(24)))
                    onCreateNew?.Invoke();
                GUI.backgroundColor = Color.white;
            });
        }
    }
}
#endif
