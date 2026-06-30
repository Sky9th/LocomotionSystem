using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ④ 资源消耗。双阶段：预检（全部可负担?）→ 扣除。
    /// 预检失败 → RejectedState；通过 → ExecutionState。
    /// </summary>
    public class CostState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Cost;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;

            if (a.selfEffects == null)
            {
                Debug.Log($"[Cost] No selfEffects — skip. → Execute");
                return new ExecutionState();
            }

            // ── Phase 1: 预检 ──
            foreach (var effect in a.selfEffects)
            {
                if (effect is not CostEffectSO cost || cost.def == null) continue;

                if (e.PeekStatCallback == null)
                {
                    Debug.LogError($"[Cost] PeekStatCallback is null — rejected.");
                    return new RejectedState();
                }

                var current = e.PeekStatCallback(cost.def);
                if (current < cost.amount)
                {
                    Debug.LogWarning($"[Cost] Rejected: {a.internalName} — insufficient {cost.def.Id} (need {cost.amount}, have {current:F1})");
                    return new RejectedState();
                }
            }

            // ── Phase 2: 扣除 ──
            foreach (var effect in a.selfEffects)
            {
                if (effect is not CostEffectSO cost || cost.def == null) continue;
                e.ModifyStatCallback?.Invoke(cost.def, -cost.amount);
            }

            // abilityTag 冷却互斥由 CooldownState 统一管理（AddCooldown + cooldownAbilityTags + 清理）
            Debug.Log($"[Cost] Deducted: {a.internalName} → Execute");
            return new ExecutionState();
        }
    }
}
