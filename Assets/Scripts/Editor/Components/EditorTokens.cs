#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// EUI 设计令牌——所有组件共用的布局、字号、内边距、颜色、样式常量。
    /// </summary>
    public static class EditorTokens
    {
        // ── 布局 ──
        public const float Pad = 6f;
        public const float PadTight = 3f;   // Pad / 2
        public const float PadCard = 10f;         // Card 内边距
        public const float PadSectionHeader = 10f;  // Card title 下方间距

        // ── 字号 ──
        public const int FontSm = 11;
        public const int FontBase = 12;
        public const int FontLg = 14;
        public const int FontSectionHeader = 15;   // Card.Draw title 专用

        // ── 内边距 ──
        public static readonly RectOffset PaddingSmall  = new(6, 6, 1, 1);
        public static readonly RectOffset PaddingMedium = new(10, 10, 3, 3);
        public static readonly RectOffset PaddingLarge  = new(14, 14, 5, 5);

        // ── 控件尺寸（L/M/S 三档，对齐 design-tokens.md §4）──
        public const float SizeSm = 16f;   // Small   — 紧凑按钮/图标/tag
        public const float SizeMd = 20f;   // Default — 标准按钮/输入框
        public const float SizeLg = 26f;   // Large   — 强调按钮/Section header

        // ── 颜色 ──
        public static readonly Color ColorGreen      = new(0.4f, 0.8f, 0.4f);       // 保存
        public static readonly Color ColorGreenDark  = new(0.4f, 0.7f, 0.4f);       // 创建
        public static readonly Color ColorBlue       = new(0.298f, 0.494f, 1.0f);   // #4C7EFF 链接
        public static readonly Color ColorRed        = new(0.827f, 0.133f, 0.133f); // #D32222 错误/Danger
        public static readonly Color ColorButtonText = new(0.933f, 0.933f, 0.933f); // #EEEEEE 按钮文字
        public static readonly Color ColorSelected   = new(0.173f, 0.365f, 0.529f); // #2C5D87 选中高亮

        // ── 语义色（Element UI 体系，暗色适配）──
        public static readonly Color ColorPrimary  = ColorBlue;                     // #4C7EFF 主操作
        public static readonly Color ColorSuccess  = new(0.404f, 0.761f, 0.227f);  // #67C23A 成功
        public static readonly Color ColorWarning  = new(0.902f, 0.635f, 0.235f);  // #E6A23C 警告
        public static readonly Color ColorDanger   = ColorRed;                      // #D32222 危险
        public static readonly Color ColorInfo     = new(0.659f, 0.698f, 0.749f);  // #A8B2BF 信息
        public static readonly Color ColorDivider  = new(0.137f, 0.137f, 0.137f, 0.3f);  // 分隔线
        public static readonly Color ColorDim      = new(0.137f, 0.137f, 0.137f);         // 卡片淡化
        public static readonly Color ColorResultOk = new(0.2f, 0.7f, 0.2f);               // 导入成功
        public static readonly Color ColorResultErr = new(0.9f, 0.3f, 0.2f);              // 导入失败

        // ── 结构色（组件外观）──
        /// <summary>Card 背景色（Element UI + Unity 皮肤融合）。</summary>
        public static Color ColorCardBg => EditorGUIUtility.isProSkin
            ? new Color(0.16f, 0.16f, 0.16f, 1f)   // dark: #292929
            : new Color(0.98f, 0.98f, 0.98f, 1f);   // light: #FAFAFA

        /// <summary>Card 边框色（仅 borderless=false 时显示）。</summary>
        public static Color ColorCardBorder => EditorGUIUtility.isProSkin
            ? new Color(0.24f, 0.24f, 0.24f, 1f)    // dark: #3D3D3D
            : new Color(0.86f, 0.86f, 0.86f, 1f);   // light: #DBDBDB

        // ═══════════════════════════════════════════════════
        // ── 语义样式（不从 Unity 原生样式克隆）──
        // ═══════════════════════════════════════════════════

        private static Color _editorTextColor;
        /// <summary>当前 Editor 皮肤的文字颜色。</summary>
        public static Color EditorTextColor =>
            _editorTextColor.a > 0f ? _editorTextColor
            : (_editorTextColor = EditorStyles.label.normal.textColor);

        // ── 标题 ──

        private static GUIStyle _headerTitleStyle;
        public static GUIStyle HeaderTitleStyle => _headerTitleStyle ??= new GUIStyle()
        {
            fontSize = FontLg,
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorTextColor },
        };

        // ── 正文 ──

        private static GUIStyle _breadcrumbStyle;
        /// <summary>窗口右侧面包屑/"L3_Ability · Editor"。</summary>
        public static GUIStyle BreadcrumbStyle => _breadcrumbStyle ??= new GUIStyle()
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = FontBase,
            normal = { textColor = Color.gray },
        };

        private static GUIStyle _emptyStateStyle;
        /// <summary>空状态/占位灰字。</summary>
        public static GUIStyle EmptyStateStyle => _emptyStateStyle ??= new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = FontBase,
            normal = { textColor = Color.grey },
        };

        private static GUIStyle _dimLabelStyle;
        /// <summary>次级说明文字（小型灰字）。</summary>
        public static GUIStyle DimLabelStyle => _dimLabelStyle ??= new GUIStyle()
        {
            fontSize = FontSm,
            normal = { textColor = Color.grey },
        };

        private static GUIStyle _errorLabelStyle;
        /// <summary>错误/必填提示。</summary>
        public static GUIStyle ErrorLabelStyle => _errorLabelStyle ??= new GUIStyle()
        {
            fontSize = FontBase,
            normal = { textColor = ColorDanger },
        };

        private static GUIStyle _successLabelStyle;
        /// <summary>成功/通过提示。</summary>
        public static GUIStyle SuccessLabelStyle => _successLabelStyle ??= new GUIStyle()
        {
            fontSize = FontBase,
            normal = { textColor = ColorSuccess },
        };

        private static GUIStyle _richLabelStyle;
        /// <summary>富文本标签（richText + 自动换行）。</summary>
        public static GUIStyle RichLabelStyle => _richLabelStyle ??= new GUIStyle()
        {
            richText = true,
            wordWrap = true,
            fontSize = FontBase,
        };
    }
}
#endif
