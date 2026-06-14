#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 搜索栏组合组件。
    /// 内部组成: EditorLabel + EditorInput.TextFieldWithClear
    /// </summary>
    public static class EditorSearchBar
    {
        public static string Draw(string current, float labelWidth = 45f)
        {
            var h = EditorGUIUtility.singleLineHeight;
            EditorGUILayout.BeginHorizontal(GUILayout.Height(h));

            EditorLabel.Draw("Search", labelWidth, trailingGap: EditorTokens.PadTight);
            var result = EditorInput.TextFieldWithClear(current);

            EditorGUILayout.EndHorizontal();
            return result;
        }
    }
}
#endif
