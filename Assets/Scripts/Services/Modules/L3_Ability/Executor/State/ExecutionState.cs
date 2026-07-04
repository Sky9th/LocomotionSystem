using System.Collections.Generic;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⑤ 效果载荷 + 逐 hit 结算。Fire 帧物理查询（Cone/Ray/Circle → ctx.Targets）→ 构造伤害 → Reactor 落地。
    /// AbilitySearch 和 AbilityEffects 均已内联。SearchState ⛔ DEPRECATED。
    /// 通过 → RecoveryState。
    ///
    /// TODO Phase 4.2: fireWindowDuration 多帧命中窗口 — OnTick 循环在 fireWindowDuration 内每帧执行物理查询
    /// </summary>
    public class ExecutionState : AbilityPipelineState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.Execution;

        public override IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;
            var caster = e.gameObject;

            // ── Fire 帧物理查询（内联自 SearchState ⛔ DEPRECATED）──
            ctx.Targets = ExecuteSearch(a.search, caster, ctx.Origin, ctx.Direction);

            // ── ⑤ Self Effects ──
            ApplySelf(a, caster, e);

            // ── ⑤ Target Effects → BuildDamageInfo ──
            ctx.Hits = BuildDamageInfo(a, caster, ctx.Targets, ctx.Origin, e, ctx.WeaponEntity);

            // ── ⑥⑦⑧ Per-hit Resolve ──
            if (ctx.Hits != null)
            {
                foreach (var hit in ctx.Hits)
                {
                    if (hit.Target == null) continue;
                    var reactor = hit.Target.GetComponent<AbilityReactor>();
                    if (reactor != null)
                        reactor.Resolve(hit);
                }
            }

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

                            // ⑤ EffectCallback: 外部伤害修正（力量/熟练度/被动）
                            if (executor.EffectCallback != null)
                                finalDamage = executor.EffectCallback(dmg, target, finalDamage);

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

        #region Search (inlined from SearchState ⛔ DEPRECATED)

        private static List<GameObject> ExecuteSearch(AbilitySearchSO search, GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (search == null || caster == null) return new List<GameObject>();

            return search switch
            {
                ConeSearchSO cone     => SearchCone(cone, caster, origin, direction),
                RaySearchSO ray       => SearchRay(ray, caster, origin, direction),
                CircleSearchSO circle => SearchCircle(circle, caster, origin),
                _                     => new List<GameObject>()
            };
        }

        private static List<GameObject> SearchCone(ConeSearchSO cone, GameObject caster, Vector3 origin, Vector3 direction)
        {
            var results = new List<GameObject>();
            int max = cone.maxTargets > 0 ? cone.maxTargets : int.MaxValue;
            var hits = Physics.OverlapSphere(origin, cone.range, cone.targetMask);
            float halfAngle = cone.angle * 0.5f;

            for (int i = 0; i < hits.Length && results.Count < max; i++)
            {
                var go = hits[i].gameObject;
                if (go == caster) continue;
                if (go.GetComponent<Identity>() == null) continue;

                var toTarget = hits[i].transform.position - origin;
                if (Vector3.Angle(direction, toTarget) > halfAngle) continue;

                if (!results.Contains(go))
                    results.Add(go);
            }

            return results;
        }

        private static List<GameObject> SearchRay(RaySearchSO ray, GameObject caster, Vector3 origin, Vector3 direction)
        {
            var results = new List<GameObject>();

            if (Physics.Raycast(origin, direction, out var hit, ray.range, ray.targetMask))
            {
                var go = hit.collider.gameObject;
                if (go != caster && go.GetComponent<Identity>() != null)
                    results.Add(go);
            }

            return results;
        }

        private static List<GameObject> SearchCircle(CircleSearchSO circle, GameObject caster, Vector3 origin)
        {
            var results = new List<GameObject>();
            int max = circle.maxTargets > 0 ? circle.maxTargets : int.MaxValue;
            var hits = Physics.OverlapSphere(origin, circle.range, circle.targetMask);

            for (int i = 0; i < hits.Length && results.Count < max; i++)
            {
                var go = hits[i].gameObject;
                if (go == caster) continue;
                if (go.GetComponent<Identity>() == null) continue;

                if (!results.Contains(go))
                    results.Add(go);
            }

            return results;
        }

        #endregion
    }
}
