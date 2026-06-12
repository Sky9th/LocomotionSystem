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
            EditorCard.Draw(Pad, () =>
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
            EditorCard.Draw(Pad, () =>
            {
                var s = EditorUIUtility.DrawSearchRow(current, labelWidth: 45f);
                if (s != current) onChanged(s);
            });
        }

        public static void DrawCreateCard(Action onCreateNew)
        {
            EditorCard.Draw(Pad, () =>
            {
                if (EditorButton.Draw("+ Create New", EditorButtonStyle.Primary,
                        EditorButtonSize.Large, 160f))
                    onCreateNew?.Invoke();
            });
        }
    }
}
#endif
