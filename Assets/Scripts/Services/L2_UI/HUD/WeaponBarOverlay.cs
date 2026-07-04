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

        private static readonly string[] FallbackLabels = { "空手", "剑", "手枪" };

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            if (uiService == null) return;
            var entity = uiService.PlayerEntity;
            if (entity == null) return;

            var equip = entity.Query.Equipment;
            var bp = equip?.GetEquipped(CharacterConst.Slot.Back);
            var bpInv = bp?.Query.Inventory;

            // 固定三槽：空手 / 剑 / 手枪（可能在背包或装备槽中，两处都查）
            var weapons = new List<Entity>
            {
                null,
                ResolveWeapon(bpInv, equip, "test_blade"),
                ResolveWeapon(bpInv, equip, "test_pistol"),
            };

            // 选中态：当前右手装备决定高亮哪个槽位
            var rh = equip?.RightHand;
            int selectedIndex = rh == null ? 0
                : rh.Id == "test_blade" ? 1
                : rh.Id == "test_pistol" ? 2
                : -1;

            EnsureSlots(weapons.Count);
            RefreshSlots(weapons, selectedIndex);
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

        /// <summary>在背包和右手装备槽中查找武器实体。</summary>
        private static Entity ResolveWeapon(InventoryQuery bpInv, EquipmentQuery equip, string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            return bpInv?.FindItem(entityId)
                ?? (equip?.RightHand?.Id == entityId ? equip.RightHand : null);
        }

        private void RefreshSlots(IReadOnlyList<Entity> equipped, int selectedIndex)
        {
            for (int i = 0; i < equipped.Count; i++)
            {
                var slot = _slots[i];
                var entity = equipped[i];

                if (entity == null)
                {
                    slot.SetEmpty();
                    slot.SetSlotLabel(FallbackLabels[i]);
                }
                else
                {
                    slot.SetIcon(null); // TODO: ItemDefSO 暂无 icon 字段
                    var name = entity.Query.Preset != null ? entity.Query.Preset.name : FallbackLabels[i];
                    slot.SetSlotLabel(name);
                }

                // SetSelected 必须在 SetEmpty 之后 —— SetEmpty 内部会 SetSelected(false)
                slot.SetSelected(i == selectedIndex);
            }
        }
    }
}
