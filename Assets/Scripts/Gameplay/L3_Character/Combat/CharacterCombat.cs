using RedDust.Ability;
using RedDust.Character.Animation;
using RedDust.Character.Animation.Drivers.HitReaction;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 战斗中枢。桥接 Ability 管道与 Character 属性系统。
    /// </summary>
    internal sealed class CharacterCombat : ModuleChild
    {
        private readonly CharacterBuildContext ctx;

        internal CharacterCombat(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnWire()
        {
            if (ctx.Ability != null)
            {
                ctx.Ability.OutgoingDamageCallback = OnModifyOutgoingDamage;
                ctx.Ability.OnHitResolved = OnHitResolved;
            }

            if (ctx.Reactor != null)
            {
                ctx.Reactor.ResolutionCallback = OnResolveDamage;
                ctx.Reactor.ApplyDamageCallback = OnApplyDamage;
                ctx.Reactor.ReactionCallback = OnReaction;
                ctx.Reactor.OnDamagedCallback = OnDamaged;
            }

            ctx.EventHub?.Get<HitEvent>()?.Register(OnHitEvent);
        }

        public void UnsubscribeEvents()
        {
            ctx.EventHub?.Get<HitEvent>()?.Unregister(OnHitEvent);
        }

        private void OnHitEvent(SDamageInfo hit) { }

        #region 修改器占位

        private float OnModifyOutgoingDamage(EffectSO effect, GameObject target, float outgoingDamage)
        {
            float strength = ctx.Properties.GetFloat(CharacterConst.PropertyPath.Attributes.Strength);
            var config = ctx.GroundSystemConfig;
            if (strength > 0f && config != null)
                outgoingDamage *= 1f + strength * config.strengthDamageBonus;
            return outgoingDamage;
        }

        private void OnHitResolved(SDamageInfo hit, float finalAmount)
        {
            if (finalAmount > 0f && ctx.Ability != null)
                ctx.Ability.NotifyPassiveEvent(ETriggerEvent.OnHit, hit.Target);
        }

        /// <summary>承受方结算管线。Avoidance → Mitigation → Absorption。
        /// 当前对 TotalAmount 统一减免，后续改为 per-DamageEntry 按 tag 路由抗性。</summary>
        private float OnResolveDamage(SDamageInfo hit)
        {
            float amount = hit.TotalAmount;
            float incoming = amount;

            var endurance = ctx.Properties.GetFloat(CharacterConst.PropertyPath.Attributes.Endurance);
            if (endurance > 0f)
                amount *= 1f - endurance * 0.05f;

            if (amount != incoming)
                Debug.Log($"[Combat] {hit.Target.name} Mitigation: {incoming:F1} → {amount:F1} (endurance={endurance:F1})");

            // TODO: 回避判定（闪避率）— 阻塞：闪避属性/装备系统未就位
            // TODO: 吸收结算（护盾）— 阻塞：护盾系统未设计
            // TODO: per-DamageEntry.Tag 路由抗性 — 当前统一 Endurance 减免，后续按 Slash/Pierce/Fire 分别查抗性

            return amount;
        }

        /// <summary>伤害落地。直接写入 HP。</summary>
        private void OnApplyDamage(SDamageInfo hit, float finalAmount)
        {
            var before = ctx.Properties.GetFloat(CharacterConst.PropertyPath.Vitals.HP);
            ctx.Properties.Modify(CharacterConst.PropertyPath.Vitals.HP, -finalAmount);
            Debug.Log($"[Combat] {hit.Target.name} HP: {before:F1} -{finalAmount:F1} → {ctx.Properties.GetFloat(CharacterConst.PropertyPath.Vitals.HP):F1}");

            // OnKill 被动触发：受害者 HP 归零 → 通知击杀者的 AbilityExecutor
            if (before > 0f && ctx.Properties.GetFloat(CharacterConst.PropertyPath.Vitals.HP) <= 0f)
            {
                var casterAbility = hit.Caster != null ? hit.Caster.GetComponent<AbilityExecutor>() : null;
                if (casterAbility != null)
                    casterAbility.NotifyPassiveEvent(ETriggerEvent.OnKill, hit.Target);
            }
        }

        private void OnReaction(SDamageInfo hit, float finalAmount)
        {
            var impact = hit.ImpactEffect;
            if (impact == null) return;
            // TODO: 霸体阈值判定（staggerValue vs 自身霸体值）— 阻塞：霸体值属性体系未建立

            var locoSet = ctx.ResolvedLocoAnimSet;
            if (locoSet == null) return;

            // 受击等级由 ImpactEffectSO 资产决定
            var mixer = impact.reactionLevel switch
            {
                EHitReactionLevel.Stagger   => locoSet.hitReactionStagger,
                EHitReactionLevel.Knockdown => locoSet.hitReactionKnockdown,
                _                           => locoSet.hitReactionFlinch,  // Flinch default
            };

            // HitDirection → 本地空间
            var (localX, localY) = WorldToLocalDirection(hit.HitDirection, ctx.Root);

            var request = new AnimationRequest
            {
                DriverType = EDriverType.HitReaction,
                Resistance = Mathf.CeilToInt(impact.staggerValue),
                FadeIn = 0.1f,
                CustomData = new SHitReactionData { Mixer = mixer, DirX = localX, DirY = localY },
            };

            // Knockdown → 击倒动画结束后链式提交起身
            if (impact.reactionLevel == EHitReactionLevel.Knockdown)
                request.OnCompleted = _ => ChainGetUp();

            ctx.Animation?.SubmitRequest(request);
        }

        private void OnDamaged(SDamageInfo hit, float finalAmount)
        {
            // OnDamaged 被动触发：承受方自身（仅当承受方有 AbilityExecutor 时）
            if (ctx.Ability != null)
                ctx.Ability.NotifyPassiveEvent(ETriggerEvent.OnDamaged, hit.Target);

            float hp = ctx.Properties.GetFloat(CharacterConst.PropertyPath.Vitals.HP);
            if (hp <= 0f)
            {
                var locoSet = ctx.ResolvedLocoAnimSet;
                if (locoSet == null) return;

                var request = new AnimationRequest
                {
                    DriverType = EDriverType.HitReaction,
                    Resistance = int.MaxValue,  // 死亡不可打断
                    FadeIn = 0.1f,
                    CustomData = new SHitReactionData
                    {
                        Mixer = locoSet.hitReactionKnockdown,
                        DirX = 0f,
                        DirY = -1f
                    },
                    // 无 OnCompleted → 不起身，停在倒地 pose
                };

                ctx.Animation?.SubmitRequest(request);
            }
        }

        private void ChainGetUp()
        {
            var locoSet = ctx.ResolvedLocoAnimSet;
            if (locoSet == null) return;

            var request = new AnimationRequest
            {
                DriverType = EDriverType.HitReaction,
                Resistance = 0,  // 可被任何受击打断
                FadeIn = 0.2f,
                CustomData = new SHitReactionData
                {
                    Mixer = locoSet.hitReactionGetUp,
                    DirX = 0f,
                    DirY = 1f
                },
            };

            ctx.Animation?.SubmitRequest(request);
        }

        /// <summary>世界空间方向 → 角色本地空间 (X=右, Y=前)。</summary>
        private static (float x, float y) WorldToLocalDirection(Vector3 worldDir, Transform root)
        {
            Vector3 local = root.InverseTransformDirection(worldDir);
            return (-local.x, -local.z);  // 反向：HitDirection 是伤害飞行方向，受击反应需要冲击来向
        }

        #endregion
    }
}
