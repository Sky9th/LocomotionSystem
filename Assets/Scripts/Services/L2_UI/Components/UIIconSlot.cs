using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedDust.UI
{
    /// <summary>
    /// 通用槽位显示组件。技能槽、武器槽共用——纯展示，不轮询游戏数据。
    ///
    /// 外部（ActionBarOverlay 等）在 Update 中调用 SetIcon / SetCooldown / SetSelected 等 setter。
    /// 遵循 UIStatBar 模式：[ExecuteAlways] + Theme SO + DOTween 动画 + Edit Mode 守卫。
    /// </summary>
    [ExecuteAlways]
    public class UIIconSlot : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private UIThemeSO theme;

        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private TMP_Text keybindLabel;
        [SerializeField] private TMP_Text slotLabel;

        [Header("State Colors")]
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color normalColor = new(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color cooldownDimColor = new(0.2f, 0.2f, 0.2f, 0.7f);

        [Header("Animation")]
        [SerializeField] private float cooldownFillDuration = 0.15f;

        private float _targetCooldownFill;
        private bool _isOnCooldown;

        private void Awake()
        {
            if (selectionBorder != null)
                selectionBorder.gameObject.SetActive(false);

            if (cooldownFill != null)
            {
                cooldownFill.type = Image.Type.Filled;
                cooldownFill.fillMethod = Image.FillMethod.Radial360;
                cooldownFill.fillOrigin = (int)Image.Origin360.Top;
                cooldownFill.fillAmount = 0f;
            }

            if (keybindLabel != null && theme != null)
            {
                keybindLabel.font = theme.bodyFont;
                keybindLabel.fontSize = theme.smallFontSize;
            }

            if (slotLabel != null && theme != null)
            {
                slotLabel.font = theme.bodyFont;
                slotLabel.fontSize = theme.smallFontSize;
            }

            if (cooldownText != null && theme != null)
            {
                cooldownText.font = theme.bodyFont;
                cooldownText.fontSize = theme.smallFontSize;
                cooldownText.text = "";
            }
        }

        // ── Public Setters ──────────────────────────────────────────

        /// <summary>设置槽位图标。null = 清空。</summary>
        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        /// <summary>
        /// 设置冷却覆层。remaining &lt;= 0 且 total &lt;= 0 → 无冷却，隐藏覆层。
        /// remaining &gt; 0 → 显示覆层 sweep（顺时针从顶）。
        /// </summary>
        public void SetCooldown(float remaining, float total)
        {
            if (total <= 0f || remaining <= 0f)
            {
                _isOnCooldown = false;
                _targetCooldownFill = 0f;

                if (cooldownFill != null)
                {
                    if (Application.isPlaying)
                        cooldownFill.DOFillAmount(0f, cooldownFillDuration).SetEase(Ease.OutCubic);
                    else
                        cooldownFill.fillAmount = 0f;
                }

                if (cooldownText != null)
                    cooldownText.text = "";

                if (iconImage != null)
                    iconImage.color = Color.white;

                return;
            }

            _isOnCooldown = true;
            _targetCooldownFill = Mathf.Clamp01(remaining / total);

            if (cooldownFill != null)
            {
                if (Application.isPlaying)
                    cooldownFill.DOFillAmount(_targetCooldownFill, cooldownFillDuration).SetEase(Ease.OutCubic);
                else
                    cooldownFill.fillAmount = _targetCooldownFill;
            }

            if (cooldownText != null)
                cooldownText.text = remaining.ToString("F1");

            if (iconImage != null)
                iconImage.color = cooldownDimColor;
        }

        /// <summary>设置选中状态边框。</summary>
        public void SetSelected(bool selected)
        {
            if (selectionBorder == null) return;
            selectionBorder.gameObject.SetActive(selected);
            if (selected)
                selectionBorder.color = selectedColor;
        }

        /// <summary>设置快捷键标签（"Q", "E", "1" 等）。</summary>
        public void SetKeybind(string key)
        {
            if (keybindLabel == null) return;
            keybindLabel.text = key;
        }

        /// <summary>设置槽位底部文字标签。</summary>
        public void SetSlotLabel(string label)
        {
            if (slotLabel == null) return;
            slotLabel.text = label ?? "";
        }

        /// <summary>清空槽位——所有显示重置为空状态。</summary>
        public void SetEmpty()
        {
            SetIcon(null);
            SetCooldown(0f, 0f);
            SetSelected(false);
            if (slotLabel != null) slotLabel.text = "";
            _isOnCooldown = false;
        }

        // ── Editor Preview ──────────────────────────────────────────

        private void Update()
        {
            if (Application.isPlaying) return;

            // Edit mode: snap cooldown fill to target without animation
            if (cooldownFill != null && !Mathf.Approximately(cooldownFill.fillAmount, _targetCooldownFill))
                cooldownFill.fillAmount = _targetCooldownFill;

            if (selectionBorder != null)
                selectionBorder.color = selectedColor;
        }

        private void OnDestroy()
        {
            if (cooldownFill != null)
                DOTween.Kill(cooldownFill);
        }
    }
}
