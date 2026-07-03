using System;
using System.Collections.Generic;
using RedDust.Ability;
using RedDust.Character;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 技能栏 Overlay。动态槽位（技能树给几个就几个）+ 事件驱动激活。
    ///
    /// 订阅玩家 EventHub 的 InputSkill1/2/3Event，按槽位索引触发对应技能。
    /// 单一 OnSkill handler，lambda 闭包捕获 slotIndex。
    ///
    /// TODO: 玩家自定义槽位缓存 — 未来玩家可把不同技能放到不同槽位。
    /// </summary>
    public class AbilityBarOverlay : UIOverlay
    {
        [Header("Slot")]
        [SerializeField] private UIIconSlot slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.15f;

        private readonly List<UIIconSlot> _slots = new();
        private ActiveAbilitySO[] _actives = Array.Empty<ActiveAbilitySO>();

        // ═══════════════════════════════════════════════════════════════
        // TODO: Input 事件消费者归位 — 以下事件订阅已迁至 AbilityExecutor。
        // UI 只保留显示逻辑。后续 UI 重构时彻底清理。
        // ═══════════════════════════════════════════════════════════════
        // private Action<SButtonInputPayload> _onSkill0, _onSkill1, _onSkill2;
        // private EventHub _playerEventHub;
        // private bool _eventsBound;

        private float _refreshTimer;

        // ── Lifecycle ────────────────────────────────────────────────

        // protected override void OnDestroy()
        // {
        //     base.OnDestroy();
        //     UnbindEvents();
        // }

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            var ctx = GetBuildContext();
            if (ctx == null) return;

            // TODO: Input 事件已迁至 AbilityExecutor，此处不再订阅
            // BindEventsOnce(ctx);

            var actives = ctx.AbilityForest?.ResolvedActives ?? Array.Empty<ActiveAbilitySO>();
            _actives = actives;
            EnsureSlots(actives.Length);
            RefreshSlots(ctx);
        }

        // ═══════════════════════════════════════════════════════════════
        // TODO: Input 事件消费者归位 — 以下已迁至 AbilityExecutor。
        // ═══════════════════════════════════════════════════════════════
        //
        // private void BindEventsOnce(CharacterBuildContext ctx)
        // {
        //     if (_eventsBound) return;
        //     var hub = ctx.EventHub;
        //     if (hub == null) return;
        //     _onSkill0 = p => OnSkill(p, 0);
        //     _onSkill1 = p => OnSkill(p, 1);
        //     _onSkill2 = p => OnSkill(p, 2);
        //     hub.Get<InputSkill1Event>().Register(_onSkill0);
        //     hub.Get<InputSkill2Event>().Register(_onSkill1);
        //     hub.Get<InputSkill3Event>().Register(_onSkill2);
        //     _playerEventHub = hub;
        //     _eventsBound = true;
        // }
        //
        // private void UnbindEvents()
        // {
        //     if (!_eventsBound || _playerEventHub == null) return;
        //     _playerEventHub.Get<InputSkill1Event>().Unregister(_onSkill0);
        //     _playerEventHub.Get<InputSkill2Event>().Unregister(_onSkill1);
        //     _playerEventHub.Get<InputSkill3Event>().Unregister(_onSkill2);
        //     _eventsBound = false;
        //     _playerEventHub = null;
        // }
        //
        // private void OnSkill(SButtonInputPayload p, int slotIndex)
        // {
        //     if (!p.IsRequested) return;
        //     if (slotIndex >= _actives.Length) return;
        //     var ability = _actives[slotIndex];
        //     if (ability == null) return;
        //     var ctx = GetBuildContext();
        //     if (ctx == null) return;
        //     var origin = ctx.ModelRoot != null ? ctx.ModelRoot.position : Vector3.zero;
        //     var direction = ctx.ModelRoot != null ? ctx.ModelRoot.forward : Vector3.forward;
        //     var weapon = ctx.CharacterContainer?.BodyContainer?.GetItem(CharacterConst.Slot.RightHand);
        //     Debug.Log($"[AbilityBar] Slot {slotIndex}: {ability.internalName}" +
        //               (weapon != null ? $" | weapon={weapon.Preset.name}" : ""));
        //     ctx.Ability.Enqueue(ability, origin, direction, weapon);
        // }

        // ── Slot Refresh ─────────────────────────────────────────────

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var slot = slotContainer != null
                    ? Instantiate(slotPrefab, slotContainer)
                    : Instantiate(slotPrefab);
                slot.name = $"SkillSlot_{_slots.Count}";
                slot.SetKeybind($"{_slots.Count + 1}");
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].gameObject.SetActive(i < count);
        }

        private void RefreshSlots(CharacterBuildContext ctx)
        {
            var executor = ctx.Ability;
            var pipeline = executor?.Pipeline;

            for (int i = 0; i < _actives.Length && i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var ability = _actives[i];
                if (ability == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(ability.icon);
                slot.SetSlotLabel(ability.displayName ?? ability.internalName);

                if (executor != null)
                {
                    float remaining = executor.GetAbilityCooldownRemaining(ability);
                    slot.SetCooldown(remaining, ability.cooldownDuration);

                    bool isActive = pipeline != null
                        && !pipeline.IsIdle
                        && pipeline.Context.Ability == ability;
                    slot.SetSelected(isActive);
                }
            }
        }

        // ── Context ──────────────────────────────────────────────────

        // TODO: BuildContext 外部引用 — UI 层不应直接依赖 L3 CharacterBuildContext。
        // 后续设计面向外部的只读接口（如 IAbilityStateProvider），由 CharacterActor 实现，UIService 暴露。
        private CharacterBuildContext GetBuildContext()
        {
            if (uiService == null) return null;
            return uiService.PlayerActor?.BuildContext;
        }
    }
}
