using System.Collections.Generic;
using UnityEngine;
using RedDust.Core;

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

        // 冷却: tag → 到期时间戳
        private readonly Dictionary<string, float> cooldownEndTimes = new();
        private readonly List<string> cooldownExpiredBuffer = new();
        private float cooldownCleanupAccum;

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

        private bool PassCooldown(PassiveAbilitySO passive)
        {
            if (passive.cooldownDuration <= 0f) return true;

            var key = passive.sharedCooldownTag != null
                ? passive.sharedCooldownTag.FullTag
                : $"Passive.Cooldown.{passive.internalName}";

            if (IsOnCooldown(key)) return false;

            return true;
        }

        private void ApplyCooldown(PassiveAbilitySO passive)
        {
            if (passive.cooldownDuration <= 0f) return;

            var key = passive.sharedCooldownTag != null
                ? passive.sharedCooldownTag.FullTag
                : $"Passive.Cooldown.{passive.internalName}";

            AddCooldown(key, passive.cooldownDuration);
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
                    var baseDamage = dmg.baseDamage;

                    // 钩子：外部修改器调整伤害值
                    var finalDamage = EffectCallback?.Invoke(effect, target, baseDamage) ?? baseDamage;

                    var hit = new SDamageInfo(
                        gameObject, target, finalDamage,
                        effect.effectTag,
                        target.transform.position,
                        target.transform.position - transform.position,
                        source,
                        effect.grantedTag
                    );

                    target.GetComponent<AbilityReactor>()?.Resolve(hit);
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
    }
}