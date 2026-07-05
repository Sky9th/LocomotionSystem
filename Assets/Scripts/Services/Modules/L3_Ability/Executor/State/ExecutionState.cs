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
            var instance = ctx.Instance;

            // ── ④ Target Resolution: 保证目标 + 物理查询合并去重 ──
            var active = a as ActiveAbilitySO;
            ctx.Targets = ExecuteSearch(active?.search, caster, ctx.Origin, ctx.Direction);

            // caster 自己也是 target（self 走标准路径）
            if (!ctx.Targets.Contains(caster))
                ctx.Targets.Add(caster);

            if (ctx.GuaranteedTargets != null)
            {
                foreach (var t in ctx.GuaranteedTargets)
                {
                    if (t != null && !ctx.Targets.Contains(t))
                        ctx.Targets.Add(t);
                }
            }

            // ── ⑤ BuildDamageInfo → SDamageInfo[]（纯伤害，Buff/Tag 由 Reactor 处理）──
            ctx.Hits = BuildDamageInfo(a, caster, ctx.Targets, ctx.Origin, e, ctx.WeaponEntity, instance);

            // ── ⑥⑦⑧ Per-hit: Resolve 内部完成伤害+Buff+Tag+事件 ──
            if (ctx.Hits != null)
            {
                foreach (var hit in ctx.Hits)
                {
                    if (hit.Target == null) continue;
                    hit.Target.GetComponent<AbilityReactor>()?.Resolve(hit);
                    // OnHit 被动：需等 Reactor→Caster 通知通路建立后，由命中判定方触发
                }
            }

            return new RecoveryState();
        }

        #region Effects (inlined from AbilityEffects)

        /// <summary>构造 SDamageInfo[]，每个 target 一个 hit。Buff/Tag 由 Reactor 处理。</summary>
        // TODO: SDamageInfo 是否有必要存在？伤害公式（武器基底 × 技能修正）本质是 Reactor 的职责，
        // Exe 侧只需要知道 "用哪个技能打了哪个目标"，具体伤害由 Reactor 自己算。
        private static List<SDamageInfo> BuildDamageInfo(
            AbilitySO ability, GameObject caster, List<GameObject> targets,
            Vector3 origin, AbilityExecutor executor, Entity weaponEntity,
            AbilityInstance sourceInstance)
        {
            var hits = new List<SDamageInfo>();
            if (targets == null || targets.Count == 0)
                return hits;

            // 武器伤害基底 & 标签（一次解析，所有 target 共用）
            var weaponEffects = weaponEntity?.Preset?.GetDamageEffects(weaponEntity);
            var damageTags = CollectDamageTags(weaponEffects);

            // ImpactEffectSO（同一 ability 的 targetEffects 中最多一个）
            ImpactEffectSO impactEffect = null;
            var abilityEffects = ability?.targetEffects;
            if (abilityEffects != null)
                foreach (var e in abilityEffects)
                    if (e is ImpactEffectSO imp) { impactEffect = imp; break; }

            foreach (var target in targets)
            {
                if (target == null) continue;

                float amount = target == caster
                    ? 0f   // TODO: self-damage 公式
                    : ComputeDamage(weaponEffects, abilityEffects, target, executor);

                hits.Add(new SDamageInfo(
                    caster, target, amount, damageTags,
                    target.transform.position,
                    target.transform.position - origin,
                    ability,
                    sourceInstance: sourceInstance,
                    impactEffect: impactEffect
                ));
            }

            return hits;
        }

        /// <summary>武器基底 × 技能修正 = 理想伤害。</summary>
        private static float ComputeDamage(
            EffectSO[] weaponEffects, EffectSO[] abilityEffects,
            GameObject target, AbilityExecutor executor)
        {
            if (weaponEffects == null || abilityEffects == null) return 0f;

            float total = 0f;
            foreach (var ae in abilityEffects)
            {
                if (ae is not DamageEffectSO mod) continue;
                foreach (var we in weaponEffects)
                {
                    if (we is not DamageEffectSO wd) continue;
                    float dmg = (wd.baseValue + mod.modAdd) * mod.modMult;
                    if (executor?.EffectCallback != null)
                        dmg = executor.EffectCallback(mod, target, dmg);
                    total += dmg;
                }
            }
            return total;
        }

        private static RdTag[] CollectDamageTags(EffectSO[] weaponEffects)
        {
            if (weaponEffects == null) return null;
            var tags = new List<RdTag>();
            foreach (var e in weaponEffects)
                if (e is DamageEffectSO dmg && dmg.effectTag != null)
                    tags.Add(dmg.effectTag);
            return tags.ToArray();
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
