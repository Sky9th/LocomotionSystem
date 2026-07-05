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
                    var reactor = hit.Target.GetComponent<AbilityReactor>();
                    if (reactor != null)
                    {
                        float finalAmount = reactor.Resolve(hit);
                        e.OnHitResolved?.Invoke(hit, finalAmount);
                    }
                }
            }

            return new RecoveryState();
        }

        #region Effects

        /// <summary>构造 SDamageInfo，每个 target 一个 hit，Damage[] 按通道分解。</summary>
        private static List<SDamageInfo> BuildDamageInfo(
            AbilitySO ability, GameObject caster, List<GameObject> targets,
            Vector3 origin, AbilityExecutor executor, Entity weaponEntity,
            AbilityInstance sourceInstance)
        {
            var hits = new List<SDamageInfo>();
            if (targets == null || targets.Count == 0)
                return hits;

            // ── ① 收集实体伤害通道（武器 + 身体 + 技能自身 targetEffects 中的 DamageEffectSO）──
            var entityChannels = CollectEntityChannels(weaponEntity, caster, ability);

            // ── ② 收集技能伤害修正 ──
            var modifiers = CollectDamageModifiers(ability);

            // ── ③ ImpactEffectSO（targetEffects 中最多一个）──
            ImpactEffectSO impactEffect = null;
            var abilityEffects = ability?.targetEffects;
            if (abilityEffects != null)
                foreach (var e in abilityEffects)
                    if (e is ImpactEffectSO imp) { impactEffect = imp; break; }

            foreach (var target in targets)
            {
                if (target == null) continue;

                // ── ④ 对每个实体通道，匹配技能修正 → DamageEntry[] ──
                var entries = new List<DamageEntry>(entityChannels.Length);
                foreach (var channel in entityChannels)
                {
                    float baseVal = channel.baseValue;
                    float percentSum = 0f;
                    float addSum = 0f;

                    foreach (var mod in modifiers)
                    {
                        if (MatchTag(mod.targetTag, channel.effectTag))
                        {
                            percentSum += mod.modPercent;
                            addSum += mod.modAdd;
                        }
                    }

                    float amount = baseVal * (1f + percentSum) + addSum;

                    // ── caster-side 属性修正（力量/穿透等施展方属性）──
                    if (executor?.OutgoingDamageCallback != null)
                        amount = executor.OutgoingDamageCallback(null, target, amount);

                    // self-damage: 对自身伤害暂为 0
                    if (target == caster)
                        amount = 0f;

                    entries.Add(new DamageEntry(
                        channel.effectTag,
                        amount,
                        channel.duration,
                        0f));
                }

                hits.Add(new SDamageInfo(
                    caster, target, entries.ToArray(),
                    target.transform.position,
                    target.transform.position - origin,
                    ability,
                    sourceInstance: sourceInstance,
                    impactEffect: impactEffect
                ));
            }

            return hits;
        }

        /// <summary>从实体 + 技能收集伤害通道。武器 + 身体（空手 fallback）+ 技能 targetEffects 中的 DamageEffectSO。</summary>
        private static DamageEffectSO[] CollectEntityChannels(Entity weaponEntity, GameObject caster, AbilitySO ability)
        {
            var channels = new List<DamageEffectSO>();

            // 武器通道
            var weaponEffects = weaponEntity?.Preset?.GetDamageEffects(weaponEntity);
            if (weaponEffects != null)
            {
                foreach (var e in weaponEffects)
                    if (e is DamageEffectSO de)
                        channels.Add(de);
            }

            // 技能 targetEffects 中的 DamageEffectSO（被动技能的主要伤害来源）
            var abilityEffects = ability?.targetEffects;
            if (abilityEffects != null)
            {
                foreach (var e in abilityEffects)
                    if (e is DamageEffectSO de)
                        channels.Add(de);
            }

            // TODO: 身体通道 (Body/Unarmed) — 空手战斗
            // caster.GetComponent<Identity>()?.Properties?.GetAssetList<DamageEffectSO>("Body/ATK")

            return channels.ToArray();
        }

        /// <summary>从技能 targetEffects 中收集 DamageModifierEffectSO。</summary>
        private static List<DamageModifierEffectSO> CollectDamageModifiers(AbilitySO ability)
        {
            var modifiers = new List<DamageModifierEffectSO>();
            var effects = ability?.targetEffects;
            if (effects == null) return modifiers;

            foreach (var e in effects)
                if (e is DamageModifierEffectSO mod)
                    modifiers.Add(mod);

            return modifiers;
        }

        /// <summary>Tag 层级匹配。modifier 是 channel 的祖先（同级或上级）则匹配。
        /// 例：modifier=Damage.Physical.Slash 匹配 channel=Damage.Physical.Slash.Heavy。</summary>
        private static bool MatchTag(RdTagDefSO modifierTag, RdTagDefSO channelTag)
        {
            if (modifierTag == null || channelTag == null) return false;
            if (modifierTag == channelTag) return true;
            RdTag m = modifierTag;
            RdTag c = channelTag;
            return m.Equals(c) || m.IsAncestorOf(c);
        }

        /// <summary>实体通道 × 技能修正 = outgoing 伤害。
        /// 公式：baseValue × (1 + ΣmodPercent) + ΣmodAdd
        /// 百分比加法叠加，避免 ×3×4 爆炸。</summary>
        private static float ComputeOutgoingDamage(DamageEffectSO channel, List<DamageModifierEffectSO> modifiers)
        {
            float baseVal = channel.baseValue;
            float percentSum = 0f;
            float addSum = 0f;

            foreach (var mod in modifiers)
            {
                if (MatchTag(mod.targetTag, channel.effectTag))
                {
                    percentSum += mod.modPercent;
                    addSum += mod.modAdd;
                }
            }

            return baseVal * (1f + percentSum) + addSum;
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
