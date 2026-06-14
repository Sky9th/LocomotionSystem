#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 搜索栏组件。参考 Element UI el-input search 模式 + FormItem 单行布局。
    /// 结构: [Label(w:固定)] [TextField(flex)] [ClearBtn(w:20)]
    /// </summary>
    public static class EditorSearchBar
    {
        /// <summary>
        /// 绘制搜索栏。当前有输入时清除按钮可点击，无输入时禁用。
        /// </summary>
        /// <returns>当前文本（可能被清除按钮置空）。</returns>
        public static string Draw(string current, float labelWidth = 45f)
        {
            EditorGUILayout.BeginHorizontal(
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.LabelField("Search", EditorStyles.label,
                GUILayout.Width(labelWidth),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            var next = EditorGUILayout.TextField(current,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            var hasText = !string.IsNullOrEmpty(next);
            var wasEnabled = GUI.enabled;
            GUI.enabled = hasText;
            if (EditorButton.Draw("x", EditorButtonStyle.Default,
                EditorButtonSize.Small, width: 20))
            {
                next = "";
                GUI.FocusControl(null);
            }
            GUI.enabled = wasEnabled;

            EditorGUILayout.EndHorizontal();
            return next;
        }
    }
}
#endif
