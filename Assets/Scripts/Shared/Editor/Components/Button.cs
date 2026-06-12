#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    public enum EditorButtonStyle { Default, Primary, Success, Danger }

    /// <summary>参考 Element UI: default/medium/small/mini。</summary>
    public enum EditorButtonSize { Auto, Small, Medium, Large }

    /// <summary>
    /// 统一编辑器按钮。参考 Element UI el-button：
    /// 大小由 padding + fontSize 决定（非固定 height），有背景色时白字。
    /// </summary>
    public static class EditorButton
    {
        // ── Element UI 风格尺寸 ──
        // default: fontSize 14, padding 12px 20px
        // medium:  fontSize 14, padding 10px 20px
        // small:   fontSize 12, padding 9px 15px
        // mini:    fontSize 12, padding 7px 15px

        private static GUIStyle _styleMedium;
        private static GUIStyle StyleMedium => _styleMedium ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            padding = new RectOffset(16, 16, 5, 5),
        };

        private static GUIStyle _styleLarge;
        private static GUIStyle StyleLarge => _styleLarge ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            padding = new RectOffset(22, 22, 7, 7),
        };

        // ═══════════════════════════════════════════════════

        public static bool Draw(string text,
            EditorButtonStyle style = EditorButtonStyle.Default,
            EditorButtonSize size = EditorButtonSize.Auto,
            float? width = null,
            bool enabled = true)
        {
            var wasEnabled = GUI.enabled;
            GUI.enabled = enabled;

            var oldBg = GUI.backgroundColor;
            var oldContent = GUI.contentColor;
            ApplyStyle(style);

            bool clicked;
            if (size == EditorButtonSize.Small)
                clicked = DrawMiniButton(text, width);
            else if (size == EditorButtonSize.Large)
                clicked = DrawWithStyle(text, StyleLarge, width);
            else if (size == EditorButtonSize.Medium)
                clicked = DrawWithStyle(text, StyleMedium, width);
            else if (width.HasValue)
                clicked = GUILayout.Button(text, GUILayout.Width(width.Value));
            else
                clicked = GUILayout.Button(text);

            GUI.contentColor = oldContent;
            GUI.backgroundColor = oldBg;
            GUI.enabled = wasEnabled;
            return clicked;
        }

        /// <summary>筛选标签。</summary>
        public static bool DrawTab(string text, bool isSelected)
        {
            var oldBg = GUI.backgroundColor;
            var oldContent = GUI.contentColor;
            GUI.backgroundColor = isSelected ? EditorUIUtility.ColorBlue : Color.white;
            if (isSelected) GUI.contentColor = EditorUIUtility.ColorButtonText;
            var clicked = GUILayout.Button(text, EditorStyles.miniButtonMid);
            GUI.contentColor = oldContent;
            GUI.backgroundColor = oldBg;
            return clicked;
        }

        /// <summary>手动 Rect。</summary>
        public static bool Draw(Rect rect, string text,
            EditorButtonStyle style = EditorButtonStyle.Default)
        {
            var oldBg = GUI.backgroundColor;
            var oldContent = GUI.contentColor;
            ApplyStyle(style);
            var clicked = GUI.Button(rect, text, EditorStyles.miniButton);
            GUI.contentColor = oldContent;
            GUI.backgroundColor = oldBg;
            return clicked;
        }

        // ═══════════════════════════════════════════════════

        private static bool DrawMiniButton(string text, float? width)
            => width.HasValue
                ? GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Width(width.Value))
                : GUILayout.Button(text, EditorStyles.miniButton);

        private static bool DrawWithStyle(string text, GUIStyle style, float? width)
            => width.HasValue
                ? GUILayout.Button(text, style, GUILayout.Width(width.Value))
                : GUILayout.Button(text, style);

        private static void ApplyStyle(EditorButtonStyle style)
        {
            switch (style)
            {
                case EditorButtonStyle.Primary:
                    GUI.backgroundColor = EditorUIUtility.ColorGreen;
                    GUI.contentColor = EditorUIUtility.ColorButtonText;
                    break;
                case EditorButtonStyle.Success:
                    GUI.backgroundColor = EditorUIUtility.ColorGreenDark;
                    GUI.contentColor = EditorUIUtility.ColorButtonText;
                    break;
                case EditorButtonStyle.Danger:
                    GUI.backgroundColor = EditorUIUtility.ColorRed;
                    GUI.contentColor = EditorUIUtility.ColorButtonText;
                    break;
            }
        }
    }
}
#endif
