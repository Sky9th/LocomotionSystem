using System.Collections.Generic;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⑤ 效果载荷 + 逐 hit 结算。从 ctx.Targets（SearchState 已填充）构造伤害并调用 AbilityReactor 落地。
    /// AbilityEffects 已内联为本类 private static 方法。
    /// 通过 → CooldownState。
    /// </summary>
    public class ExecutionState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Execution;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;
            var caster = e.gameObject;

            // ── ⑤ Self Effects ──
            ApplySelf(a, caster, e);

            // ── ⑤ Target Effects → BuildDamageInfo ──
            ctx.Hits = BuildDamageInfo(a, caster, ctx.Targets, ctx.Origin, e, ctx.WeaponEntity);
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

            Debug.Log($"[Execution] Done: {a.internalName} → Recovery");
            return new RecoveryState();
        }

        #region Effects (inlined from AbilityEffects)

        /// <summary>
        /// 对施法者自身施加 selfEffects。CostEffectSO 已在 ③ 处理，此处跳过。
        /// </summary>
        private static void ApplySelf(ActiveAbilitySO ability, GameObject caster, AbilityExecutor executor)
        {
            if (ability?.selfEffects == null) return;

            foreach (var effect in ability.selfEffects)
            {
                if (effect == null || effect is CostEffectSO) continue;

                switch (effect)
                {
                    case BuffEffectSO buff:
                        ApplyBuffInternal(buff, caster, executor);
                        break;
                    case DamageEffectSO:
                        // TODO: 武器 Entity → GetDamageEffect → SDamageInfo → self Reactor
                        break;
                }

                ApplyDurationTag(effect, caster, executor);
            }
        }

        /// <summary>
        /// 对目标施加 targetEffects → 构造 SDamageInfo[]。
        /// </summary>
        private static List<SDamageInfo> BuildDamageInfo(
            ActiveAbilitySO ability, GameObject caster, List<GameObject> targets,
            Vector3 origin, AbilityExecutor executor, Entity weaponEntity)
        {
            var hits = new List<SDamageInfo>();
            if (ability?.targetEffects == null || targets == null || targets.Count == 0)
                return hits;

            foreach (var target in targets)
            {
                if (target == null) continue;

                foreach (var effect in ability.targetEffects)
                {
                    if (effect == null) continue;

                    switch (effect)
                    {
                        case DamageEffectSO dmg:
                        {
                            float finalDamage = 0f;
                            var sourceEffect = ResolveDamageEffect(weaponEntity);

                            if (sourceEffect != null)
                                finalDamage = (sourceEffect.baseValue + dmg.modAdd) * dmg.modMult;

                            var hit = new SDamageInfo(
                                caster, target, finalDamage,
                                sourceEffect?.effectTag ?? default,
                                target.transform.position,
                                target.transform.position - origin,
                                ability
                            );
                            hits.Add(hit);
                            break;
                        }

                        case BuffEffectSO buff:
                            ApplyBuffInternal(buff, target, executor);
                            break;
                    }

                    ApplyDurationTag(effect, target, executor);
                }
            }

            return hits;
        }

        /// <summary>
        /// 从武器 Entity 解析 DamageEffectSO。
        /// 优先走 Preset.GetDamageEffect，Fallback 从 PropertyTable Weapon/ATK 取。
        /// </summary>
        private static DamageEffectSO ResolveDamageEffect(Entity weaponEntity)
        {
            if (weaponEntity?.Preset == null) return null;

            var dmg = weaponEntity.Preset.GetDamageEffect(weaponEntity);
            if (dmg != null) return dmg;

            var effects = weaponEntity.Properties?.GetAssetList<DamageEffectSO>("Weapon/ATK");
            return effects?.Length > 0 ? effects[0] : null;
        }

        private static void ApplyBuffInternal(BuffEffectSO buff, GameObject target, AbilityExecutor executor)
        {
            if (buff == null || target == null) return;

            if (buff.grantedTags != null && buff.grantedTags.Length > 0)
            {
                var targetExecutor = target.GetComponent<AbilityExecutor>();
                if (targetExecutor != null)
                {
                    foreach (var tag in buff.grantedTags)
                        targetExecutor.OwnedTags.AddTag(tag);

                    if (buff.duration > 0f)
                        targetExecutor.AddBuffTags(buff.grantedTags, Time.time + buff.duration);
                }
            }
        }

        private static void ApplyDurationTag(EffectSO effect, GameObject target, AbilityExecutor executor)
        {
            if (effect.duration <= 0f || effect.effectTag == null) return;

            if (target == executor.gameObject)
                executor.OwnedTags.AddTag(effect.effectTag.FullTag);
            else
                target.GetComponent<Identity>()?.Tags.AddTag(effect.effectTag.FullTag);
        }

        #endregion
    }
}
