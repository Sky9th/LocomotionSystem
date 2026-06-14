#if UNITY_EDITOR
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
    /// <summary>
    /// EUI 设计令牌——所有组件共用的布局、字号、内边距、颜色常量。
    /// </summary>
    public static class EditorTokens
    {
        // ── 布局 ──
        public const float Pad = 6f;
        public const float PadTight = 3f;   // Pad / 2

        // ── 字号 ──
        public const int FontSm = 11;
        public const int FontBase = 12;
        public const int FontLg = 14;

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
    }
}
#endif
