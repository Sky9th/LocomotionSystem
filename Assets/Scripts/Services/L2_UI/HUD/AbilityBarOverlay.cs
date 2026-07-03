using System;
using RedDust.Ability;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 技能栏 Overlay。动态槽位 + 事件驱动激活。
    /// 纯 Entity.Query.Ability 读数据，不再穿透 Actor/BuildContext。
    /// </summary>
    public class AbilityBarOverlay : UIOverlay
    {
        [Header("Slot")]
        [SerializeField] private UIIconSlot slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.15f;

        private readonly System.Collections.Generic.List<UIIconSlot> _slots = new();
        private ActiveAbilitySO[] _actives = Array.Empty<ActiveAbilitySO>();
        private float _refreshTimer;

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            if (uiService == null) return;
            var entity = uiService.PlayerEntity;
            if (entity == null) return;

            var ability = entity.Query.Ability;
            if (ability == null) return;

            var actives = ability.ActiveAbilities;
            _actives = actives;
            EnsureSlots(actives.Length);
            RefreshSlots(ability);
        }

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

        private void RefreshSlots(AbilityQuery ability)
        {
            for (int i = 0; i < _actives.Length && i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var active = _actives[i];
                if (active == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(active.icon);
                slot.SetSlotLabel(active.displayName ?? active.internalName);

                float remaining = ability.GetCooldownRemaining(active);
                slot.SetCooldown(remaining, active.cooldownDuration);
                slot.SetSelected(ability.IsActive(active));
            }
        }
    }
}
