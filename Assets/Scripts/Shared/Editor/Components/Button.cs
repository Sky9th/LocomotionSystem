#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDust.Shared.EditorUI
{
	public enum EditorButtonType { Default, Primary, Success, Warning, Danger, Info }

	public enum EditorButtonStyle { Default, Primary, Success, Danger } // ← 旧枚举，迁移完成后删除

	/// <summary>参考 Element UI: default/medium/small/mini。</summary>
	public enum EditorButtonSize { Auto, Small, Medium, Large }

	/// <summary>
	/// 统一编辑器按钮。参考 Element UI el-button：
	/// 大小由 padding + fontSize 决定（非固定 height），有背景色时白字。
	/// </summary>
	public static class EditorButton
	{
		// ── 颜色令牌（Element UI）──
		private static readonly Color PrimaryColor = new(0.251f, 0.620f, 1.000f);  // #409EFF
		private static readonly Color SuccessColor = new(0.404f, 0.761f, 0.227f);  // #67C23A
		private static readonly Color WarningColor = new(0.902f, 0.635f, 0.235f);  // #E6A23C
		private static readonly Color DangerColor  = EditorTokens.ColorRed;      // #D32222
		private static readonly Color InfoColor    = new(0.565f, 0.576f, 0.600f);  // #909399

		// ── 纹理 + Style 缓存 ──
		private static readonly Dictionary<(EditorButtonType, EditorButtonSize), GUIStyle> _styleCache = new();
		private static readonly Dictionary<Color, Texture2D> _texCache = new();

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
				padding = GetPadding(size),
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
			EditorButtonType type,
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
		public static bool Delete() => Danger("✕", EditorButtonSize.Small, width: 20);
		public static bool Delete(Rect rect) => Draw(rect, "✕", EditorButtonType.Danger);

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
		// Internal: 纹理工厂
		// ═══════════════════════════════════════════════════

		private static Texture2D GetSolidTexture(Color color)
		{
			if (_texCache.TryGetValue(color, out var tex)) return tex;
			tex = new Texture2D(2, 2)
			{
				hideFlags = HideFlags.HideAndDontSave,
			};
			var pixels = new Color[4];
			for (int i = 0; i < 4; i++) pixels[i] = color;
			tex.SetPixels(pixels);
			tex.Apply();
			_texCache[color] = tex;
			return tex;
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

		private static Color Lighten(Color c, float amount)
		{
			return new Color(
				Mathf.Clamp01(c.r + amount),
				Mathf.Clamp01(c.g + amount),
				Mathf.Clamp01(c.b + amount),
				c.a);
		}

		private static Color Darken(Color c, float amount)
		{
			return new Color(
				Mathf.Clamp01(c.r - amount),
				Mathf.Clamp01(c.g - amount),
				Mathf.Clamp01(c.b - amount),
				c.a);
		}

		// ═══════════════════════════════════════════════════
		// ↓↓↓ 旧 API — 迁移完成后删除 ↓↓↓
		// ═══════════════════════════════════════════════════

		// ── Element UI 风格尺寸 ──
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

		[Obsolete("Use EditorButton.Default/Primary/... or EditorButton.Draw(type:...)")]
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

		[Obsolete("Use EditorButton.Draw(Rect, text, type:...)")]
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
					GUI.backgroundColor = EditorTokens.ColorGreen;
					GUI.contentColor = EditorTokens.ColorButtonText;
					break;
				case EditorButtonStyle.Success:
					GUI.backgroundColor = EditorTokens.ColorGreenDark;
					GUI.contentColor = EditorTokens.ColorButtonText;
					break;
				case EditorButtonStyle.Danger:
					GUI.backgroundColor = EditorTokens.ColorRed;
					GUI.contentColor = EditorTokens.ColorButtonText;
					break;
			}
		}
		// ↑↑↑ 旧 API 结束 ↑↑↑
	}
}
#endif
