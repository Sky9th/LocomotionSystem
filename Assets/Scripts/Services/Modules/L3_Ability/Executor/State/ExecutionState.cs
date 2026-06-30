using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⑤ 效果载荷 + 逐 hit 结算。从 ctx.Targets（SearchState 已填充）构造伤害并调用 AbilityReactor 落地。
    /// 通过 → CompletedState（TODO: → CooldownState）。
    /// </summary>
    public class ExecutionState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Execution;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;
            var caster = e.gameObject;
            var effects = new AbilityEffects();

            // ── ⑤ Self Effects ──
            effects.ApplySelf(a, caster, e);

            // ── ⑤ Target Effects → BuildDamageInfo ──
            ctx.Hits = effects.BuildDamageInfo(a, caster, ctx.Targets, ctx.Origin, e, ctx.WeaponEntity);
            Debug.Log($"[Execution] ⑤ Effects: {a.internalName} self+target, hits={ctx.Hits?.Count ?? 0}");

            // ── ⑥⑦⑧ Per-hit Resolve ──
            if (ctx.Hits != null)
            {
                foreach (var hit in ctx.Hits)
                {
                    if (hit.Target == null) continue;
                    var reactor = hit.Target.GetComponent<AbilityReactor>();
                    if (reactor != null)
                        reactor.Resolve(hit);
                    else
                        Debug.LogWarning($"[Execution] Target '{hit.Target.name}' has no AbilityReactor — hit not resolved.");
                }
            }

            Debug.Log($"[Execution] Done: {a.internalName} → Completed");
            return new CompletedState(); // TODO: → CooldownState
        }
    }
}
