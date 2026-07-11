#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
	public enum EditorButtonType { Default, Primary, Success, Warning, Danger, Info }

	/// <summary>参考 Element UI: default/medium/small/mini。</summary>
	public enum EditorButtonSize { Auto, Small, Medium, Large }

	/// <summary>
	/// 统一编辑器按钮。参考 Element UI el-button：
	/// 大小由 padding + fontSize 决定（非固定 height），有背景色时白字。
	/// </summary>
	public static class EditorButton
	{
		// ── 颜色令牌（Element UI）──
		private static readonly Color PrimaryColor = EditorTokens.ColorPrimary;  // #4C7EFF
		private static readonly Color SuccessColor = EditorTokens.ColorSuccess;  // #67C23A
		private static readonly Color WarningColor = EditorTokens.ColorWarning;  // #E6A23C
		private static readonly Color DangerColor  = EditorTokens.ColorDanger;   // #D32222
		private static readonly Color InfoColor    = EditorTokens.ColorInfo;     // #A8B2BF

		private static readonly Dictionary<(EditorButtonType, EditorButtonSize), GUIStyle> _styleCache = new();

		// ═══════════════════════════════════════════════════
		// 公开 Style 工厂
		// ═══════════════════════════════════════════════════

		/// <summary>获取指定 type+size 的 GUIStyle。Default 类型无自定义背景。</summary>
		public static GUIStyle GetStyle(EditorButtonType type, EditorButtonSize size)
		{
			var key = (type, size);

			// 有色 Small：原生 miniButton，不加任何覆盖
			if (type != EditorButtonType.Default && size == EditorButtonSize.Small)
				return EditorStyles.miniButton;

			if (_styleCache.TryGetValue(key, out var cached)) return cached;

			var style = new GUIStyle(EditorStyles.miniButton)
			{
				fontSize = GetFontSize(size),
				fixedHeight = GetFixedHeight(size),
				padding = GetPadding(size),
				margin = new RectOffset(0, 0, EditorStyles.miniButton.margin.top, EditorStyles.miniButton.margin.bottom),
				overflow = new RectOffset(),
			};

			_styleCache[key] = style;
			return style;
		}

		// ═══════════════════════════════════════════════════
		// 类型入口（高频）
		// ═══════════════════════════════════════════════════

		public static bool Default(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Default, size, width, null, null, enabled);

		public static bool Primary(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Primary, size, width, null, null, enabled);

		public static bool Success(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Success, size, width, null, null, enabled);

		public static bool Warning(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Warning, size, width, null, null, enabled);

		public static bool Danger(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Danger, size, width, null, null, enabled);

		public static bool Info(string text,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null, bool enabled = true)
			=> Draw(text, EditorButtonType.Info, size, width, null, null, enabled);

		// ═══════════════════════════════════════════════════
		// 通用入口（低频，标准内最大自由）
		// ═══════════════════════════════════════════════════

		public static bool Draw(string text,
			EditorButtonType type = EditorButtonType.Default,
			EditorButtonSize size = EditorButtonSize.Medium,
			float? width = null,
			float? height = null,
			string tooltip = null,
			bool enabled = true)
		{
			var style = GetStyle(type, size);
			var content = string.IsNullOrEmpty(tooltip)
				? new GUIContent(text)
				: new GUIContent(text, tooltip);

			var options = new List<GUILayoutOption>();
			if (width.HasValue)  options.Add(GUILayout.Width(width.Value));
			if (height.HasValue) options.Add(GUILayout.Height(height.Value));

			return DrawButton(type, () => GUILayout.Button(content, style, options.ToArray()), enabled);
		}

		/// <summary>手动 Rect 版本。</summary>
		public static bool Draw(Rect rect, string text,
			EditorButtonType type = EditorButtonType.Default,
			string tooltip = null)
		{
			var content = string.IsNullOrEmpty(tooltip)
				? new GUIContent(text)
				: new GUIContent(text, tooltip);
			return DrawButton(type, () => GUI.Button(rect, content, GetStyle(type, EditorButtonSize.Small)));
		}

		// ═══════════════════════════════════════════════════
		// Internal: 颜色 + enabled 统一包装
		// ═══════════════════════════════════════════════════

		// Delete
		public static bool Delete() => Danger("✕", EditorButtonSize.Small, width: EditorTokens.SizeMd);

		private static bool DrawButton(EditorButtonType type, Func<bool> draw, bool enabled = true)
		{
			var wasEnabled = GUI.enabled;
			GUI.enabled = enabled;

			var oldBg = GUI.backgroundColor;
			var oldContent = GUI.contentColor;
			if (type != EditorButtonType.Default)
			{
				GUI.backgroundColor = GetColor(type);
				GUI.contentColor = Color.white;
			}

			bool clicked = draw();

			GUI.backgroundColor = oldBg;
			GUI.contentColor = oldContent;
			GUI.enabled = wasEnabled;
			return clicked;
		}


		// ═══════════════════════════════════════════════════
		// Internal: 颜色工具
		// ═══════════════════════════════════════════════════

		private static Color GetColor(EditorButtonType type) => type switch
		{
			EditorButtonType.Primary => PrimaryColor,
			EditorButtonType.Success => SuccessColor,
			EditorButtonType.Warning => WarningColor,
			EditorButtonType.Danger  => DangerColor,
			EditorButtonType.Info    => InfoColor,
			_ => Color.gray,
		};

		private static int GetFontSize(EditorButtonSize size) => size switch
		{
			EditorButtonSize.Small  => EditorTokens.FontSm,
			EditorButtonSize.Large  => EditorTokens.FontLg,
			_                       => EditorTokens.FontBase,  // Medium / Auto
		};

		private static RectOffset GetPadding(EditorButtonSize size) => size switch
		{
			EditorButtonSize.Small  => EditorTokens.PaddingSmall,
			EditorButtonSize.Large  => EditorTokens.PaddingLarge,
			_                       => EditorTokens.PaddingMedium,   // Medium / Auto
		};

		private static float GetFixedHeight(EditorButtonSize size) => size switch
		{
			EditorButtonSize.Medium => 24f,
			EditorButtonSize.Large  => 28f,
			_                       => 0f,  // Small / Auto: miniButton 自适应
		};

	}
}
#endif
