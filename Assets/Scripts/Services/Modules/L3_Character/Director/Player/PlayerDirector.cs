using RedDust.Core;
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

        /// <summary>Equip1→[0], Equip2→[1], Equip3→[2]。GripTable entries 按序对应。</summary>
        private readonly bool[] equippedSlots = new bool[3];

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
                ResolveBodyForm(),
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

        private void ProcessSkillInput()
        {
            if (input.FirstSkillRequested) TryActivateSkill(ctx.SkillSlot1, "SkillSlot1");
            if (input.SecondSkillRequested) TryActivateSkill(ctx.SkillSlot2, "SkillSlot2");
        }

        /// <summary>
        /// Equip 输入处理。Equip1→entries[0], Equip2→entries[1], Equip3→entries[2]。
        /// TODO: 当前 Director 直接写 OwnedTags 是过渡方案。装备系统完成后由 GripSwitchEvent 替代。
        /// </summary>
        private void ProcessEquipInput()
        {
            var table = ctx.GripTable;
            if (table == null || table.entries == null || table.entries.Length == 0) return;
            var ownedTags = ctx.OwnedGripTags;

            for (int i = 0; i < equippedSlots.Length; i++)
            {
                bool requested = i switch
                {
                    0 => input.Equip1Requested,
                    1 => input.Equip2Requested,
                    2 => input.Equip3Requested,
                    _ => false
                };
                if (!requested) continue;

                if (i >= table.entries.Length) return;
                var entry = table.entries[i];
                if (entry.gripTag == null) return;

                if (equippedSlots[i])
                {
                    // 卸下
                    ownedTags.RemoveTag(entry.gripTag.FullTag);
                    equippedSlots[i] = false;
                    Debug.Log($"[PlayerDirector] Unequipped slot {i}: {entry.gripTag.FullTag}");
                }
                else
                {
                    // 清除所有已有 grip tag，装备新 slot（武器互斥）
                    for (int j = 0; j < table.entries.Length; j++)
                    {
                        if (table.entries[j].gripTag != null)
                            ownedTags.RemoveTag(table.entries[j].gripTag.FullTag);
                        equippedSlots[j] = false;
                    }
                    ownedTags.AddTag(entry.gripTag.FullTag);
                    equippedSlots[i] = true;
                    Debug.Log($"[PlayerDirector] Equipped slot {i}: {entry.gripTag.FullTag}");
                }
                break; // 每帧只处理一个 Equip 输入
            }
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

        // TODO: 临时方案 — 直接读 Actor 槽位。技能树/装备系统完成后由 AbilitySlotManager 替代。
        private void TryActivateSkill(RedDust.Ability.AbilityDefSO def, string slotName)
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
            Debug.Log($"[PlayerDirector] Activating {slotName}: {def.internalName}");
            ability.TryActivate(def, ctx.ModelRoot.position, ctx.ModelRoot.forward);
        }

        /// <summary>任意 slot 装备 → Combat，否则 Relax</summary>
        private EBodyForm ResolveBodyForm()
        {
            for (int i = 0; i < equippedSlots.Length; i++)
                if (equippedSlots[i])
                    return EBodyForm.Combat;
            return EBodyForm.Relax;
        }
    }
}
