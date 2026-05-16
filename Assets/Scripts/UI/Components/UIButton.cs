using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private UIThemeSO theme;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button button;

    public event Action OnClicked;

    public string Label
    {
        get => labelText != null ? labelText.text : "";
        set { if (labelText != null) labelText.text = value; }
    }

    public bool Interactable
    {
        get => button != null && button.interactable;
        set
        {
            if (button != null) button.interactable = value;
            ApplyColor(value ? theme.buttonNormal : theme.buttonDisabled);
        }
    }

    public Button Button => button;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(HandleClick);
        ApplyTheme();
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        OnClicked?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable) return;
        transform.DOScale(theme.buttonHoverScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
        background.DOColor(theme.buttonHover, theme.buttonAnimDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!Interactable) return;
        transform.DOScale(1f, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
        background.DOColor(theme.buttonNormal, theme.buttonAnimDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        transform.DOScale(theme.buttonPressScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
        background.DOColor(theme.buttonPressed, theme.buttonAnimDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable) return;
        transform.DOScale(theme.buttonHoverScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
        background.DOColor(theme.buttonHover, theme.buttonAnimDuration);
    }

    private void ApplyTheme()
    {
        if (theme == null) return;
        if (background != null) ApplyColor(theme.buttonNormal);
        if (labelText != null) labelText.color = theme.buttonTextColor;
        if (button != null)
        {
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
        }
    }

    private void ApplyColor(Color color)
    {
        if (background != null) background.color = color;
    }
}
