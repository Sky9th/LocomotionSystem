using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ③ 资源消耗。双阶段：预检（全部可负担?）→ 扣除。
    /// 预检失败 → RejectedState；通过 → CompletedState（TODO: → ExecutionState）。
    /// </summary>
    public class CostState : AbilityState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Cost;

        public override IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;

            if (a.selfEffects == null)
            {
                Debug.Log($"[Cost] No selfEffects — skip. → Execute");
                return new CompletedState(); // TODO: → ExecutionState
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

            // 有冷却时挂 abilityTag
            if (a.cooldownDuration > 0f && a.abilityTag != null)
                e.OwnedTags.AddTag(a.abilityTag.FullTag);

            Debug.Log($"[Cost] Deducted: {a.internalName} → Execute");
            return new CompletedState(); // TODO: → ExecutionState
        }
    }
}
