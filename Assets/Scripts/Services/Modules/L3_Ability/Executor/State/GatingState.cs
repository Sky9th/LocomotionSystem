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

            // ── 1. 独立冷却 ──
            if (a.cooldownDuration > 0f && a.abilityTag != null && e.IsOnCooldown(a.abilityTag.FullTag))
            {
                Debug.LogWarning($"[Gating] Rejected: {a.internalName} — on cooldown");
                return new RejectedState();
            }

            // ── 2. 联动冷却 — sharedCooldownTag 或其父级任一在冷却中即拒绝 ──
            if (e.IsBlockedBySharedCooldown(a.sharedCooldownTag))
            {
                Debug.LogWarning($"[Gating] Rejected: {a.internalName} — shared cooldown active ({a.sharedCooldownTag?.FullTag})");
                return new RejectedState();
            }

            // ── 2. 互斥 ──
            if (!a.overrideExclusion)
            {
                if (a.abilityTag?.Parent != null && e.OwnedTags.HasTag(a.abilityTag.Parent.FullTag))
                {
                    Debug.LogWarning($"[Gating] Rejected: {a.internalName} — mutual exclusion ({a.abilityTag.Parent.FullTag})");
                    return new RejectedState();
                }

                if (a.extraExclusionTags != null)
                {
                    foreach (var tag in a.extraExclusionTags)
                    {
                        if (tag != null && e.OwnedTags.HasTag(tag.FullTag))
                        {
                            Debug.LogWarning($"[Gating] Rejected: {a.internalName} — extra exclusion ({tag.FullTag})");
                            return new RejectedState();
                        }
                    }
                }
            }

            // ── 3. 外部条件 ──
            if (e.ConditionCallback != null)
            {
                var reason = e.ConditionCallback(a);
                if (reason != null)
                {
                    Debug.LogWarning($"[Gating] Rejected: {a.internalName} — condition: {reason}");
                    return new RejectedState();
                }
            }

            Debug.Log($"[Gating] Passed: {a.internalName} → Cost");
            return new CostState();
        }

    }
}
