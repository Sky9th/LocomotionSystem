using RedDust.Container;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Director
{
    internal sealed class PlayerDirector : ModuleChild, ICharacterDirector
    {
        private readonly CharacterBuildContext ctx;
        private PlayerInput input;

        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;

        // 硬编码：1=空手, 2=Blade, 3=Pistol（无 UI 临时方案）
        private static readonly string[] EquipMap = { null, "test_blade", "test_pistol" };

        internal PlayerDirector(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnAssemble()
        {
            input = new PlayerInput(ctx.EventHub);
        }

        public override void OnWire()
        {
            input.BindEvents();
        }

        public SCharacterIntent Evaluate()
        {
            ProcessEquipInput();
            ProcessClickToMove();

            ProcessSkillInput();

            var pathfinding = ctx.Pathfinding;
            bool hasActivePath = pathfinding != null && pathfinding.HasPath && !pathfinding.HasReachedDestination;

            var intent = new SCharacterIntent(
                ComputeHeading(),
                ComputeAim(),
                ResolveGait(),
                ResolvePosture(),
                EBodyForm.Relax,
                false,
                input.FirstSkillRequested,
                input.SecondSkillRequested,
                pathfinding?.DesiredVelocity ?? Vector3.zero,
                hasActivePath);

            if (pathfinding != null && pathfinding.HasPath)
                input.SecondaryRequested = false;
            input.ClearFrameSignals();

            return intent;
        }

        // TODO: 技能槽/物品栏/装备栏系统未完成，暂用 ctx.AbilityForest 直接取。
        private void ProcessSkillInput()
        {
            var slots = ctx.AbilityForest?.ResolvedActives;
            if (slots == null) return;
            if (input.FirstSkillRequested  && slots.Length > 0) TryActivateSkill(slots[0], "Q");
            if (input.SecondSkillRequested && slots.Length > 1) TryActivateSkill(slots[1], "E");
        }

        /// <summary>
        /// Equip 输入处理。1=空手 2=Blade 3=Pistol（无 UI 临时硬编码）。
        /// 从背包 Container 拿武器到手，Equipment.SyncEquipment 负责后续 GO + GripTag 同步。
        ///
        /// TODO: 临时措施。Director 是输入行为层，不应直接操作 Container。
        /// 技能槽/物品栏/装备栏完成后，输入应经装备系统中转，而非 Director 直接 Place/Remove。
        /// NpcDirector 同理——NPC 的装备行为不应走 Container 裸操作。
        /// </summary>
        private void ProcessEquipInput()
        {
            int equipIndex = -1;
            if (input.Equip1Requested) equipIndex = 0;
            else if (input.Equip2Requested) equipIndex = 1;
            else if (input.Equip3Requested) equipIndex = 2;
            if (equipIndex < 0) return;

            var bodyContainer = ctx.CharacterContainer?.BodyContainer;
            if (bodyContainer == null) return;

            var currentEquipped = bodyContainer.GetItem("RightHand");
            string targetId = EquipMap[equipIndex];

            // 已在手上 或 空手→空手 → 跳过
            if (currentEquipped != null && currentEquipped.Id == targetId) return;
            if (currentEquipped == null && targetId == null) return;

            var bpContainer = GetBackpackContainer();
            if (bpContainer == null) { Debug.Log("[ProcessEquipInput] No backpack found."); return; }

            Entity target = null;
            if (targetId != null)
            {
                target = bpContainer.FindItem("ContainerSlot", targetId);
                if (target == null) { Debug.Log($"[ProcessEquipInput] {targetId} not in backpack."); return; }
            }

            Debug.Log($"[ProcessEquipInput] Key {equipIndex + 1}: {(targetId ?? "empty")} ← {(currentEquipped != null ? currentEquipped.Id : "empty")}");

            // 卸当前手持 → 背包
            if (currentEquipped != null)
            {
                bodyContainer.Remove("RightHand", currentEquipped);
                bpContainer.Place("ContainerSlot", currentEquipped);
            }

            // 装目标 → RightHand
            if (target != null)
            {
                bpContainer.Remove("ContainerSlot", target);
                bodyContainer.Place("RightHand", target);
            }
        }

        private Container.Container GetBackpackContainer()
        {
            return ctx.CharacterContainer?.BodyContainer?.GetItem("Back")?.NestedContainer;
        }

        private void ProcessClickToMove()
        {
            var pathfinding = ctx.Pathfinding;
            if (pathfinding == null) return;
            if (!input.SecondaryRequested) return;
            if (!input.HasMouseGround) return;

            pathfinding.SetDestination(input.MouseGroundPosition);
            currentGait = EMovementGait.Run;
        }

        private Vector3 ComputeAim()
        {
            if (input.HasMouseGround)
            {
                var dir = input.MouseGroundPosition - ctx.ModelRoot.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > Mathf.Epsilon)
                    return dir.normalized;
            }
            return ctx.ModelRoot.forward;
        }

        private Vector3 ComputeHeading()
        {
            var pathfinding = ctx.Pathfinding;
            if (pathfinding != null && pathfinding.HasPath)
            {
                var desired = pathfinding.DesiredVelocity;
                if (desired.sqrMagnitude > Mathf.Epsilon)
                    return desired.normalized;
            }
            return ctx.ModelRoot.forward;
        }

        private EMovementGait ResolveGait()
        {
            var pathfinding = ctx.Pathfinding;
            bool hasPath = pathfinding != null && pathfinding.HasPath && !pathfinding.HasReachedDestination;
            bool wantsMove = input.SecondaryRequested || hasPath;

            if (wantsMove)
            {
                if (input.SprintRequested)
                    currentGait = currentGait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;

                if (currentGait == EMovementGait.Idle)
                    currentGait = EMovementGait.Run;
            }
            else
            {
                currentGait = EMovementGait.Idle;
            }

            return currentGait;
        }

        private EPosture ResolvePosture()
        {
            if (input.StandRequested)
                currentPosture = EPosture.Standing;
            else if (input.ProneRequested)
                currentPosture = EPosture.Prone;
            else if (input.CrouchRequested)
                currentPosture = EPosture.Crouching;

            return currentPosture;
        }

        private void TryActivateSkill(RedDust.Ability.ActiveAbilitySO def, string slotName)
        {
            var ability = ctx.Ability;
            if (ability == null)
            {
                Debug.LogWarning("[PlayerDirector] AbilityExecutor is null — skill activation skipped");
                return;
            }
            if (def == null)
            {
                Debug.LogWarning($"[PlayerDirector] {slotName} is empty — skill activation skipped");
                return;
            }

            var weapon = ctx.CharacterContainer?.BodyContainer?.GetItem("RightHand");
            Debug.Log($"[PlayerDirector] Enqueue {slotName}: {def.internalName}" +
                      (weapon != null ? $" | weapon={weapon.Preset.name}" : ""));
            ability.Enqueue(def, ctx.ModelRoot.position, ctx.ModelRoot.forward, weapon);
        }

    }
}
