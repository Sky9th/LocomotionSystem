#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 纯 Label 渲染组件——只画标签，不管布局和 Input。
    /// 默认样式基于 EditorStyles.label，左右 padding/margin 清零。
    /// </summary>
    public static class EditorLabel
    {
        private static GUIStyle _defaultStyle;
        public static GUIStyle DefaultStyle => _defaultStyle ??= CreateDefaultStyle();

        private static GUIStyle CreateDefaultStyle()
        {
            return new GUIStyle()
            {
                fontSize = EditorTokens.FontBase,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorTokens.EditorTextColor },
                padding = new RectOffset(3, 3, 3, 3),
                margin = new RectOffset(1, 0, 0, 0),
            };
        }

        /// <summary>固定宽度标签。</summary>
        public static void Draw(string text, float width,
            string tooltip = null, float trailingGap = 0f,
            GUIStyle style = null)
        {
            var guiContent = string.IsNullOrEmpty(tooltip)
                ? new GUIContent(text)
                : new GUIContent(text, tooltip);
            EditorGUILayout.LabelField(guiContent,
                style ?? DefaultStyle,
                GUILayout.Width(width));
            if (trailingGap > 0f) GUILayout.Space(trailingGap);
        }

        /// <summary>自适应宽度标签（使用可用空间）。</summary>
        public static void Draw(string text, string tooltip = null, GUIStyle style = null)
        {
            var guiContent = string.IsNullOrEmpty(tooltip)
                ? new GUIContent(text)
                : new GUIContent(text, tooltip);
            EditorGUILayout.LabelField(guiContent,
                style ?? DefaultStyle,
                GUILayout.ExpandWidth(true));
        }
    }
}
#endif
