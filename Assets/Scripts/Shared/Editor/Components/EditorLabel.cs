#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 纯 Label 渲染组件——只画标签，不管布局和 Input。
    /// </summary>
    public static class EditorLabel
    {
        public static void Draw(string text, float width,
            string tooltip = null, float trailingGap = 0f,
            GUIStyle style = null)
        {
            var h = EditorGUIUtility.singleLineHeight;
            var guiContent = string.IsNullOrEmpty(tooltip)
                ? new GUIContent(text)
                : new GUIContent(text, tooltip);
            EditorGUILayout.LabelField(guiContent,
                style ?? EditorStyles.label,
                GUILayout.Width(width), GUILayout.Height(h));
            if (trailingGap > 0f) GUILayout.Space(trailingGap);
        }
    }
}
#endif
