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
        /// <summary>
        /// 绘制一张标准卡片。四内边距 = pad。
        /// </summary>
        public static void DrawCard(float pad, Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(pad);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad);
            EditorGUILayout.BeginVertical();

            drawContent();

            EditorGUILayout.EndVertical();
            GUILayout.Space(pad);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(pad);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 卡片间间距。值应与卡片内边距一致。
        /// </summary>
        public static void CardGap(float pad)
        {
            GUILayout.Space(pad);
        }

        // Cached styles for DrawHeaderCard — lazy-init for EditorStyles availability
        private static GUIStyle _headerTitleStyle;
        private static GUIStyle HeaderTitleStyle => _headerTitleStyle ??= new GUIStyle(EditorStyles.largeLabel);
        private static GUIStyle _headerSubStyle;
        private static GUIStyle HeaderSubStyle => _headerSubStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleRight, normal = { textColor = Color.gray } };
        private static readonly Color HeaderSaveColor = new(0.4f, 0.8f, 0.4f);

        // ── 共用颜色常量 ──
        public static readonly Color ColorGreen = HeaderSaveColor;                  // 0.4, 0.8, 0.4 — 保存/Save
        public static readonly Color ColorGreenDark = new(0.4f, 0.7f, 0.4f);       // 0.4, 0.7, 0.4 — 创建/Create/Add
        public static readonly Color ColorBlue = new(0.3f, 0.6f, 0.9f);            // 选中筛选标签
        public static readonly Color ColorRed = new(0.9f, 0.3f, 0.3f);             // 删除/移除

        // ── 缓存样式 ──
        private static GUIStyle _greyPlaceholder;
        public static GUIStyle GreyPlaceholder => _greyPlaceholder ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 13, normal = { textColor = Color.grey } };

        /// <summary>
        /// 标准编辑器 Header 卡片。
        /// [Title] [Subtitle(右对齐, gray)] [FlexibleSpace] [Save*(可选)]
        /// </summary>
        public static void DrawHeaderCard(float pad, string title, string subtitle,
            bool hasChanges = false, Action onSave = null)
        {
            DrawCard(pad, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, HeaderTitleStyle);

                var subWidth = (subtitle?.Length ?? 10) * 10;
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
        /// 仅绘制行内元素，不包裹卡片。返回新文本，调用方负责赋值。
        /// </summary>
        public static string DrawSearchRow(string current, float labelWidth = 45f)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search", EditorStyles.label, GUILayout.Width(labelWidth));
            var s = EditorGUILayout.TextField(current, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(s) && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                s = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            return s;
        }

        /// <summary>
        /// 通用筛选标签栏。水平排列 miniButtonMid，选中项蓝色高亮。
        /// </summary>
        public static T DrawFilterTabBar<T>(T current, T[] tabs, string[] labels)
            where T : Enum
        {
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < tabs.Length; i++)
            {
                var isSelected = EqualityComparer<T>.Default.Equals(current, tabs[i]);
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = isSelected ? ColorBlue : Color.white;
                if (GUILayout.Button(labels[i], EditorStyles.miniButtonMid, GUILayout.Height(20)))
                {
                    GUI.backgroundColor = oldBg;
                    EditorGUILayout.EndHorizontal();
                    return tabs[i];
                }
                GUI.backgroundColor = oldBg;
            }
            EditorGUILayout.EndHorizontal();
            return current;
        }

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
