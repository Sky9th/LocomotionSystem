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
        // Cached styles for DrawHeaderCard — lazy-init for EditorStyles availability
        private static GUIStyle _headerTitleStyle;
        private static GUIStyle HeaderTitleStyle => _headerTitleStyle ??= new GUIStyle(EditorStyles.largeLabel);
        private static GUIStyle _headerSubStyle;
        private static GUIStyle HeaderSubStyle => _headerSubStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.gray } };
        private static readonly Color HeaderSaveColor = new(0.4f, 0.8f, 0.4f);

        // ── Unity 设计令牌色 ──
        // highlight-background: #2C5D87 → 选中高亮
        // ── 共用颜色常量 ──
        public static readonly Color ColorGreen = HeaderSaveColor;                  // 0.4, 0.8, 0.4 — 保存/Save
        public static readonly Color ColorGreenDark = new(0.4f, 0.7f, 0.4f);       // 0.4, 0.7, 0.4 — 创建/Create/Add
        public static readonly Color ColorBlue = new(0.298f, 0.494f, 1.0f);        // #4C7EFF — Unity link-text
        public static readonly Color ColorRed = new(0.827f, 0.133f, 0.133f);       // #D32222 — Unity error-text
        public static readonly Color ColorButtonText = new(0.933f, 0.933f, 0.933f); // #EEEEEE — Unity button-text

        // ── 缓存样式 ──
        private static GUIStyle _greyPlaceholder;
        public static GUIStyle GreyPlaceholder => _greyPlaceholder ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 12, normal = { textColor = Color.grey } };

        /// <summary>
        /// 标准编辑器 Header 卡片。
        /// [Title] [Subtitle(右对齐, gray)] [FlexibleSpace] [Save*(可选)]
        /// </summary>
        public static void DrawHeaderCard(float pad, string title, string subtitle,
            bool hasChanges = false, Action onSave = null)
        {
            EditorCard.Draw(pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, HeaderTitleStyle);

                var subWidth = HeaderSubStyle.CalcSize(new GUIContent(subtitle ?? "")).x;
                EditorGUILayout.LabelField(subtitle, HeaderSubStyle, GUILayout.Width(subWidth));

                GUILayout.FlexibleSpace();

                if (onSave != null)
                {
                    var oldBg = GUI.backgroundColor;
                    if (hasChanges) GUI.backgroundColor = HeaderSaveColor;
                    EditorGUI.BeginDisabledGroup(!hasChanges);
                    var label = hasChanges ? "Save *" : "Save";
                    if (GUILayout.Button(label, GUILayout.Height(24), GUILayout.Width(80)))
                        onSave();
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = oldBg;
                }

                EditorGUILayout.EndHorizontal();
            });
        }

        /// <summary>
        /// 带 Tooltip 的 LabelField。自动从 ScriptableObject 的 [Tooltip] 属性读取。
        /// </summary>
        public static void LabelWithTooltip(ScriptableObject so, string fieldName,
            float width, string overrideLabel = null)
        {
            var label = overrideLabel ?? fieldName;
            var tooltip = GetFieldTooltip(so, fieldName);
            EditorGUILayout.LabelField(
                new GUIContent(label, tooltip),
                GUILayout.Width(width));
        }

        /// <summary>
        /// 带 Tooltip 的 LabelField。直接传入 tooltip 字符串（跳过 SerializedObject 查找）。
        /// 用于 FormItem 等已缓存 tooltip 的场景。
        /// </summary>
        public static void LabelWithTooltip(string label, string tooltip, float width)
        {
            EditorGUILayout.LabelField(
                new GUIContent(label, tooltip),
                GUILayout.Width(width));
        }

        /// <summary>
        /// 标准搜索行：Label("Search", width) + TextField + 清除按钮("x")。
        /// 搜索行，委托给 EditorSearchBar 组件。
        /// </summary>
        public static string DrawSearchRow(string current, float labelWidth = 45f)
        {
            return EditorSearchBar.Draw(current, labelWidth);
        }

        /// <summary>
        /// 通用筛选标签栏，委托给 EditorButtonGroup 单选模式。
        /// </summary>
        public static T DrawFilterTabBar<T>(T current, T[] tabs, string[] labels)
            where T : struct, Enum
        {
            return EditorButtonGroup.Draw(current, tabs, labels);
        }

        /// <summary>
        /// 标准删除按钮（GUILayout 版）。红色背景，miniButton，宽20，"x"。
        /// 返回 true 表示被点击。
        /// </summary>
        public static bool DeleteButton()
            => EditorButton.Draw("x", EditorButtonStyle.Danger);

        public static bool DeleteButton(Rect rect)
            => EditorButton.Draw(rect, "x", EditorButtonStyle.Danger);

        private static string GetFieldTooltip(ScriptableObject so, string fieldName)
        {
            if (so == null) return null;
            using var serialized = new SerializedObject(so);
            var prop = serialized.FindProperty(fieldName);
            return prop?.tooltip;
        }
    }
}
#endif
