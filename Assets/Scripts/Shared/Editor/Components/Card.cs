#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 编辑器卡片容器。
    /// </summary>
    public static class EditorCard
    {
        public static void Draw(Action drawContent)
        {
            EditorGUILayout.BeginVertical(CardStyle);
            drawContent();
            EditorGUILayout.EndVertical();
        }

        public static void Draw(string title, Action drawBody)
        {
            Draw(() =>
            {
                Header(title);
                drawBody();
            });
        }

        // ═══════════════════════════════════════════════════
        // 间距
        // ═══════════════════════════════════════════════════

        public static void Gap(float px) => GUILayout.Space(px);
        public static void GapTight() => GUILayout.Space(EditorTokens.PadTight);

        // ═══════════════════════════════════════════════════
        // Style
        // ═══════════════════════════════════════════════════

        private static GUIStyle _cardStyle;
        private static GUIStyle CardStyle => _cardStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10,10,10,10),
            margin = new RectOffset(),
        };

        // ═══════════════════════════════════════════════════
        // Internal
        // ═══════════════════════════════════════════════════

        private static void Header(string title)
        {
            GUILayout.Label(title, SectionTitleStyle,
                GUILayout.Height(EditorGUIUtility.singleLineHeight + 2));
            EditorCard.Gap(EditorTokens.PadSectionHeader);
        }

        internal static GUIStyle _sectionTitleStyle;
        internal static GUIStyle SectionTitleStyle => _sectionTitleStyle ??= new GUIStyle()
        {
            fontSize = EditorTokens.FontSectionHeader,
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorStyles.boldLabel.normal.textColor },
        };
    }
}
#endif
