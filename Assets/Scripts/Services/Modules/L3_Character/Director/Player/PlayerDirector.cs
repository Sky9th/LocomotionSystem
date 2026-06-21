using RedDust.Core;
using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Director
{
    internal sealed class PlayerDirector : Module, ICharacterDirector
    {
        private readonly CharacterBuildContext ctx;
        private PlayerInput input;

        private EMovementGait currentGait = EMovementGait.Idle;
        private EPosture currentPosture = EPosture.Standing;
        private EBodyForm currentBodyForm = EBodyForm.Relax;

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

        private int debugGripIndex = 0;

        public SCharacterIntent Evaluate()
        {
            ProcessDebugGripSwitch();
            ProcessDebugCombatToggle();
            ProcessClickToMove();

            // TODO: 临时方案 — 直接读 Actor 槽位。技能树/装备系统完成后由 AbilitySlotManager 替代。
            if (input.FirstSkillRequested)
            {
                var ability = ctx.Ability;
                var def = ctx.SkillSlot1;
                if (ability == null)
                    Debug.LogWarning("[PlayerDirector] AbilityExecutor is null — skill activation skipped");
                else if (def == null)
                    Debug.LogWarning("[PlayerDirector] SkillSlot1 is empty — skill activation skipped");
                else
                {
                    Debug.Log($"[PlayerDirector] Activating SkillSlot1: {def.internalName}");
                    ability.TryActivate(def, ctx.ModelRoot.position, ctx.ModelRoot.forward);
                }
            }
            if (input.SencondSkillRequested)
            {
                var ability = ctx.Ability;
                var def = ctx.SkillSlot2;
                if (ability == null)
                    Debug.LogWarning("[PlayerDirector] AbilityExecutor is null — skill activation skipped");
                else if (def == null)
                    Debug.LogWarning("[PlayerDirector] SkillSlot2 is empty — skill activation skipped");
                else
                {
                    Debug.Log($"[PlayerDirector] Activating SkillSlot2: {def.internalName}");
                    ability.TryActivate(def, ctx.ModelRoot.position, ctx.ModelRoot.forward);
                }
            }

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
                input.SencondSkillRequested,
                pathfinding?.DesiredVelocity ?? Vector3.zero,
                hasActivePath);

            if (pathfinding != null && pathfinding.HasPath)
                input.SecondaryRequested = false;
            input.ClearFrameSignals();

            return intent;
        }

        // TODO(debug): 临时 grip 切换，仅用于测试。之后改为 EventHub 事件驱动：
        //   PlayerDirector 发布 GripSwitchEvent → CharacterActor 订阅 → 更新 OwnedTags
        private void ProcessDebugGripSwitch()
        {
            var table = ctx.GripTable;
            if (table == null || table.entries == null || table.entries.Length == 0) return;
            var ownedTags = ctx.Ability?.OwnedTags;
            if (ownedTags == null) return;

            int newIndex = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1)) newIndex = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2) && table.entries.Length > 1) newIndex = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha3) && table.entries.Length > 2) newIndex = 2;

            if (newIndex < 0 || newIndex == debugGripIndex) return;

            // 清除旧 grip tag
            for (int i = 0; i < table.entries.Length; i++)
                if (table.entries[i].gripTag != null)
                    ownedTags.RemoveTag(table.entries[i].gripTag.FullTag);

            // 添加新 grip tag
            var entry = table.entries[newIndex];
            if (entry.gripTag != null)
            {
                ownedTags.AddTag(entry.gripTag.FullTag);
                debugGripIndex = newIndex;
                Debug.Log($"[PlayerDirector] Grip switched to: {entry.gripTag.FullTag}");
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

        // TODO(debug): 临时 BodyForm 切换，仅用于测试。
        private void ProcessDebugCombatToggle()
        {
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                currentBodyForm = EBodyForm.Combat;
                Debug.Log("[PlayerDirector] BodyForm → Combat");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                currentBodyForm = EBodyForm.Relax;
                Debug.Log("[PlayerDirector] BodyForm → Relax");
            }
        }

        private EBodyForm ResolveBodyForm()
        {
            return currentBodyForm;
        }
    }
}
