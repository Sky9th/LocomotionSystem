using RedDust.Gameplay.Container;
using RedDust.Gameplay.Character;
using UnityEngine;

namespace RedDust.Services.EntityService
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

        /// <summary>
        /// 缓存的 CharacterActor 引用。
        /// 注意：必须使用 Unity 重载的 == null（而非 C# ?. / ??=）检测已销毁的 UnityEngine.Object。
        /// C# null-conditional 运算符绕过 Unity 的 fake-null 重载，导致 MissingReferenceException。
        /// 因此 getter 最后一步将销毁引用转为真正的 null，确保调用方的 ?. 能正确短路。
        /// </summary>
        private CharacterActor Character
        {
            get
            {
                // Unity overloaded == null: true for both real null AND destroyed objects
                if (_character == null)
                {
                    // HasView uses Unity's != null overload — correctly false for destroyed GO
                    if (_entity.HasView)
                        _character = _entity.View.GetComponent<CharacterActor>();
                }

                // If _character is still "Unity null" (destroyed), null it out so callers'
                // C# ?. operators correctly short-circuit instead of touching native memory.
                if (_character == null)
                    _character = null; // destroyed ref → real null
                return _character;
            }
        }

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

            actor.Pathfinding?.Stop();
            var weapon = _entity.Query.Equipment?.GetEquipped(CharacterConst.Slot.RightHand);
            actor.Ability.TryUse(def, ctx.ModelRoot.position,
                ctx.ModelRoot.forward, weapon);
        }

        // ── 容器 ──

        /// <summary>将物品放入自身指定槽位。</summary>
        public void Place(string slotKey, Entity item)
            => _entity.NestedContainer?.Place(slotKey, item);

        /// <summary>从自身指定槽位移除物品。</summary>
        public void Remove(string slotKey, Entity item)
            => _entity.NestedContainer?.Remove(slotKey, item);

        // ── 装备切换（临时硬编码，后续迁移至装备系统）──

        private static readonly string[] EquipMap = { null, "test_blade", "test_pistol" };

        public void CycleEquip(int equipIndex)
        {
            if (equipIndex < 0 || equipIndex >= EquipMap.Length) return;

            var currentEquipped = _entity.Query.Equipment?.GetEquipped(CharacterConst.Slot.RightHand);
            string targetId = EquipMap[equipIndex];

            if (currentEquipped != null && currentEquipped.Id == targetId) return;
            if (currentEquipped == null && targetId == null) return;

            var backpackEntity = _entity.Query.Equipment?.GetEquipped(CharacterConst.Slot.Back);
            if (backpackEntity == null) return;

            Entity target = null;
            if (targetId != null)
            {
                target = backpackEntity.Query.Inventory.FindItem(targetId);
                if (target == null) return;
            }

            if (currentEquipped != null)
            {
                Remove(CharacterConst.Slot.RightHand, currentEquipped);
                backpackEntity.Command.Place(CharacterConst.Slot.ContainerSlot, currentEquipped);
            }
            if (target != null)
            {
                backpackEntity.Command.Remove(CharacterConst.Slot.ContainerSlot, target);
                Place(CharacterConst.Slot.RightHand, target);
            }
        }

        // ── 输入状态 ──

        internal void SetInputState(SCharacterInputState state)
        {
            var actor = Character;
            if (actor != null) actor.InputState = state;
        }
    }
}
