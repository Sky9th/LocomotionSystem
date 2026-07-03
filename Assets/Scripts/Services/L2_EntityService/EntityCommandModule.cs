using RedDust.Character;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// Entity 的命令门面——外部系统通过此模块向实体下达命令。
    ///
    /// 子模块（Pathfinding、Ability、Container）由 CharacterActor 暴露为 internal 属性，
    /// Command 直接调用，Actor 不代理。
    /// </summary>
    public class EntityCommandModule
    {
        private readonly Entity _entity;

        private CharacterActor _character;
        private CharacterActor Character => _character ??= _entity.View?.GetComponent<CharacterActor>();

        internal EntityCommandModule(Entity entity) { _entity = entity; }

        // ── 移动 ──

        public void MoveTo(Vector3 target) => Character?.Pathfinding?.SetDestination(target);
        public void StopMoving() => Character?.Pathfinding?.Stop();

        // ── 技能 ──

        public void UseActiveAbility(int slotIndex)
        {
            var actor = Character;
            if (actor == null) return;
            var ctx = actor.BuildContext;
            var actives = ctx.AbilityForest?.ResolvedActives;
            if (actives == null || slotIndex < 0 || slotIndex >= actives.Length) return;
            var def = actives[slotIndex];
            if (def == null || actor.Ability == null) return;

            var weapon = actor.Container?.BodyContainer?.GetItem(
                CharacterConst.Slot.RightHand);
            actor.Ability.Enqueue(def, ctx.ModelRoot.position,
                ctx.ModelRoot.forward, weapon);
        }

        // ── 装备（临时硬编码，后续迁移至装备系统）──

        private static readonly string[] EquipMap = { null, "test_blade", "test_pistol" };

        public void CycleEquip(int equipIndex)
        {
            if (equipIndex < 0 || equipIndex >= EquipMap.Length) return;
            var actor = Character;
            if (actor == null) return;

            var bodyContainer = actor.Container?.BodyContainer;
            if (bodyContainer == null) return;

            var currentEquipped = bodyContainer.GetItem(CharacterConst.Slot.RightHand);
            string targetId = EquipMap[equipIndex];

            if (currentEquipped != null && currentEquipped.Id == targetId) return;
            if (currentEquipped == null && targetId == null) return;

            var backpack = actor.Container?.BodyContainer
                ?.GetItem(CharacterConst.Slot.Back)?.NestedContainer;
            if (backpack == null) return;

            Entity target = null;
            if (targetId != null)
            {
                target = backpack.FindItem(CharacterConst.Slot.ContainerSlot, targetId);
                if (target == null) return;
            }

            if (currentEquipped != null)
            {
                bodyContainer.Remove(CharacterConst.Slot.RightHand, currentEquipped);
                backpack.Place(CharacterConst.Slot.ContainerSlot, currentEquipped);
            }
            if (target != null)
            {
                backpack.Remove(CharacterConst.Slot.ContainerSlot, target);
                bodyContainer.Place(CharacterConst.Slot.RightHand, target);
            }
        }

        // Future: Container / Item commands
    }
}
