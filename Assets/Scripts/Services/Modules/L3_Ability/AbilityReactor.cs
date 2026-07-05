using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 技能反应器。挂载在 Target 侧，对应 AbilityExecutor 的发送端。
    /// 负责接收面 ⑥⑦⑧：结算 SDamageInfo → 落地伤害 → 触发反应 → 广播事件。
    /// 也是 Buff/Tag 在目标侧的唯一起效入口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityReactor : ModuleChildMono
    {
        private HitEvent hitEvent;
        private Identity _identity;
        private AbilityExecutor _executor;
        private PropertyTable _propertyTable;
        private bool _resolved;

        /// <summary>⑥ 结算回调。外部修改器介入 Avoidance → Mitigation → Absorption，返回结算后伤害。0 = 完全回避。</summary>
        public System.Func<SDamageInfo, float> ResolutionCallback;

        /// <summary>⑦ 反应回调。外部修改器追加反伤 / 吸血。</summary>
        public System.Action<SDamageInfo, float> ReactionCallback;

        /// <summary>伤害落地回调。外部（CharacterCombat / Stats）写入最终数值。</summary>
        public System.Action<SDamageInfo, float> ApplyDamageCallback;

        /// <summary>被动通知回调。外部触发目标自身 OnDamaged 被动。</summary>
        public System.Action<SDamageInfo, float> OnDamagedCallback;

        public override void OnWire()
        {
            hitEvent = GetComponent<EventHub>()?.Get<HitEvent>();
        }

        private void EnsureResolved()
        {
            if (_resolved) return;
            _identity = GetComponent<Identity>();
            _executor = GetComponent<AbilityExecutor>();
            _propertyTable = _identity?.Properties;
            _resolved = true;
        }

        /// <summary>
        /// 受击结算。目标侧唯一入口。
        ///
        /// 完整流程：
        ///   ① Damage Resolution — Avoidance → Mitigation → Absorption → finalAmount
        ///   ② Effect Application — Buff + Tag（伤害本身通过回调在①落地）
        ///   ③ Reaction — 反伤 / 吸血 / 被动通知
        ///   ④ Broadcast — hitEvent → VFX / Audio / UI 等不确定消费者
        /// </summary>
        public float Resolve(SDamageInfo hit)
        {
            EnsureResolved();

            // ── ② Non-damage Effects（Buff + Tag，无论是否造成伤害都施加）──
            var effects = hit.Target == hit.Caster
                ? hit.SourceAbility?.selfEffects
                : hit.SourceAbility?.targetEffects;
            ApplyEffects(effects, hit.SourceInstance);

            // ── ① Damage Resolution ──
            var damage = hit.Damage;
            if (damage == null || damage.Length == 0) return 0f;

            float instantSum = 0f;
            foreach (var entry in damage)
            {
                if (entry.IsDot)
                {
                    // TODO: DOT 落地 — FloatModifier Mode B PerSecond 挂到目标 HP，需 FloatModifier 加 ExpiryTime
                    continue;
                }
                instantSum += entry.Amount;
            }

            float finalAmount = ResolutionCallback?.Invoke(hit) ?? instantSum;
            // TODO: per-DamageEntry ResolutionCallback — 当前单回调处理 TotalAmount，后续按 tag 分 channel 路由抗性

            // ── ③ Damage + Reaction ──
            if (finalAmount > 0f)
            {
                ApplyDamageCallback?.Invoke(hit, finalAmount);
                ReactionCallback?.Invoke(hit, finalAmount);
                // OnHit 通知施法者 → 由 ExecutionState 通过 OnHitResolved 回调完成
            }
            else
            {
                // TODO: OnDodge / OnBlock 分事件 → 阻塞：回避判定未落地
            }

            // ── ④ Broadcast ──
            OnDamagedCallback?.Invoke(hit, finalAmount);
            hitEvent?.Raise(hit);
            // TODO: 伤害类型转换（防弹衣穿刺→钝伤）— 阻塞：防弹衣系统未就位

            return finalAmount;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Effect Application（目标侧效果统一入口）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>对自身施加效果（Buff + Tag）。selfEffects / targetEffects 都走这里。</summary>
        public void ApplyEffects(EffectSO[] effects, AbilityInstance owner)
        {
            if (effects == null) return;
            EnsureResolved();

            foreach (var effect in effects)
            {
                if (effect == null) continue;
                if (effect is CostEffectSO) continue;
                if (effect is DamageEffectSO) continue;   // 伤害由 Resolve 处理
                if (effect is ImpactEffectSO) continue;   // 冲击由 Resolve 处理

                if (effect is BuffEffectSO buff)
                    ApplyBuff(buff, owner);

                if (effect.duration > 0f && effect.effectTag != null)
                    ApplyTag(effect.effectTag.FullTag, owner);
            }
        }

        private void ApplyBuff(BuffEffectSO buff, AbilityInstance owner)
        {
            // ── FloatAdjunct ──
            if (buff.adjuncts != null && buff.adjuncts.Length > 0)
            {
                float expiry = buff.duration > 0f ? Time.time + buff.duration : -1f;
                foreach (var adj in buff.adjuncts)
                {
                    if (adj.property == null) continue;
                    if (_propertyTable == null) continue;
                    if (!_propertyTable.TryGetPath(adj.property, out var path))
                    {
                        Debug.LogWarning($"[Passive] ApplyBuff {buff.name}: property '{adj.property.Id}' not in PropertyTable structure, skipped");
                        continue;
                    }
                    _propertyTable.AddAdjunct(new FloatAdjunct
                    {
                        Owner = owner,
                        TargetPath = path,
                        ValueAdd = adj.valueAdd,
                        ValueMultiply = adj.valueMultiply,
                        MaxAdd = adj.maxAdd,
                        MaxMultiply = adj.maxMultiply <= 0f ? 1f : adj.maxMultiply,
                        ExpiryTime = expiry,
                    });
                }
            }

            // ── grantedTags（独立于 adjuncts）──
            if (buff.grantedTags != null && buff.grantedTags.Length > 0)
            {
                foreach (var tag in buff.grantedTags)
                    ApplyTag(tag.FullTag, owner);

                if (buff.duration > 0f && _executor != null)
                    _executor.AddBuffTags(buff.grantedTags, Time.time + buff.duration, owner);
            }
        }

        private void ApplyTag(string tag, AbilityInstance owner)
        {
            if (_executor != null)
                _executor.OwnedTags.AddTag(tag, owner);
            else
                _identity?.Tags.AddTag(tag, owner);
        }
    }
}
