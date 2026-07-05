using DG.Tweening;
using TMPro;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 单个浮动伤害数字 — Screen Space Overlay 版本。
    ///
    /// 由 DamageNumberOverlay 对象池管理。Play() 接收 overlay 本地坐标（来自
    /// RectTransformUtility.ScreenPointToLocalPointInRectangle），设置 anchoredPosition + DOTween 上飘淡出。
    ///
    /// Prefab 要求：Anchor=(0.5,0.5), Pivot=(0.5,0.5)。
    /// RectTransformUtility 返回的 localPos 以 overlay 中心为原点，
    /// widget 锚点也是中心 → anchoredPosition = localPos 直接对齐，数字居中于命中点上方。
    ///
    /// Amount 语义：瞬时伤害总和（SDamageInfo.TotalAmount），施展方 outgoing 伤害。
    /// </summary>
    public class DamageNumberWidget : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float riseDistance = 70f;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private float fadeDelay = 0.3f;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Offset")]
        [SerializeField] private float randomX = 15f;
        [SerializeField] private float randomY = 5f;

        [Header("References")]
        [SerializeField] private TMP_Text label;

        private RectTransform _rt;

        public bool IsIdle { get; private set; } = true;

        private void Awake()
        {
            _rt = transform as RectTransform;
        }

        /// <summary>
        /// 播放伤害数字动画。
        /// </summary>
        /// <param name="amount">预减免原始伤害值（SDamageInfo.Amount）</param>
        /// <param name="localPos">overlay 本地坐标，来自 RectTransformUtility.ScreenPointToLocalPointInRectangle</param>
        public void Play(float amount, Vector2 localPos)
        {
            DOTween.Kill(transform);

            IsIdle = false;
            gameObject.SetActive(true);

            float offsetX = Random.Range(-randomX, randomX);
            float offsetY = Random.Range(-randomY, randomY);

            _rt.anchoredPosition = new Vector2(
                localPos.x + offsetX,
                localPos.y + offsetY);

            label.text = Mathf.RoundToInt(amount).ToString();
            var c = label.color;
            c.a = 1f;
            label.color = c;

            var seq = DOTween.Sequence();
            seq.Join(_rt.DOAnchorPosY(_rt.anchoredPosition.y + riseDistance, duration)
                .SetEase(Ease.OutQuad));
            seq.Insert(fadeDelay,
                label.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                IsIdle = true;
                gameObject.SetActive(false);
            });
        }

        public void Recycle()
        {
            DOTween.Kill(transform);
            IsIdle = true;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
        }
    }
}
