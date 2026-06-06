using RedDust.Ability;
using RedDust.Character.Stats;
using RedDust.Core;
using RedDust.Stats;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 战斗中枢。持有 AbilityExecutor、AbilityReactor、CharacterStats 引用，
    /// 在构造时注入五个修改器回调，桥接 Ability 管道与 Character 属性系统。
    /// </summary>
    public class CharacterCombat
    {
        private readonly AbilityExecutor ability;
        private readonly AbilityReactor reactor;
        private readonly CharacterStats stats;
        private readonly EventHub eventHub;

        public CharacterCombat(AbilityExecutor ability, AbilityReactor reactor, CharacterStats stats, EventHub eventHub)
        {
            this.ability = ability;
            this.reactor = reactor;
            this.stats = stats;
            this.eventHub = eventHub;

            WireCallbacks();
        }

        /// <summary>Start 时订阅事件（确保 EventHub.Awake 已完成）。</summary>
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

        /// <summary>
        /// ③ 标准属性修改。直接走 StatInstance.Modify()。
        /// </summary>
        private float OnPeekStat(StatDefinitionSO def)
        {
            return stats?.Get(def)?.Current ?? 0f;
        }

        private void OnModifyStat(StatDefinitionSO def, float delta)
        {
            stats?.Get(def)?.Modify(delta);
        }

        /// <summary>
        /// ⑥ 施展方伤害修正。在 AbilityExecutor 构造 SDamageInfo 时调用。
        /// 修改 baseDamage（力量加成、穿透等）。
        /// </summary>
        private float OnEffectModify(EffectSO effect, GameObject target, float baseDamage)
        {
            // TODO: Phase 4.2 — 施展方属性修正（力量/穿透/暴击）
            return baseDamage;
        }

        /// <summary>
        /// ⑥ 承受方结算管线。Avoidance → Mitigation → Absorption。
        /// 返回结算后伤害。0 = 完全回避。
        /// </summary>
        private float OnResolveDamage(SDamageInfo hit)
        {
            float amount = hit.Amount;

            float incoming = amount;

            // Mitigation — Endurance 减伤: 5 Endurance = 25%
            var endurance = stats.Get("Attributes/Endurance")?.Current ?? 0f;
            if (endurance > 0f)
                amount *= 1f - endurance * 0.05f;

            if (amount != incoming)
                Debug.Log($"[Combat] {hit.Target.name} Mitigation: {incoming:F1} → {amount:F1} (endurance={endurance:F1})");

            // TODO: Phase 4.2 — 回避判定（闪避率）
            // TODO: Phase 4.2 — 吸收结算（护盾）

            return amount;
        }

        /// <summary>
        /// ⑥ 伤害落地。直接写入 HP。
        /// </summary>
        private void OnApplyDamage(SDamageInfo hit, float finalAmount)
        {
            var hp = stats?.Get("Vitals/HP");
            if (hp != null)
            {
                var before = hp.Current;
                hp.Modify(-finalAmount);
                Debug.Log($"[Combat] {hit.Target.name} HP: {before:F1} -{finalAmount:F1} → {hp.Current:F1}");
            }
        }

        /// <summary>
        /// ⑦ 反应。反伤 / 吸血等命中后效果（Phase 4.2+）。
        /// </summary>
        private void OnReaction(SDamageInfo hit, float finalAmount) { }

        /// <summary>
        /// ⑧ 受击通知。触发目标自身 OnDamaged 被动技能（Phase 4.2+）。
        /// </summary>
        private void OnDamaged(SDamageInfo hit, float finalAmount) { }

        #endregion
    }
}
