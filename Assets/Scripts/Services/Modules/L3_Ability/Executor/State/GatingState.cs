using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ② 门控检查。冷却 → 互斥 → 外部条件，三道闸门串联。
    /// 通过 → CostState；失败 → RejectedState。Search 已并入 Execution。
    /// </summary>
    public class GatingState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Gating;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;
            var active = a as ActiveAbilitySO; // Passive 时 safe cast 返回 null

            // ── 1. 独立冷却 ──
            if (a.cooldownDuration > 0f && a.abilityTag != null && e.IsOnCooldown(a.abilityTag.FullTag))
            {
                Debug.LogWarning($"[Gating] Rejected: {a.internalName} — on cooldown");
                return new RejectedState();
            }

            // ── 2. 联动冷却 ──
            if (e.IsBlockedBySharedCooldown(a.sharedCooldownTags))
            {
                Debug.LogWarning($"[Gating] Rejected: {a.internalName} — shared cooldown active");
                return new RejectedState();
            }

            // ── 3. 互斥（仅 ActiveAbilitySO 有此字段）──
            if (active != null && !active.overrideExclusion && active.extraExclusionTags != null)
            {
                foreach (var tag in active.extraExclusionTags)
                {
                    if (tag != null && e.OwnedTags.HasTag(tag.FullTag))
                    {
                        Debug.LogWarning($"[Gating] Rejected: {a.internalName} — extra exclusion ({tag.FullTag})");
                        return new RejectedState();
                    }
                }
            }

            // ── 4. 外部条件 ──
            if (e.GatingConditionCallback != null)
            {
                var reason = e.GatingConditionCallback(a);
                if (reason != null)
                {
                    Debug.LogWarning($"[Gating] Rejected: {a.internalName} — condition: {reason}");
                    return new RejectedState();
                }
            }

            return new CostState();
        }

    }
}
