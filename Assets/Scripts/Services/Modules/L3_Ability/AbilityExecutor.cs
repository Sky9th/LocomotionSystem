using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;

namespace RedDust.Ability
{
    [DisallowMultipleComponent]
    public sealed class AbilityExecutor : MonoBehaviour
    {
        // ╔══════════════════════════════════════════════════════════════╗
        // ║  NEW IMPLEMENTATION                                          ║
        // ╚══════════════════════════════════════════════════════════════╝

        private readonly ActiveAbilityPipeline _pipeline = new();
        private readonly Queue<SQueuedSkill> _queue = new();

        /// <summary>
        /// 将主动技能加入释放队列。当前无技能在释放时下一帧开始执行。
        /// </summary>
        public void Enqueue(ActiveAbilitySO ability, Vector3 origin, Vector3 direction, Entity weaponEntity = null)
        {
            _queue.Enqueue(new SQueuedSkill
            {
                Ability = ability,
                Origin = origin,
                Direction = direction,
                WeaponEntity = weaponEntity,
            });
        }

        private void Update()
        {
            CleanupExpiredCooldowns();
            _pipeline.Tick(Time.deltaTime);

            // 管道空闲且有排队技能 → 启动下一个
            if (_queue.Count > 0 && _pipeline.IsIdle)
            {
                var next = _queue.Dequeue();
                _pipeline.Start(next.Ability, this, next.Origin, next.Direction, next.WeaponEntity);
            }
        }

        private struct SQueuedSkill
        {
            public ActiveAbilitySO Ability;
            public Vector3 Origin;
            public Vector3 Direction;
            public Entity WeaponEntity;
        }

        // ═══════════════════════════════════════════════════════════════
        // ▼ OLD IMPLEMENTATION — preserved as reference
        // ═══════════════════════════════════════════════════════════════
        #region OLD_IMPLEMENTATION

        public rTagContainer OwnedTags { get; } = new();

        [Header("Passives")]
        [SerializeField] private PassiveAbilitySO[] initialPassives;

        private readonly List<PassiveAbilitySO> runtimePassives = new();
        private readonly AbilitySearch _search = new();

        public System.Func<PassiveAbilitySO, GameObject, string> TargetFilterCallback;
        public System.Func<EffectSO, GameObject, float, float> EffectCallback;
        public System.Func<ActiveAbilitySO, string> ConditionCallback;
        public System.Func<PropertyDefSO, float> PeekStatCallback;
        public System.Action<PropertyDefSO, float> ModifyStatCallback;

        private readonly Dictionary<string, float> cooldownEndTimes = new();
        private readonly List<string> cooldownExpiredBuffer = new();
        private float cooldownCleanupAccum;
        private readonly Dictionary<string, string> cooldownAbilityTags = new();
        private readonly List<(string tag, float expiryTime)> _buffTags = new();

        private void Awake()
        {
            if (initialPassives == null) return;
            foreach (var p in initialPassives)
            {
                if (p != null) runtimePassives.Add(p);
            }
        }

        // private void Update() { CleanupExpiredCooldowns(); }  // ← moved to NEW

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
                if (cooldownAbilityTags.TryGetValue(key, out var abilityTag))
                {
                    OwnedTags.RemoveTag(abilityTag);
                    cooldownAbilityTags.Remove(key);
                }
            }

            if (_buffTags.Count > 0)
            {
                _buffTags.RemoveAll(t =>
                {
                    if (now >= t.expiryTime) { OwnedTags.RemoveTag(t.tag); return true; }
                    return false;
                });
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

        public void AddBuffTags(rTagDefSO[] tags, float expiryTime)
        {
            if (tags == null || expiryTime <= Time.time) return;
            foreach (var t in tags)
            {
                if (t != null) _buffTags.Add((t.FullTag, expiryTime));
            }
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
            if (ability.abilityTag != null)
                cooldownAbilityTags[key] = ability.abilityTag.FullTag;
        }

        #endregion

        #region Passive

        private void ExecutePassive(PassiveAbilitySO passive, GameObject target)
        {
            ApplyEffects(passive, passive.selfEffects, gameObject);
            ApplyEffects(passive, passive.targetEffects, target);
            ApplyCooldown(passive);
        }

        private void ApplyEffects(PassiveAbilitySO source, EffectSO[] effects, GameObject target)
        {
            if (effects == null) return;
            foreach (var effect in effects)
            {
                if (effect == null) continue;

                if (effect is DamageEffectSO dmg)
                {
                    // TODO: baseDamage 已改为 baseValue，装备系统填充。后续重写。
                }

                if (effect.duration > 0f && effect.effectTag != null)
                {
                    if (target == gameObject)
                        OwnedTags.AddTag(effect.effectTag.FullTag);
                    else
                        target.GetComponent<Identity>()?.Tags.AddTag(effect.effectTag.FullTag);
                }
            }
        }

        public void AddPassive(PassiveAbilitySO passive) { /* TODO */ }
        public void RemovePassive(PassiveAbilitySO passive) { /* TODO */ }

        #endregion

        #region Active

        public bool TryActivate(ActiveAbilitySO ability, Vector3 origin, Vector3 direction)
        {
            if (ability == null) return false;

            Debug.Log($"[Ability] TryActivate: {ability.internalName} | origin={origin} dir={direction}");

            // ── ② Gating ──
            if (!PassCooldown(ability))
            {
                Debug.Log($"[Ability] ② Rejected: {ability.internalName} — on cooldown");
                return false;
            }

            if (!ability.overrideExclusion)
            {
                if (ability.abilityTag?.Parent != null && OwnedTags.HasTag(ability.abilityTag.Parent.FullTag))
                {
                    Debug.Log($"[Ability] ② Rejected: {ability.internalName} — mutual exclusion ({ability.abilityTag.Parent.FullTag})");
                    return false;
                }

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
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO cost && cost.def != null)
                    {
                        if (PeekStatCallback == null)
                        {
                            Debug.LogError($"[Ability] PeekStatCallback is null");
                            return false;
                        }
                        var current = PeekStatCallback.Invoke(cost.def);
                        if (current < cost.amount)
                        {
                            Debug.Log($"[Ability] ③ Cost fail: {ability.internalName} needs {cost.def.Id}={cost.amount}, current={current:F1}");
                            return false;
                        }
                    }
                }

                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO cost && cost.def != null)
                    {
                        ModifyStatCallback?.Invoke(cost.def, -cost.amount);
                    }
                }
            }

            if (ability.cooldownDuration > 0f && ability.abilityTag != null)
                OwnedTags.AddTag(ability.abilityTag.FullTag);

            // ── ④ Search ──
            var targets = ability.search != null
                ? _search.Execute(ability.search, gameObject, origin, direction)
                : new List<GameObject>();

            Debug.Log($"[Ability] ④ Search: {ability.internalName} type={ability.search?.searchType} hits={targets.Count}");

            // ── ⑤ Effects ──
            if (ability.selfEffects != null)
            {
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO) continue;
                    if (effect is BuffEffectSO buff) { ApplyBuff(buff, gameObject); continue; }
                    if (effect.duration > 0f && effect.effectTag != null)
                        OwnedTags.AddTag(effect.effectTag.FullTag);
                }
            }

            if (ability.targetEffects != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    foreach (var effect in ability.targetEffects)
                    {
                        if (effect is BuffEffectSO buff) { ApplyBuff(buff, target); continue; }
                        if (effect.duration > 0f && effect.effectTag != null)
                            target.GetComponent<Identity>()?.Tags.AddTag(effect.effectTag.FullTag);
                    }
                }
            }

            ApplyCooldown(ability);

            Debug.Log($"[Ability] ✅ Activated: {ability.internalName} | targets={targets.Count}");
            return true;
        }

        private void ApplyBuff(BuffEffectSO buff, GameObject target)
        {
            if (buff == null || target == null) return;

            if (buff.grantedTags != null && buff.grantedTags.Length > 0)
            {
                var targetAC = target.GetComponent<AbilityExecutor>();
                if (targetAC != null)
                {
                    foreach (var tag in buff.grantedTags)
                        targetAC.OwnedTags.AddTag(tag);
                    if (buff.duration > 0f)
                        targetAC.AddBuffTags(buff.grantedTags, Time.time + buff.duration);
                }
            }
        }

        #endregion

        #endregion
    }
}