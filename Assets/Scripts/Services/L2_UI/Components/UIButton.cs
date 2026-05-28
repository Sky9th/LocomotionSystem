using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RedDust.UI
{

    [ExecuteAlways]
    public class UIButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private UIColorStyle style = UIColorStyle.Normal;
        [SerializeField] private UIThemeSO theme;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Button button;

        public event Action OnClicked;

        public string Label
        {
            get => labelText.text;
            set => labelText.text = value;
        }

        public void SetText(string text) => labelText.text = text;

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
            ApplyColor(interactable ? theme.GetColorSet(style).primary : theme.buttonDisabled);
        }

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null) button.interactable = value;
                ApplyColor(value ? theme.GetColorSet(style).primary : theme.buttonDisabled);
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
            DOTween.Kill(transform);
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable || !Application.isPlaying) return;
            var cs = theme.GetColorSet(style);
            transform.DOScale(theme.buttonHoverScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
            background.DOColor(cs.primaryHover, theme.buttonAnimDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Interactable || !Application.isPlaying) return;
            var cs = theme.GetColorSet(style);
            transform.DOScale(1f, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
            background.DOColor(cs.primary, theme.buttonAnimDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable || !Application.isPlaying) return;
            var cs = theme.GetColorSet(style);
            transform.DOScale(theme.buttonPressScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
            background.DOColor(cs.primaryPressed, theme.buttonAnimDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Interactable || !Application.isPlaying) return;
            var cs = theme.GetColorSet(style);
            transform.DOScale(theme.buttonHoverScale, theme.buttonAnimDuration).SetEase(Ease.OutQuad);
            background.DOColor(cs.primaryHover, theme.buttonAnimDuration);
        }

        private void ApplyTheme()
        {
            if (theme == null) return;
            var cs = theme.GetColorSet(style);
            if (background != null) ApplyColor(cs.primary);
            if (labelText != null)
            {
                labelText.color = cs.onPrimary;
                labelText.font = theme.bodyFont;
                labelText.fontSize = theme.buttonFontSize;
            }
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
}
