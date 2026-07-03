using System.Collections.Generic;
using System.Linq;
using RedDust.Character;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.UI
{
    /// <summary>
    /// 武器栏 Overlay。动态槽位——背包里几个武器就几个槽位。
    ///
    /// TODO: BuildContext 外部引用 — 当前直接穿透 PlayerActor → BuildContext → CharacterContainer
    /// → BodyContainer → GetItem("Back") → NestedContainer，四层链直达 L3 Character 内部。
    /// UI 层不应知道 Back 槽位、NestedContainer 这些 L3 概念。
    /// 后续武器槽位系统应提供只读的 IWeaponSlotProvider，由 CharacterActor 实现，UIService 暴露。
    ///
    /// 暂时裸读背包容器，后续会有专用武器槽位系统。
    /// TODO: 专用武器槽位系统，不再是裸读背包。
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

            var ctx = GetBuildContext();
            if (ctx == null) return;

            var items = GetBackpackItems(ctx);
            EnsureSlots(items.Count);
            RefreshSlots(items);
        }

        // ── Slot Refresh ─────────────────────────────────────────────

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

        private void RefreshSlots(List<Entity> items)
        {
            for (int i = 0; i < items.Count && i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var entity = items[i];
                if (entity == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(null); // TODO: ItemDefSO 暂无 icon 字段
                slot.SetSlotLabel(entity.Preset != null ? entity.Preset.name : "???");

                // 显示武器标签
                var tags = entity.Properties?.GetTagList(CharacterConst.PropertyPath.CommonTags);
                if (tags != null && tags.Length > 0)
                    slot.SetSlotLabel($"{entity.Preset?.name ?? "???"} [{string.Join(", ", tags)}]");
            }
        }

        // ── Data ─────────────────────────────────────────────────────

        // TODO: BuildContext 外部引用 — 4 层穿透直达 L3 Character 内部结构。
        // ctx.CharacterContainer?.BodyContainer?.GetItem("Back")?.NestedContainer
        // 后续收敛为只读外部接口，UI 层只拿武器列表，不知道来源是哪个身体槽位。
        private static List<Entity> GetBackpackItems(CharacterBuildContext ctx)
        {
            var result = new List<Entity>();
            var backpack = ctx.CharacterContainer?.BodyContainer
                ?.GetItem(CharacterConst.Slot.Back)
                ?.NestedContainer;

            if (backpack == null) return result;

            result.AddRange(backpack.AllItems().Where(e => e != null));
            return result;
        }

        // TODO: BuildContext 外部引用 — 同上 AbilityBarOverlay，后续收敛到外部接口。
        private CharacterBuildContext GetBuildContext()
        {
            if (uiService == null) return null;
            return uiService.PlayerActor?.BuildContext;
        }
    }
}
