using TMPro;
using UnityEngine;

public enum UITextStyle { Title, Subtitle, Body, Button, Small }

public class UILabel : MonoBehaviour
{
    [SerializeField] private UIThemeSO theme;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private UITextStyle textStyle = UITextStyle.Body;

    public string Text
    {
        get => tmpText != null ? tmpText.text : "";
        set { if (tmpText != null) tmpText.text = value; }
    }

    private void Awake()
    {
        ApplyStyle();
    }

    public void SetStyle(UITextStyle style)
    {
        textStyle = style;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (theme == null || tmpText == null) return;

        switch (textStyle)
        {
            case UITextStyle.Title:
                tmpText.font = theme.titleFont;
                tmpText.fontSize = theme.titleFontSize;
                tmpText.color = theme.titleColor;
                break;
            case UITextStyle.Subtitle:
                tmpText.font = theme.titleFont;
                tmpText.fontSize = theme.subtitleFontSize;
                tmpText.color = theme.subtitleColor;
                break;
            case UITextStyle.Body:
                tmpText.font = theme.bodyFont;
                tmpText.fontSize = theme.bodyFontSize;
                tmpText.color = theme.bodyColor;
                break;
            case UITextStyle.Button:
                tmpText.font = theme.bodyFont;
                tmpText.fontSize = theme.buttonFontSize;
                tmpText.color = theme.buttonTextColor;
                break;
            case UITextStyle.Small:
                tmpText.font = theme.bodyFont;
                tmpText.fontSize = theme.smallFontSize;
                tmpText.color = theme.subtitleColor;
                break;
        }
    }
}
