using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedDust.Services.UI
{
    /// <summary>
    /// 技能详情卡片。在技能槽 hover/选中时弹出，展示图标、名称、效果、冷却、连招等完整信息。
    ///
    /// 用法：调用 SetData(SkillCardData) 填充，调用 SetVisible(bool) 控制显隐。
    /// 数据由 ActiveAbilitySO.ToSkillCardData() 提取。
    /// </summary>
    [ExecuteAlways]
    public class SkillCard : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private UIThemeSO theme;
        [SerializeField] private UIColorStyle colorStyle = UIColorStyle.Normal;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Identity")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        [Header("Stats")]
        [SerializeField] private TMP_Text cooldownLabel;
        [SerializeField] private TMP_Text activationInfoLabel;

        [Header("Timing")]
        [SerializeField] private GameObject timingSection;
        [SerializeField] private TMP_Text phaseTimingLabel;
        [SerializeField] private TMP_Text cancelInfoLabel;

        [Header("Effects")]
        [SerializeField] private GameObject effectsSection;
        [SerializeField] private TMP_Text damageModLabel;
        [SerializeField] private TMP_Text impactLabel;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private TMP_Text buffLabel;

        [Header("Combo")]
        [SerializeField] private GameObject comboSection;
        [SerializeField] private TMP_Text comboLabel;

        [Header("Noise")]
        [SerializeField] private TMP_Text noiseLabel;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Ease fadeEase = Ease.OutCubic;

        private bool _visible;
        private SkillCardData _data;

        // ── Public API ──────────────────────────────────────────────

        /// <summary>填充卡片数据并刷新所有显示。</summary>
        public void SetData(SkillCardData data)
        {
            _data = data;
            RefreshDisplay();
        }

        /// <summary>控制卡片显隐（带 fade 动画）。</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;

            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            if (Application.isPlaying)
            {
                DOTween.Kill(canvasGroup);
                canvasGroup.DOFade(visible ? 1f : 0f, fadeDuration).SetEase(fadeEase);
                if (visible) gameObject.SetActive(true);
                else
                {
                    var cg = canvasGroup;
                    DOVirtual.DelayedCall(fadeDuration, () =>
                    {
                        if (cg != null && Mathf.Approximately(cg.alpha, 0f))
                            cg.gameObject.SetActive(false);
                    });
                }
            }
            else
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                gameObject.SetActive(visible);
            }
        }

        // ── Display Refresh ────────────────────────────────────────

        private void RefreshDisplay()
        {
            // Identity
            if (iconImage != null)
            {
                iconImage.sprite = _data.icon;
                iconImage.enabled = _data.icon != null;
            }

            if (nameLabel != null)
                nameLabel.text = _data.displayName;

            if (descriptionLabel != null)
            {
                descriptionLabel.text = _data.description;
                descriptionLabel.gameObject.SetActive(!string.IsNullOrEmpty(_data.description));
            }

            // Stats
            if (cooldownLabel != null)
            {
                cooldownLabel.text = _data.cooldownDuration > 0
                    ? $"冷却: {_data.cooldownDuration:F1}s"
                    : "冷却: 无";
            }

            if (activationInfoLabel != null)
            {
                var sb = new StringBuilder();
                sb.Append(_data.activationTypeLabel);
                if (!string.IsNullOrEmpty(_data.searchTypeLabel) && _data.searchRange > 0)
                    sb.Append($"  |  {_data.searchTypeLabel} {_data.searchRange:F1}m");
                sb.Append($"  |  {_data.animationLayerLabel}");
                activationInfoLabel.text = sb.ToString();
            }

            // Timing
            if (timingSection != null)
            {
                bool hasTiming = _data.windupDuration > 0 || _data.fireWindowDuration > 0 || _data.recoveryDuration > 0;
                timingSection.SetActive(hasTiming);
            }

            if (phaseTimingLabel != null)
            {
                var speed = _data.animationSpeed > 0 ? _data.animationSpeed : 1f;
                var w = _data.windupDuration / speed;
                var f = _data.fireWindowDuration / speed;
                var r = _data.recoveryDuration / speed;
                var total = w + f + r;
                phaseTimingLabel.text = total > 0
                    ? $"前摇 {w:F2}s  →  激发 {f:F2}s  →  后摇 {r:F2}s  (总计 {total:F2}s)"
                    : "";
            }

            if (cancelInfoLabel != null)
            {
                var parts = new System.Collections.Generic.List<string>();
                if (_data.canCancelWindup) parts.Add("前摇✔");
                if (_data.canCancelRecovery) parts.Add("后摇✔");
                cancelInfoLabel.text = parts.Count > 0
                    ? $"可打断: {string.Join("  ", parts)}"
                    : "不可打断";
            }

            // Effects
            if (effectsSection != null)
                effectsSection.SetActive(_data.HasEffects);

            if (damageModLabel != null)
            {
                if (_data.damageModifiers != null && _data.damageModifiers.Length > 0)
                {
                    damageModLabel.text = $"伤害: {string.Join("  |  ", _data.damageModifiers)}";
                    damageModLabel.gameObject.SetActive(true);
                }
                else damageModLabel.gameObject.SetActive(false);
            }

            if (impactLabel != null)
            {
                impactLabel.text = _data.impactText;
                impactLabel.gameObject.SetActive(!string.IsNullOrEmpty(_data.impactText));
            }

            if (costLabel != null)
            {
                if (_data.costs != null && _data.costs.Length > 0)
                {
                    costLabel.text = $"消耗: {string.Join("  |  ", _data.costs)}";
                    costLabel.gameObject.SetActive(true);
                }
                else costLabel.gameObject.SetActive(false);
            }

            if (buffLabel != null)
            {
                if (_data.buffs != null && _data.buffs.Length > 0)
                {
                    buffLabel.text = $"Buff: {string.Join("\n", _data.buffs)}";
                    buffLabel.gameObject.SetActive(true);
                }
                else buffLabel.gameObject.SetActive(false);
            }

            // Combo
            if (comboSection != null)
                comboSection.SetActive(_data.HasCombo);

            if (comboLabel != null)
            {
                if (_data.comboLinks != null && _data.comboLinks.Length > 0)
                {
                    comboLabel.text = string.Join("\n", _data.comboLinks);
                    comboLabel.gameObject.SetActive(true);
                }
                else comboLabel.gameObject.SetActive(false);
            }

            // Noise
            if (noiseLabel != null)
            {
                if (_data.HasNoise)
                {
                    noiseLabel.text = $"噪音 Lv{_data.noiseLevel}  衰减半径: {_data.noiseDecayRadius:F0}m";
                    noiseLabel.gameObject.SetActive(true);
                }
                else noiseLabel.gameObject.SetActive(false);
            }
        }

        // ── Lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
            }

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (theme == null || background == null) return;
            var cs = theme.GetColorSet(colorStyle);
            background.color = cs.surface;

            // Fonts are applied by UILabel components on child objects.
            // For raw TMP_Text fields, apply body font/color directly.
            ApplyTextStyle(nameLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(descriptionLabel, cs.onSurfaceMuted, theme.bodyFont, theme.smallFontSize);
            ApplyTextStyle(cooldownLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(activationInfoLabel, cs.onSurfaceMuted, theme.bodyFont, theme.smallFontSize);
            ApplyTextStyle(phaseTimingLabel, cs.onSurfaceMuted, theme.bodyFont, theme.smallFontSize);
            ApplyTextStyle(cancelInfoLabel, cs.onSurfaceMuted, theme.bodyFont, theme.smallFontSize);
            ApplyTextStyle(damageModLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(impactLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(costLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(buffLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(comboLabel, cs.onSurface, theme.bodyFont, theme.bodyFontSize);
            ApplyTextStyle(noiseLabel, cs.onSurfaceMuted, theme.bodyFont, theme.smallFontSize);
        }

        private static void ApplyTextStyle(TMP_Text text, Color color, TMP_FontAsset font, float fontSize)
        {
            if (text == null) return;
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = color;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            // Delay call to avoid SendMessage warnings during reimport
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyTheme();
                    if (_data.displayName != null)
                        RefreshDisplay();
                }
            };
        }
#endif

        private void OnDestroy()
        {
            if (canvasGroup != null)
                DOTween.Kill(canvasGroup);
        }
    }
}
