using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UITheme", menuName = "Game/UI/Theme")]
public class UIThemeSO : ScriptableObject
{
    [Header("Panel")]
    public Color screenBackgroundColor = new(0.08f, 0.08f, 0.08f, 1f);
    public Color overlayBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.85f);

    [Header("Button")]
    public Color buttonNormal = new(0.2f, 0.2f, 0.2f, 1f);
    public Color buttonHover = new(0.35f, 0.35f, 0.35f, 1f);
    public Color buttonPressed = new(0.12f, 0.12f, 0.12f, 1f);
    public Color buttonDisabled = new(0.1f, 0.1f, 0.1f, 0.5f);
    public Color buttonTextColor = Color.white;

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
