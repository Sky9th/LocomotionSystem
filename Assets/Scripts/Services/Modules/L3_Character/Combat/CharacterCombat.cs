using RedDust.Ability;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 战斗中枢。桥接 Ability 管道与 Character 属性系统。
    /// </summary>
    public class CharacterCombat
    {
        private readonly AbilityExecutor ability;
        private readonly AbilityReactor reactor;
        private readonly PropertyAgent agent;
        private readonly EventHub eventHub;

        public CharacterCombat(AbilityExecutor ability, AbilityReactor reactor, PropertyAgent agent, EventHub eventHub)
        {
            this.ability = ability;
            this.reactor = reactor;
            this.agent = agent;
            this.eventHub = eventHub;

            WireCallbacks();
        }

        public void SubscribeEvents()
        {
            eventHub?.Get<HitEventSO>()?.Register(OnHitEvent);
        }

        private void WireCallbacks()
        {
            if (ability != null)
            {
                ability.EffectCallback = OnEffectModify;
                ability.PeekStatCallback = OnPeekStat;
                ability.ModifyStatCallback = OnModifyStat;
            }

            if (reactor != null)
            {
                reactor.ResolutionCallback = OnResolveDamage;
                reactor.ApplyDamageCallback = OnApplyDamage;
                reactor.ReactionCallback = OnReaction;
                reactor.OnDamagedCallback = OnDamaged;
            }
        }

        private void OnHitEvent(SDamageInfo hit) { }

        #region 修改器占位

        private float OnPeekStat(PropertyDefSO def)
        {
            return agent.GetFloat(def.Id);
        }

        private void OnModifyStat(PropertyDefSO def, float delta)
        {
            agent.Modify(def.Id, delta);
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

            var endurance = agent.GetFloat("Attributes/Endurance");
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
            var before = agent.GetFloat("Vitals/HP");
            agent.Modify("Vitals/HP", -finalAmount);
            Debug.Log($"[Combat] {hit.Target.name} HP: {before:F1} -{finalAmount:F1} → {agent.GetFloat("Vitals/HP"):F1}");
        }

        private void OnReaction(SDamageInfo hit, float finalAmount) { }
        private void OnDamaged(SDamageInfo hit, float finalAmount) { }

        #endregion
    }
}
