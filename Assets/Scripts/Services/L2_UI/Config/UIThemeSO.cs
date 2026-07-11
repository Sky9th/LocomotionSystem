using TMPro;
using UnityEngine;

namespace RedDust.Services.UI
{

    [CreateAssetMenu(fileName = "UIThemeSO", menuName = "RedDust/UI/Theme")]
    public class UIThemeSO : ScriptableObject
    {
        [Header("Panel")]
        public Color screenBackgroundColor = new(0.08f, 0.08f, 0.08f, 1f);
        public Color overlayBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.85f);

        [Header("Button Colors")]
        public Color buttonDisabled = new(0.1f, 0.1f, 0.1f, 0.5f);

        [Header("Color Styles")]
        public UIColorSet normalColors = new()
        {
            primary = new(0.2f, 0.2f, 0.2f, 1f),
            primaryHover = new(0.35f, 0.35f, 0.35f, 1f),
            primaryPressed = new(0.12f, 0.12f, 0.12f, 1f),
            onPrimary = Color.white,
            surface = new(0.1f, 0.1f, 0.1f, 0.9f),
            surfaceAlt = new(0.16f, 0.16f, 0.16f, 1f),
            onSurface = new(0.85f, 0.85f, 0.85f, 1f),
            onSurfaceMuted = new(0.55f, 0.55f, 0.55f, 1f),
            border = new(0.25f, 0.25f, 0.25f, 1f),
        };

        public UIColorSet primaryColors = new()
        {
            primary = new(0.7f, 0.4f, 0.1f, 1f),
            primaryHover = new(0.85f, 0.5f, 0.12f, 1f),
            primaryPressed = new(0.5f, 0.28f, 0.07f, 1f),
            onPrimary = Color.white,
            surface = new(0.12f, 0.09f, 0.06f, 0.9f),
            surfaceAlt = new(0.18f, 0.14f, 0.1f, 1f),
            onSurface = new(0.9f, 0.88f, 0.82f, 1f),
            onSurfaceMuted = new(0.65f, 0.6f, 0.5f, 1f),
            border = new(0.35f, 0.25f, 0.15f, 1f),
        };

        public UIColorSet dangerColors = new()
        {
            primary = new(0.7f, 0.15f, 0.15f, 1f),
            primaryHover = new(0.85f, 0.18f, 0.18f, 1f),
            primaryPressed = new(0.5f, 0.1f, 0.1f, 1f),
            onPrimary = Color.white,
            surface = new(0.12f, 0.07f, 0.07f, 0.9f),
            surfaceAlt = new(0.18f, 0.11f, 0.11f, 1f),
            onSurface = new(0.9f, 0.85f, 0.85f, 1f),
            onSurfaceMuted = new(0.65f, 0.55f, 0.55f, 1f),
            border = new(0.35f, 0.18f, 0.18f, 1f),
        };

        public UIColorSet warningColors = new()
        {
            primary = new(0.7f, 0.55f, 0.1f, 1f),
            primaryHover = new(0.85f, 0.68f, 0.12f, 1f),
            primaryPressed = new(0.5f, 0.38f, 0.07f, 1f),
            onPrimary = new(0.1f, 0.1f, 0.1f, 1f),
            surface = new(0.12f, 0.1f, 0.06f, 0.9f),
            surfaceAlt = new(0.18f, 0.15f, 0.1f, 1f),
            onSurface = new(0.9f, 0.88f, 0.82f, 1f),
            onSurfaceMuted = new(0.65f, 0.6f, 0.5f, 1f),
            border = new(0.35f, 0.3f, 0.15f, 1f),
        };

        public UIColorSet successColors = new()
        {
            primary = new(0.15f, 0.55f, 0.2f, 1f),
            primaryHover = new(0.18f, 0.68f, 0.24f, 1f),
            primaryPressed = new(0.1f, 0.4f, 0.14f, 1f),
            onPrimary = Color.white,
            surface = new(0.07f, 0.12f, 0.08f, 0.9f),
            surfaceAlt = new(0.11f, 0.18f, 0.13f, 1f),
            onSurface = new(0.85f, 0.9f, 0.87f, 1f),
            onSurfaceMuted = new(0.55f, 0.65f, 0.58f, 1f),
            border = new(0.15f, 0.32f, 0.2f, 1f),
        };

        public UIColorSet GetColorSet(UIColorStyle style) => style switch
        {
            UIColorStyle.Primary => primaryColors,
            UIColorStyle.Danger => dangerColors,
            UIColorStyle.Warning => warningColors,
            UIColorStyle.Success => successColors,
            _ => normalColors,
        };

        [Header("Text")]
        public Color titleColor = Color.white;
        public Color bodyColor = new(0.85f, 0.85f, 0.85f, 1f);
        public Color subtitleColor = new(0.7f, 0.7f, 0.7f, 1f);
        public Color accentColor = new(0.85f, 0.45f, 0.1f, 1f);
        public Color dangerColor = Color.red;

        [Header("Stat Bar")]
        public Color statHighColor = new(0.3f, 0.8f, 0.3f, 1f);
        public Color statMidColor = new(0.9f, 0.72f, 0.2f, 1f);
        public Color statLowColor = new(0.9f, 0.2f, 0.2f, 1f);
        public float statHighThreshold = 0.66f;
        public float statLowThreshold = 0.33f;

        [Header("Typography")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        public float titleFontSize = 48f;
        public float subtitleFontSize = 28f;
        public float bodyFontSize = 18f;
        public float buttonFontSize = 22f;
        public float smallFontSize = 14f;

        [Header("Layout")]
        public float elementSpacing = 12f;
        public Vector2 buttonSize = new(280f, 50f);

        [Header("Animation")]
        public float fadeDuration = 0.3f;
        public float slideDuration = 0.35f;
        public float buttonHoverScale = 1.05f;
        public float buttonPressScale = 0.97f;
        public float buttonAnimDuration = 0.1f;
        public float statBarFillSpeed = 0.2f;
    }
}
