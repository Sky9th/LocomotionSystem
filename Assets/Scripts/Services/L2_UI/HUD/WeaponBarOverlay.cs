using System.Collections.Generic;
using RedDust.Character;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 武器栏 Overlay。动态槽位——身体装备几个就几个槽位。
    /// 纯数据驱动，通过 Entity.Query.Equipment 遍历身体槽位装备。
    /// </summary>
    public class WeaponBarOverlay : UIOverlay
    {
        [Header("Slot")]
        [SerializeField] private UIIconSlot slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.15f;

        private readonly List<UIIconSlot> _slots = new();
        private float _refreshTimer;

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            if (uiService == null) return;
            var entity = uiService.PlayerEntity;
            if (entity == null) return;

            var bp = entity.Query.Equipment.GetEquipped(CharacterConst.Slot.Back);
            var equipped = bp.Query.Inventory.AllItems;
            if (equipped == null) return;

            EnsureSlots(equipped.Count);
            RefreshSlots(equipped);
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var slot = slotContainer != null
                    ? Instantiate(slotPrefab, slotContainer)
                    : Instantiate(slotPrefab);
                slot.name = $"WeaponSlot_{_slots.Count}";
                slot.SetKeybind($"{_slots.Count + 1}");
                _slots.Add(slot);
            }
        

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].gameObject.SetActive(i < count);
        }

        private void RefreshSlots(IReadOnlyList<Entity> equipped)
        {
            for (int i = 0; i < equipped.Count; i++)
            {
                var slot = _slots[i];
                var entity = equipped[i];
                if (entity == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(null); // TODO: ItemDefSO 暂无 icon 字段
                var name = entity.Query.Preset != null ? entity.Query.Preset.name : "???";
                slot.SetSlotLabel($"{name}");
            }
        }
    }
}
