#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// IMGUI 布局工具。统一卡片模式：
    ///
    ///   BeginVertical(helpBox)
    ///     Space(pad)             ← 上内边距
    ///     BeginHorizontal
    ///       Space(pad)           ← 左内边距
    ///       BeginVertical
    ///         [content]
    ///       EndVertical
    ///       Space(pad)           ← 右内边距
    ///     EndHorizontal
    ///     Space(pad)             ← 下内边距
    ///   EndVertical
    ///
    ///   卡片间用 GUILayout.Space(pad) 作为下外边距。
    /// </summary>
    public static class EditorUIUtility
    {
        // ── 缓存样式 ──
        private static GUIStyle _greyPlaceholder;
        public static GUIStyle GreyPlaceholder => _greyPlaceholder ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 12, normal = { textColor = Color.grey } };

    }
}
#endif
