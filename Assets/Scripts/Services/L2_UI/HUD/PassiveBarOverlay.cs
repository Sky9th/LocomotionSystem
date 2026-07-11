using System;
using RedDust.Gameplay.Ability;
using RedDust.Services.EntityService;
using UnityEngine;

namespace RedDust.Services.UI
{
    /// <summary>
    /// 被动技能栏 Overlay。展示当前生效的被动技能图标和冷却状态。
    /// 纯 Entity.Query.Ability 读数据，参考 AbilityBarOverlay 模式（无快捷键、无选中态）。
    /// </summary>
    public class PassiveBarOverlay : UIOverlay
    {
        [Header("Slot")]
        [SerializeField] private UIIconSlot slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.15f;

        private readonly System.Collections.Generic.List<UIIconSlot> _slots = new();
        private PassiveAbilitySO[] _passives = Array.Empty<PassiveAbilitySO>();
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

            var passives = ability.PassiveAbilities;
            _passives = passives;
            EnsureSlots(passives.Length);
            RefreshSlots(ability);
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var slot = slotContainer != null
                    ? Instantiate(slotPrefab, slotContainer)
                    : Instantiate(slotPrefab);
                slot.name = $"PassiveSlot_{_slots.Count}";
                slot.SetKeybind(null); // 被动技能无快捷键
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].gameObject.SetActive(i < count);
        }

        private void RefreshSlots(AbilityQuery ability)
        {
            for (int i = 0; i < _passives.Length && i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var passive = _passives[i];
                if (passive == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(passive.icon);
                slot.SetSlotLabel(passive.displayName ?? passive.internalName);

                float remaining = ability.GetPassiveCooldownRemaining(passive);
                slot.SetCooldown(remaining, passive.cooldownDuration);
                // 被动无选中态
                slot.SetSelected(false);
            }
        }
    }
}
