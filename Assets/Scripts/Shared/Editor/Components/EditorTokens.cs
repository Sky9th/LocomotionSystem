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
        public static readonly RectOffset PaddingSmall  = new(4, 4, 1, 1);
        public static readonly RectOffset PaddingMedium = new(14, 14, 5, 5);
        public static readonly RectOffset PaddingLarge  = new(18, 18, 7, 7);

        // ── 颜色 ──
        public static readonly Color ColorGreen      = new(0.4f, 0.8f, 0.4f);       // 保存/Primary
        public static readonly Color ColorGreenDark  = new(0.4f, 0.7f, 0.4f);       // 创建/Success
        public static readonly Color ColorBlue       = new(0.298f, 0.494f, 1.0f);   // #4C7EFF 链接
        public static readonly Color ColorRed        = new(0.827f, 0.133f, 0.133f); // #D32222 错误/Danger
        public static readonly Color ColorButtonText = new(0.933f, 0.933f, 0.933f); // #EEEEEE 按钮文字
        public static readonly Color ColorSelected   = new(0.173f, 0.365f, 0.529f); // #2C5D87 选中高亮
    }
}
#endif
