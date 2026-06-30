using System.Collections.Generic;
using RedDust.Core;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ⛔ DEPRECATED — 逻辑已内联至 <see cref="ExecutionState"/>。旧 AbilityExecutor.TryActivate 仍在引用，该旧代码废弃后删除本类。
    /// </summary>
    /// <remarks>
    /// 伤害来源链：武器 Entity → Preset.GetDamageEffect(entity)
    /// → DamageEffectSO → (baseValue + modAdd) × modMult → SDamageInfo.Amount（理想值，未经防御公式）。
    /// </remarks>
    public class AbilityEffects
    {
        /// <summary>
        /// 对施法者自身施加 selfEffects。
        /// CostEffectSO 已在 ③ 处理，此处跳过。
        /// </summary>
        public void ApplySelf(
            ActiveAbilitySO ability,
            GameObject caster,
            AbilityExecutor executor)
        {
            if (ability?.selfEffects == null) return;

            foreach (var effect in ability.selfEffects)
            {
                if (effect == null || effect is CostEffectSO) continue; // ③ 已处理

                switch (effect)
                {
                    case BuffEffectSO buff:
                        ApplyBuffInternal(buff, caster, executor);
                        break;

                    case DamageEffectSO:
                        // TODO: 武器 Entity → GetDamageEffect → SDamageInfo → self Reactor
                        break;
                }

                // duration > 0 的效果：把 effectTag 写入持有者标签
                ApplyDurationTag(effect, caster, executor);
            }
        }

        /// <summary>
        /// 对目标施加 targetEffects → 构造 SDamageInfo[]。
        /// 伤害 DamageEffectSO 由武器 Entity 决定；非伤害效果直接施加。
        /// </summary>
        public List<SDamageInfo> BuildDamageInfo(
            ActiveAbilitySO ability,
            GameObject caster,
            List<GameObject> targets,
            Vector3 origin,
            AbilityExecutor executor,
            Entity weaponEntity)
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
                            {
                                // (baseValue + modAdd) × modMult — 装备地基 + 技能修正
                                finalDamage = (sourceEffect.baseValue + dmg.modAdd) * dmg.modMult;

                                // IEffectModifier 回调链：外部（力量/熟练度）修改伤害
                                // TODO: finalDamage = executor.EffectCallback?.Invoke(effect, target, finalDamage) ?? finalDamage;
                            }

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

                    // duration > 0 的效果：把 effectTag 写入目标标签
                    ApplyDurationTag(effect, target, executor);
                }
            }

            return hits;
        }

        /// <summary>
        /// 从武器 Entity 解析 DamageEffectSO。
        /// 优先走 Preset.GetDamageEffect（武器子类自行决定伤害来源：近战读自身 ATK，远程沿容器链查弹药）。
        /// Fallback 从 PropertyTable 的 Weapon/ATK 路径直接取。
        /// </summary>
        public DamageEffectSO ResolveDamageEffect(Entity weaponEntity)
        {
            if (weaponEntity?.Preset == null) return null;

            var dmg = weaponEntity.Preset.GetDamageEffect(weaponEntity);
            if (dmg != null) return dmg;

            var effects = weaponEntity.Properties?.GetAssetList<DamageEffectSO>("Weapon/ATK");
            return effects?.Length > 0 ? effects[0] : null;
        }

        #region Internal

        private static void ApplyBuffInternal(BuffEffectSO buff, GameObject target, AbilityExecutor executor)
        {
            if (buff == null || target == null) return;

            // TODO: FloatAdjunct 注入 — PropertyTable.AddAdjunct

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
