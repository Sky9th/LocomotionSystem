using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能反应器。挂载在 Target 侧，对应 AbilityExecutor 的发送端。
    /// 负责接收面 ⑥⑦⑧：结算 SDamageInfo → 落地伤害 → 触发反应 → 广播事件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityReactor : MonoBehaviour
    {
        private HitEventSO hitEvent;

        /// <summary>⑥ 结算回调。外部修改器介入 Avoidance → Mitigation → Absorption，返回结算后伤害。0 = 完全回避。</summary>
        public System.Func<SDamageInfo, float> ResolutionCallback;

        /// <summary>⑦ 反应回调。外部修改器追加反伤 / 吸血。</summary>
        public System.Action<SDamageInfo, float> ReactionCallback;

        /// <summary>伤害落地回调。外部（CharacterCombat / Stats）写入最终数值。</summary>
        public System.Action<SDamageInfo, float> ApplyDamageCallback;

        /// <summary>被动通知回调。外部触发目标自身 OnDamaged 被动。</summary>
        public System.Action<SDamageInfo, float> OnDamagedCallback;

        private void Awake()
        {
            hitEvent = GetComponent<EventHub>()?.Get<HitEventSO>();
        }

        /// <summary>
        /// 结算单次命中。Caster.AbilityExecutor 直接调用。
        /// </summary>
        public void Resolve(SDamageInfo hit)
        {
            float finalAmount = ResolutionCallback?.Invoke(hit) ?? hit.Amount;

            if (finalAmount <= 0f)
            {
                OnDamagedCallback?.Invoke(hit, 0f);
                hitEvent?.Raise(hit);
                return;
            }

            ApplyDamageCallback?.Invoke(hit, finalAmount);
            ReactionCallback?.Invoke(hit, finalAmount);
            OnDamagedCallback?.Invoke(hit, finalAmount);
            hitEvent?.Raise(hit);
        }
    }
}
