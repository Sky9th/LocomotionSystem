using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using RedDust.Ability;
using RedDust.Core;
using RedDust.Core.Events;

namespace RedDust.UI
{
    /// <summary>
    /// 伤害飘字 Overlay — Screen Space 方案。
    ///
    /// 位于 UIService.overlayContainer 下，全屏拉伸，共享父级 Screen Space Canvas。
    /// 订阅 HitEvent，WorldToScreenPoint → RectTransformUtility 坐标转换 → widget 对象池。
    ///
    /// 尸潮场景：单 Canvas 合批，maxPoolSize 上限静默丢弃，无性能压力。
    /// </summary>
    public class DamageNumberOverlay : UIOverlay
    {
        [Header("Pool")]
        [SerializeField] private DamageNumberWidget widgetPrefab;
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 30;

        [Header("Screen Offset")]
        [SerializeField] private float screenOffsetY = 50f;

        [Header("Camera")]
        [SerializeField] private Camera worldCamera;

        private EventHub _eventHub;
        private HitEvent _hitEvent;
        private RectTransform _rectTransform;

        private readonly Stack<DamageNumberWidget> _pool = new();
        private readonly List<DamageNumberWidget> _active = new();

        // ── UIOverlay Override ──

        /// <summary>
        /// 全屏透明 Overlay 不拦截射线，也不需要 fade 过渡动画。
        /// 跳过基类的 CanvasGroup 交互开启，直接返回 null。
        /// </summary>
        public override DG.Tweening.Sequence PlayEnterSequence(object args = null) => null;

        // ── Lifecycle ──

        protected override void OnInitialize()
        {
            _rectTransform = transform as RectTransform;

            if (!TrySubscribe()) return;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (widgetPrefab == null)
            {
                Debug.LogError("[DamageNumber] widgetPrefab is null.");
                return;
            }

            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                var w = Instantiate(widgetPrefab, _rectTransform);
                w.name = $"DmgNum_{i}";
                w.gameObject.SetActive(false);
                _pool.Push(w);
            }

            Debug.Log($"[DamageNumber] Ready — {_pool.Count} widgets pooled.");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_hitEvent != null)
                _hitEvent.Unregister(OnHitReceived);

            foreach (var w in _active)
                if (w != null) Destroy(w.gameObject);
            _active.Clear();

            while (_pool.Count > 0)
            {
                var w = _pool.Pop();
                if (w != null) Destroy(w.gameObject);
            }
        }

        private bool TrySubscribe()
        {
            if (GameContext.Instance == null)
            {
                Debug.LogError("[DamageNumber] GameContext.Instance is null.");
                return false;
            }
            if (!GameContext.Instance.TryResolveService(out _eventHub))
            {
                Debug.LogError("[DamageNumber] EventHub not found.");
                return false;
            }
            _hitEvent = _eventHub.Get<HitEvent>();
            if (_hitEvent == null)
            {
                Debug.LogError("[DamageNumber] HitEvent not found in EventHub.abilityEvents.");
                return false;
            }
            _hitEvent.Register(OnHitReceived);
            return true;
        }

        // ── Update: Recycle ──

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var w = _active[i];
                if (w == null) { _active.RemoveAt(i); continue; }
                if (w.IsIdle)
                {
                    _active.RemoveAt(i);
                    ReturnToPool(w);
                }
            }
        }

        // ── Event ──

        private void OnHitReceived(SDamageInfo hit)
        {
            // 过滤零伤害 / 完全回避
            if (hit.Amount <= 0f) return;

            // World → Screen 像素
            Vector3 screenPos = worldCamera.WorldToScreenPoint(hit.HitPoint);
            if (screenPos.z < 0f) return; // 相机后方

            // Screen → Overlay 本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                new Vector2(screenPos.x, screenPos.y + screenOffsetY),
                null, // Screen Space - Overlay
                out Vector2 localPos);

            var widget = GetWidget();
            if (widget == null) return; // 池耗尽，丢弃

            widget.Play(hit.Amount, localPos);
            _active.Add(widget);
        }

        // ── Pool ──

        private DamageNumberWidget GetWidget()
        {
            if (_pool.Count > 0)
                return _pool.Pop();

            if (_active.Count + _pool.Count < maxPoolSize)
                return CreateWidget();

            return null; // 池耗尽
        }

        private void ReturnToPool(DamageNumberWidget widget)
        {
            widget.Recycle();
            _pool.Push(widget);
        }

        private DamageNumberWidget CreateWidget()
        {
            var instance = Instantiate(widgetPrefab, _rectTransform);
            instance.name = $"DmgNum_{_pool.Count + _active.Count}";
            return instance;
        }

        // ── Editor Test ──

#if UNITY_EDITOR
        [ContextMenu("Test Damage Number")]
        private void TestDamage()
        {
            if (!Application.isPlaying || widgetPrefab == null) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            var widget = GetWidget();
            if (widget == null) return;

            // 屏幕中央
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                new Vector2(Screen.width / 2f, Screen.height / 2f),
                null,
                out Vector2 localPos);

            widget.Play(Random.Range(10f, 99f), localPos);
            _active.Add(widget);
        }
#endif
    }
}
