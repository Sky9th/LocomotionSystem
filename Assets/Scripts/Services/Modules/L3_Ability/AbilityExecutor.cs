using System.Collections.Generic;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;

namespace RedDust.Ability
{
    /// <summary>
    /// 通用能力执行器。角色、陷阱、Boss 均可使用。
    /// 与 AbilityReactor 配对：Executor 是发送端（造成伤害），Reactor 是接收端（处理命中）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityExecutor : MonoBehaviour
    {
        public GameplayTagContainer OwnedTags { get; } = new();

        [Header("Passives")]
        [SerializeField] private PassiveAbilitySO[] initialPassives;

        private readonly List<PassiveAbilitySO> runtimePassives = new();

        /// <summary>
        /// 目标过滤回调。由外部实体（如 Trap）设置。
        /// 参数: (匹配到的被动技能, 候选目标) → null=放行, 非null=过滤原因
        /// </summary>
        public System.Func<PassiveAbilitySO, GameObject, string> TargetFilterCallback;

        /// <summary>
        /// 效果修改回调。由外部实体（如 Stats）在 Awake 设置。
        /// 参数: (效果SO, 目标, 基础伤害值) → 修改后的伤害值
        /// </summary>
        public System.Func<EffectSO, GameObject, float, float> EffectCallback;

        /// <summary>② 条件门控回调。外部注入。null=通过，非null=拒绝原因（沉默/眩晕等）。</summary>
        public System.Func<AbilityDefSO, string> ConditionCallback;

        // TODO: Phase 4.2 — 特殊消耗回调，当前无真正需求，暂注释
        // public System.Func<CostEffectSO, bool> CostCallback;

        /// <summary>③ 属性值查询。(def) → 当前值。用于预检。</summary>
        public System.Func<PropertyDefSO, float> PeekStatCallback;

        /// <summary>③ 标准属性修改。(statDef, delta) → void。预检已通过，必定执行。</summary>
        public System.Action<PropertyDefSO, float> ModifyStatCallback;

        // 冷却: tag → 到期时间戳
        private readonly Dictionary<string, float> cooldownEndTimes = new();
        private readonly List<string> cooldownExpiredBuffer = new();
        private float cooldownCleanupAccum;

        // 冷却 key → abilityTag.FullTag，冷却到期后一并移除
        private readonly Dictionary<string, string> cooldownAbilityTags = new();

        private void Awake()
        {
            if (initialPassives == null) return;
            foreach (var p in initialPassives)
            {
                if (p != null) runtimePassives.Add(p);
            }
        }

        private void Update()
        {
            CleanupExpiredCooldowns();
        }

        private void CleanupExpiredCooldowns()
        {
            cooldownCleanupAccum += Time.deltaTime;
            if (cooldownCleanupAccum < 0.5f) return;
            cooldownCleanupAccum = 0f;

            var now = Time.time;
            cooldownExpiredBuffer.Clear();
            foreach (var kv in cooldownEndTimes)
            {
                if (now >= kv.Value)
                    cooldownExpiredBuffer.Add(kv.Key);
            }
            foreach (var key in cooldownExpiredBuffer)
            {
                OwnedTags.RemoveTag(key);
                cooldownEndTimes.Remove(key);

                // 移除关联的 abilityTag
                if (cooldownAbilityTags.TryGetValue(key, out var abilityTag))
                {
                    OwnedTags.RemoveTag(abilityTag);
                    cooldownAbilityTags.Remove(key);
                }
            }
        }

        public void AddCooldown(string tag, float duration)
        {
            if (string.IsNullOrEmpty(tag) || duration <= 0f) return;
            OwnedTags.AddTag(tag);
            cooldownEndTimes[tag] = Time.time + duration;
        }

        public bool IsOnCooldown(string tag)
        {
            return !string.IsNullOrEmpty(tag) && cooldownEndTimes.TryGetValue(tag, out var end) && Time.time < end;
        }

        #region Physics Callbacks

        private void OnTriggerEnter(Collider other)
        {
            for (int i = 0; i < runtimePassives.Count; i++)
            {
                var p = runtimePassives[i];
                if (p == null || p.trigger != ETriggerEvent.OnEnterArea) continue;

                if (!PassTargetRequiredTag(p, other.gameObject)) continue;
                if (!PassCooldown(p)) continue;

                if (TargetFilterCallback?.Invoke(p, other.gameObject) != null) continue;

                ExecutePassive(p, other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            for (int i = 0; i < runtimePassives.Count; i++)
            {
                var p = runtimePassives[i];
                if (p == null || p.trigger != ETriggerEvent.OnExitArea) continue;

                if (!PassTargetRequiredTag(p, other.gameObject)) continue;
                if (!PassCooldown(p)) continue;

                if (TargetFilterCallback?.Invoke(p, other.gameObject) != null) continue;

                ExecutePassive(p, other.gameObject);
            }
        }

        #endregion

        #region Gates

        private bool PassTargetRequiredTag(PassiveAbilitySO passive, GameObject target)
        {
            if (passive.targetRequiredTag == null) return true;

            var identity = target.GetComponent<Identity>();
            if (identity == null) return false;
            if (!identity.Tags.HasTag(passive.targetRequiredTag.FullTag)) return false;

            return true;
        }

        private bool PassCooldown(AbilitySO ability)
        {
            if (ability.cooldownDuration <= 0f) return true;

            var key = ability.sharedCooldownTag != null
                ? ability.sharedCooldownTag.FullTag
                : $"Ability.Cooldown.{ability.internalName}";

            if (IsOnCooldown(key)) return false;

            return true;
        }

        private void ApplyCooldown(AbilitySO ability)
        {
            if (ability.cooldownDuration <= 0f) return;

            var key = ability.sharedCooldownTag != null
                ? ability.sharedCooldownTag.FullTag
                : $"Ability.Cooldown.{ability.internalName}";

            AddCooldown(key, ability.cooldownDuration);

            // 记录关联的 abilityTag，冷却到期后一并移除
            if (ability.abilityTag != null)
            {
                cooldownAbilityTags[key] = ability.abilityTag.FullTag;
            }
        }

        #endregion

        #region Passive

        private void ExecutePassive(PassiveAbilitySO passive, GameObject target)
        {
            // selfEffects — 施加给技能持有者自己
            ApplyEffects(passive, passive.selfEffects, gameObject);

            // targetEffects — 施加给目标
            ApplyEffects(passive, passive.targetEffects, target);

            ApplyCooldown(passive);
        }

        private void ApplyEffects(PassiveAbilitySO source, EffectSO[] effects, GameObject target)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (effect == null) continue;

                // 伤害效果 — 构造 SDamageInfo
                if (effect is DamageEffectSO dmg)
                {
                    // TODO: baseDamage 已改为 baseValue，装备系统填充。后续重写。
                    /*
                    var baseDamage = dmg.baseValue;

                    // 钩子：外部修改器调整伤害值
                    var finalDamage = EffectCallback?.Invoke(effect, target, baseDamage) ?? baseDamage;

                    var hit = new SDamageInfo(
                        gameObject, target, finalDamage,
                        effect.effectTag,
                        target.transform.position,
                        target.transform.position - transform.position,
                        source
                    );

                    target.GetComponent<AbilityReactor>()?.Resolve(hit);
                    */
                }

                // duration > 0 的效果 — 把自身的 effectTag 挂给目标
                if (effect.duration > 0f && effect.effectTag != null)
                {
                    if (target == gameObject)
                        OwnedTags.AddTag(effect.effectTag.FullTag);
                    else
                        target.GetComponent<Identity>()?.Tags.AddTag(effect.effectTag.FullTag);
                }
            }
        }

        public void AddPassive(PassiveAbilitySO passive)
        {
            // TODO: 添加被动技能到运行时列表
        }

        public void RemovePassive(PassiveAbilitySO passive)
        {
            // TODO: 从运行时列表移除被动技能
        }

        #endregion

        #region Active

        /// <summary>
        /// 尝试激活主动技能。②→③→④→⑤→⑥→⑧ 完整管道，同步执行（瞬发）。
        /// </summary>
        /// <returns>true=激活成功，false=被门控拒绝。</returns>
        public bool TryActivate(AbilityDefSO ability, Vector3 origin, Vector3 direction)
        {
            if (ability == null) return false;

            Debug.Log($"[Ability] TryActivate: {ability.internalName} | origin={origin} dir={direction}");

            // ── ② Gating ──
            // 冷却
            if (!PassCooldown(ability))
            {
                Debug.Log($"[Ability] ② Rejected: {ability.internalName} — on cooldown");
                return false;
            }

            // 互斥 — 默认检查 abilityTag.Parent + 额外互斥标签
            if (!ability.overrideExclusion)
            {
                // 默认：同父标签下互斥
                if (ability.abilityTag?.Parent != null && OwnedTags.HasTag(ability.abilityTag.Parent.FullTag))
                {
                    Debug.Log($"[Ability] ② Rejected: {ability.internalName} — mutual exclusion ({ability.abilityTag.Parent.FullTag})");
                    return false;
                }

                // 额外互斥标签：跨分类互斥
                if (ability.extraExclusionTags != null)
                {
                    foreach (var tag in ability.extraExclusionTags)
                    {
                        if (tag != null && OwnedTags.HasTag(tag.FullTag))
                        {
                            Debug.Log($"[Ability] ② Rejected: {ability.internalName} — extra exclusion ({tag.FullTag})");
                            return false;
                        }
                    }
                }
            }

            // 外部条件
            if (ConditionCallback != null)
            {
                var reason = ConditionCallback(ability);
                if (reason != null)
                {
                    Debug.Log($"[Ability] ② Rejected: {ability.internalName} — condition: {reason}");
                    return false;
                }
            }

            // ── ③ Cost ──
            if (ability.selfEffects != null)
            {
                // Phase 1: 预检。确保全部消耗可负担，不实际扣费。
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO cost && cost.def != null)
                    {
                        if (PeekStatCallback == null)
                        {
                            Debug.LogError($"[Ability] PeekStatCallback is null — cost check skipped for {ability.internalName}");
                            return false;
                        }
                        var current = PeekStatCallback.Invoke(cost.def);
                        if (current < cost.amount)
                        {
                            Debug.Log($"[Ability] ③ Cost fail: {ability.internalName} needs {cost.def.Id}={cost.amount}, current={current:F1}");
                            return false;
                        }
                        Debug.Log($"[Ability] ③ Cost check: {cost.def.Id} current={current:F1} cost={cost.amount} → OK");
                    }
                }

                // Phase 2: 扣除。预检通过，逐项执行。
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO cost && cost.def != null)
                    {
                        ModifyStatCallback?.Invoke(cost.def, -cost.amount);
                        var after = PeekStatCallback.Invoke(cost.def);
                        Debug.Log($"[Ability] ③ Cost deduct: {cost.def.Id} -{cost.amount} → {after:F1}");
                    }
                }
            }

            // ③b 有冷却时挂 abilityTag（冷却结束移除，见 ApplyCooldown）
            if (ability.cooldownDuration > 0f && ability.abilityTag != null)
                OwnedTags.AddTag(ability.abilityTag.FullTag);

            // ── ④ Search ──
            var targets = ability.search != null
                ? AbilitySearchUtility.Execute(ability.search, gameObject, origin, direction)
                : new List<GameObject>();

            Debug.Log($"[Ability] ④ Search: {ability.internalName} type={ability.search?.searchType} hits={targets.Count}");

            // ── ⑤ Effects ──
            // selfEffects — 对施法者自身的伤害 / buff / tag
            if (ability.selfEffects != null)
            {
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO) continue; // ③ 已处理

                    if (effect is DamageEffectSO dmg)
                    {
                        // TODO: baseDamage 已改为 baseValue，装备系统填充。后续重写。
                        /*
                        var finalDamage = EffectCallback?.Invoke(effect, gameObject, dmg.baseValue) ?? dmg.baseValue;
                        Debug.Log($"[Ability] ⑤ SelfDamage: {ability.internalName} → self base={dmg.baseValue} final={finalDamage:F1}");
                        var hit = new SDamageInfo(
                            gameObject, gameObject, finalDamage,
                            effect.effectTag,
                            transform.position,
                            Vector3.zero,
                            ability
                        );
                        GetComponent<AbilityReactor>()?.Resolve(hit);
                        */
                    }

                    // duration > 0 的效果 — 把自身的 effectTag 挂给目标
                    if (effect.duration > 0f && effect.effectTag != null)
                    {
                        OwnedTags.AddTag(effect.effectTag.FullTag);
                        Debug.Log($"[Ability] ⑤ SelfTag: {ability.internalName} tag={effect.effectTag.FullTag} duration={effect.duration}s");
                    }
                }
            }

            // targetEffects → SDamageInfo → Reactor
            if (ability.targetEffects != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    foreach (var effect in ability.targetEffects)
                    {
                        if (effect is DamageEffectSO dmg)
                        {
                            // TODO: baseDamage 已改为 baseValue，装备系统填充。后续重写。
                            /*
                            var finalDamage = EffectCallback?.Invoke(effect, target, dmg.baseValue) ?? dmg.baseValue;
                            Debug.Log($"[Ability] ⑤ TargetDamage: {ability.internalName} → {target.name} base={dmg.baseValue} final={finalDamage:F1}");
                            var hit = new SDamageInfo(
                                gameObject, target, finalDamage,
                                effect.effectTag,
                                target.transform.position,
                                target.transform.position - origin,
                                ability
                            );
                            target.GetComponent<AbilityReactor>()?.Resolve(hit);
                            */
                        }

                        // duration > 0 的效果 — 把自身的 effectTag 挂给目标
                        if (effect.duration > 0f && effect.effectTag != null)
                        {
                            target.GetComponent<Identity>()?.Tags.AddTag(effect.effectTag.FullTag);
                            Debug.Log($"[Ability] ⑤ TargetTag: {ability.internalName} → {target.name} tag={effect.effectTag.FullTag} duration={effect.duration}s");
                        }
                    }
                }
            }

            // 冷却（移除 cooldown key + abilityTag）
            ApplyCooldown(ability);

            Debug.Log($"[Ability] ✅ Activated: {ability.internalName} | targets={targets.Count} cooldown={ability.cooldownDuration}s");
            return true;
        }

        #endregion
    }
}