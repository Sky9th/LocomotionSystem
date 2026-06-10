using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedDust.UI
{

    [ExecuteAlways]
    public class UIStatBar : MonoBehaviour
    {
        [SerializeField] private UIThemeSO theme;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private float fillDuration = 0.2f;

        private float targetFill;

        public void SetName(string name)
        {
            if (nameLabel != null) nameLabel.text = name;
        }

        public void SetValue(float normalized)
        {
            targetFill = Mathf.Clamp01(normalized);
            if (fillImage == null) return;

            if (Application.isPlaying)
                fillImage.DOFillAmount(targetFill, fillDuration).SetEase(Ease.OutCubic);
            else
                fillImage.fillAmount = targetFill;
        }

        public void SetValue(float current, float max)
        {
            if (max <= 0f)
            {
                SetValue(0f);
                if (valueLabel != null) valueLabel.text = "--";
                return;
            }

            SetValue(current / max);
            if (valueLabel != null)
                valueLabel.text = $"{current:F1}/{max:F1}";
        }

        private void Update()
        {
            if (fillImage != null && theme != null)
            {
                if (targetFill > theme.statHighThreshold)
                    fillImage.color = theme.statHighColor;
                else if (targetFill > theme.statLowThreshold)
                    fillImage.color = theme.statMidColor;
                else
                    fillImage.color = theme.statLowColor;
            }
        }

        private void Awake()
        {
            if (theme != null && backgroundImage != null)
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;
                fillImage.fillAmount = 0f;
            }
        }
    }
}
