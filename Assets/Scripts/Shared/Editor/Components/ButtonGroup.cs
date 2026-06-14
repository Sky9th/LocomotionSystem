#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// Element UI el-button-group 的 Unity IMGUI 实现。
    /// 多个按钮横向紧密排列，共享边框，仅一个高亮选中。
    /// </summary>
    public static class EditorButtonGroup
    {
        /// <summary>
        /// 单选按钮组（Enum 模式）。每个值对应一个按钮，选中项以 Primary 高亮。
        /// </summary>
        /// <returns>点击后返回新值，未点击返回原值。</returns>
        public static T Draw<T>(T current, T[] values, string[] labels,
            EditorButtonSize size = EditorButtonSize.Small)
            where T : struct, Enum
        {
            EditorGUILayout.BeginHorizontal();
            var result = current;
            for (var i = 0; i < values.Length; i++)
            {
                var isSelected = EqualityComparer<T>.Default.Equals(current, values[i]);
                var style = isSelected ? EditorButtonStyle.Primary : EditorButtonStyle.Default;
                if (EditorButton.Draw(labels[i], style, size))
                    result = values[i];
            }
            EditorGUILayout.EndHorizontal();
            return result;
        }

        /// <summary>
        /// 单选按钮组（索引模式）。返回被点击的按钮索引，未点击返回原值。
        /// </summary>
        public static int Draw(string[] labels, int selectedIndex = -1,
            EditorButtonSize size = EditorButtonSize.Small)
        {
            EditorGUILayout.BeginHorizontal();
            var result = selectedIndex;
            for (var i = 0; i < labels.Length; i++)
            {
                var style = i == selectedIndex ? EditorButtonStyle.Primary : EditorButtonStyle.Default;
                if (EditorButton.Draw(labels[i], style, size))
                    result = i;
            }
            EditorGUILayout.EndHorizontal();
            return result;
        }
    }
}
#endif
