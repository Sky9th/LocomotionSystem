#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 分隔线组件。画一条细线 + 可选标题。
    /// </summary>
    public static class EditorDivider
    {
        public static void Draw(string title = null)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label,
                GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            rect.y += 7f;
            EditorGUI.DrawRect(rect, EditorTokens.ColorDivider);
            if (!string.IsNullOrEmpty(title))
            {
                var labelRect = new Rect(rect.x, rect.y - 2f, rect.width, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, title, EditorStyles.miniLabel);
            }
            GUILayout.Space(EditorTokens.Pad);
        }
    }
}
#endif
