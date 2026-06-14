#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// 编辑器卡片组件。参考 Unity 设计令牌。
    /// </summary>
    public static class EditorCard
    {
        // highlight-background-hover: rgba(255,255,255,0.06)
        private static readonly Color HighlightHover = new(1f, 1f, 1f, 0.06f);

        // ═══════════════════════════════════════════════════
        // 标准卡片
        // ═══════════════════════════════════════════════════

        public static void Draw(float pad, Action drawContent)
        {
            Impl(pad, drawContent);
        }

        /// <summary>带选中高亮。#2C5D87 蓝色背景。</summary>
        public static void Draw(float pad, Action drawContent, bool selected)
        {
            var oldBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = EditorTokens.ColorSelected;
            Impl(pad, drawContent);
            GUI.backgroundColor = oldBg;
        }

        /// <summary>带标题。等价于 DrawCardHeader + Draw。</summary>
        public static void Draw(float pad, string title, Action drawBody)
        {
            Impl(pad, () =>
            {
                Header(title);
                drawBody();
            });
        }

        // ═══════════════════════════════════════════════════
        // 变体
        // ═══════════════════════════════════════════════════

        /// <summary>轻量卡片（半内边距，边框淡化）。嵌套时减轻视觉重量。</summary>
        public static void DrawLight(float pad, Action drawContent)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.137f, 0.137f, 0.137f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = oldColor;
            GUILayout.Space(pad / 2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad);
            EditorGUILayout.BeginVertical();
            drawContent();
            EditorGUILayout.EndVertical();
            GUILayout.Space(pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(pad / 2);
            EditorGUILayout.EndVertical();
        }

        /// <summary>折叠卡片。标题栏 + ▸/▾箭头 + 可展开内容。</summary>
        public static void DrawFoldout(float pad, string title,
            ref bool folded, Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(pad / 2);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad);

            var arrow = folded ? "▸" : "▾";
            var arrowStyle = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            if (GUILayout.Button(arrow, arrowStyle, GUILayout.Width(16), GUILayout.Height(18)))
                folded = !folded;

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { alignment = TextAnchor.MiddleLeft };
            if (GUILayout.Button(title, titleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(18)))
                folded = !folded;

            GUILayout.Space(pad);
            EditorGUILayout.EndHorizontal();

            if (!folded)
            {
                GUILayout.Space(pad / 2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(pad);
                EditorGUILayout.BeginVertical();
                drawContent();
                EditorGUILayout.EndVertical();
                GUILayout.Space(pad);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(pad);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>列表项。扁平内边距 + 选中高亮 + 点击回调。</summary>
        public static void DrawItem(float pad, Action drawContent,
            bool selected = false, Action onClick = null)
        {
            var oldBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = EditorTokens.ColorSelected;

            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldBg;

            GUILayout.Space(pad / 3);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(pad);
            EditorGUILayout.BeginVertical();
            drawContent();
            EditorGUILayout.EndVertical();
            GUILayout.Space(pad);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(pad / 3);
            EditorGUILayout.EndVertical();

            if (onClick != null && Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition))
            {
                onClick();
                Event.current.Use();
            }
        }

        // ═══════════════════════════════════════════════════
        // 间距 & 标题
        // ═══════════════════════════════════════════════════

        public static void Gap(float pad) => GUILayout.Space(pad);

        /// <summary>紧凑间距 (3px)。关联紧密的同级卡片。</summary>
        public static void GapTight() => GUILayout.Space(3f);

        /// <summary>统一区域标题。boldLabel + 下方间距。</summary>
        private static void Header(string title)
        {
            EditorGUILayout.BeginHorizontal(
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        // ═══════════════════════════════════════════════════
        // Internal
        // ═══════════════════════════════════════════════════

        private static void Impl(float pad, Action drawContent)
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
        // ═══════════════════════════════════════════════════
        // Header
        // ═══════════════════════════════════════════════════

        private static GUIStyle _headerTitleStyle;
        private static GUIStyle HeaderTitleStyle => _headerTitleStyle ??= new GUIStyle(EditorStyles.largeLabel);
        private static GUIStyle _headerSubStyle;
        private static GUIStyle HeaderSubStyle => _headerSubStyle ??= new GUIStyle(EditorStyles.label)
            { alignment = TextAnchor.MiddleRight, fontSize = 11, normal = { textColor = Color.gray } };

        /// <summary>Header 卡片：[Title][Subtitle][Flexible][drawRight Slot]</summary>
        public static void DrawCardHeader(string title, string subtitle,
            Action drawRight = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, HeaderTitleStyle);
            var subWidth = HeaderSubStyle.CalcSize(new GUIContent(subtitle ?? "")).x;
            EditorGUILayout.LabelField(subtitle, HeaderSubStyle, GUILayout.Width(subWidth));
            GUILayout.FlexibleSpace();
            drawRight?.Invoke();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
