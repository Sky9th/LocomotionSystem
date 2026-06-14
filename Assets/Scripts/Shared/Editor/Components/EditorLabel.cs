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
            var s = new GUIStyle(EditorStyles.label);
            s.padding = new RectOffset(0, 0, s.padding.top + 3, s.padding.bottom + 3);
            s.margin = new RectOffset(1, 0, s.margin.top, s.margin.bottom);
            return s;
        }

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
    }
}
#endif
