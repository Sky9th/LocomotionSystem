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

        internal PlayerDirector(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnAssemble()
        {
            input = new PlayerInput(ctx.EventHub);
            ctx.EventHub.RegisterListener(input);
        }

        public SCharacterIntent Evaluate()
        {
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
    }
}
