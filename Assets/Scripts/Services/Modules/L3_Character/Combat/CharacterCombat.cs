using RedDust.Ability;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 战斗中枢。桥接 Ability 管道与 Character 属性系统。
    /// </summary>
    internal sealed class CharacterCombat : Module
    {
        private readonly CharacterBuildContext ctx;

        internal CharacterCombat(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        public override void OnAssemble()
        {
            if (ctx.Ability != null)
            {
                ctx.Ability.EffectCallback = OnEffectModify;
                ctx.Ability.PeekStatCallback = OnPeekStat;
                ctx.Ability.ModifyStatCallback = OnModifyStat;
            }

            if (ctx.Reactor != null)
            {
                ctx.Reactor.ResolutionCallback = OnResolveDamage;
                ctx.Reactor.ApplyDamageCallback = OnApplyDamage;
                ctx.Reactor.ReactionCallback = OnReaction;
                ctx.Reactor.OnDamagedCallback = OnDamaged;
            }
        }

        public override void OnWire()
        {
            ctx.EventHub?.Get<HitEventSO>()?.Register(OnHitEvent);
        }

        public void UnsubscribeEvents()
        {
            ctx.EventHub?.Get<HitEventSO>()?.Unregister(OnHitEvent);
        }

        private void OnHitEvent(SDamageInfo hit) { }

        #region 修改器占位

        private float OnPeekStat(PropertyDefSO def)
        {
            return ctx.Agent.GetFloat(def.Id);
        }

        private void OnModifyStat(PropertyDefSO def, float delta)
        {
            ctx.Agent.Modify(def.Id, delta);
        }

        private float OnEffectModify(EffectSO effect, GameObject target, float baseDamage)
        {
            // TODO: Phase 4.2 — 施展方属性修正（力量/穿透/暴击）
            return baseDamage;
        }

        /// <summary>承受方结算管线。Avoidance → Mitigation → Absorption。</summary>
        private float OnResolveDamage(SDamageInfo hit)
        {
            float amount = hit.Amount;
            float incoming = amount;

            var endurance = ctx.Agent.GetFloat("Attributes/Endurance");
            if (endurance > 0f)
                amount *= 1f - endurance * 0.05f;

            if (amount != incoming)
                Debug.Log($"[Combat] {hit.Target.name} Mitigation: {incoming:F1} → {amount:F1} (endurance={endurance:F1})");

            // TODO: Phase 4.2 — 回避判定（闪避率）
            // TODO: Phase 4.2 — 吸收结算（护盾）

            return amount;
        }

        /// <summary>伤害落地。直接写入 HP。</summary>
        private void OnApplyDamage(SDamageInfo hit, float finalAmount)
        {
            var before = ctx.Agent.GetFloat("Vitals/HP");
            ctx.Agent.Modify("Vitals/HP", -finalAmount);
            Debug.Log($"[Combat] {hit.Target.name} HP: {before:F1} -{finalAmount:F1} → {ctx.Agent.GetFloat("Vitals/HP"):F1}");
        }

        private void OnReaction(SDamageInfo hit, float finalAmount) { }
        private void OnDamaged(SDamageInfo hit, float finalAmount) { }

        #endregion
    }
}
