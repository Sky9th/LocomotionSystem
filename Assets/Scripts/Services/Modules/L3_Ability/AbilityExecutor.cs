using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;
using RedDust.Character.Animation;

namespace RedDust.Ability
{
    [DisallowMultipleComponent]
    public sealed class AbilityExecutor : MonoBehaviour
    {
        private readonly AbilityPipeline _activePipeline = new();
        private readonly InstanceManager _instances = new();
        private readonly List<AbilityPipeline> _runningPassives = new();
        private readonly Queue<(AbilityInstance instance, List<GameObject> targets)> _pendingPassiveStarts = new();
        private PropertyTable _propertyTable;
        private bool _propertyTableResolved;

        private AnimationBrain _brain;
        private bool _brainResolved;
        private bool _fireMarkerReached;
        private bool _clipFinished;
        private bool _isAnimationActive;
        private AnimationRequest _currentAnimRequest;

        /// <summary>
        /// 同 GameObject 的 PropertyTable（来自 Identity）。惰性初始化。
        /// null = 该实体无属性系统。
        /// </summary>
        public PropertyTable PropertyTable
        {
            get
            {
                if (!_propertyTableResolved)
                {
                    var identity = GetComponent<Identity>();
                    _propertyTable = identity != null ? identity.Properties : null;
                    _propertyTableResolved = true;
                }
                return _propertyTable;
            }
        }

        /// <summary>
        /// 同 GameObject 的 AnimationBrain。惰性初始化。
        /// null = 该实体无动画系统 → 纯计时器兜底。
        /// </summary>
        public AnimationBrain Brain
        {
            get
            {
                if (!_brainResolved)
                {
                    _brain = GetComponentInChildren<AnimationBrain>();
                    _brainResolved = true;
                }
                return _brain;
            }
        }

        // ── Animation Bridge ──

        public bool SubmitAbilityAnimation(AbilityActivationSO activation)
        {
            var brain = Brain;
            if (brain == null || activation?.animationClip == null)
                return false;

            _fireMarkerReached = false;
            _clipFinished = false;
            _isAnimationActive = true;

            var request = new AnimationRequest
            {
                Clip = activation.animationClip,
                FadeIn = 0.1f,
                DriverType = EDriverType.Ability,
                CustomData = activation,
                OnMarker = (req) => { if (req != _currentAnimRequest) return; _fireMarkerReached = true; },
                OnCompleted = (req) => { if (req != _currentAnimRequest) return; _clipFinished = true; },
                OnInterrupt = (req) => { if (req != _currentAnimRequest) return; _fireMarkerReached = false; _clipFinished = false; },
            };

            _currentAnimRequest = request;
            brain.SubmitRequest(request);
            return true;
        }

        public void ReleaseAbilityAnimation()
        {
            _isAnimationActive = false;
            Brain?.Release();
        }

        public bool IsAnimationFireMarkerReached() => _fireMarkerReached;
        public bool IsAnimationClipFinished() => _clipFinished;
        public bool IsAnimationActive => _isAnimationActive;

        private void ResetAnimationFlags()
        {
            _fireMarkerReached = false;
            _clipFinished = false;
            _isAnimationActive = false;
            _currentAnimRequest = null;
        }

        /// <summary>联动冷却层级检查——任一 sharedTag 或其父级在冷却中即返回 true。</summary>
        public bool IsBlockedBySharedCooldown(RdTagDefSO[] tags)
        {
            if (tags == null || tags.Length == 0) return false;
            foreach (var tag in tags)
            {
                if (tag == null) continue;
                var t = tag.FullTag;
                foreach (var key in cooldownEndTimes.Keys)
                    if (t == key || t.StartsWith(key + ".")) return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        // TODO: UI 查询 API — 以下三个成员仅服务于 UI 层轮询，不属于 Executor 核心职责。
        // 后续应提取到只读接口（如 IAbilityStateProvider），由 Executor 实现，UI 通过 UIService 消费接口而非 Executor 具体类型。
        // ═══════════════════════════════════════════════════════════════

        public AbilityPipeline Pipeline => _activePipeline;

        /// <summary>技能实例管理器。外部（CharacterActor）通过此接口同步卡片。</summary>
        public InstanceManager Instances => _instances;

        public float GetCooldownRemaining(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0f;
            if (cooldownEndTimes.TryGetValue(tag, out var end) && Time.time < end)
                return end - Time.time;
            return 0f;
        }

        public float GetAbilityCooldownRemaining(ActiveAbilitySO ability)
        {
            if (ability == null || ability.cooldownDuration <= 0f || ability.abilityTag == null) return 0f;
            return GetCooldownRemaining(ability.abilityTag.FullTag);
        }

        // ═══════════════════════════════════════════════════════════════
        // ▲ END UI 查询 API
        // ═══════════════════════════════════════════════════════════════

        public void TryUse(AbilitySO ability, Vector3 origin, Vector3 direction, Entity weaponEntity = null)
        {
            if (ability == null) return;
            if (!_activePipeline.IsIdle) return;

            ResetAnimationFlags();
            var instance = _instances.Activate(ability, "input", ELifecycle.OneShot);
            _activePipeline.Start(instance, this, origin, direction, weaponEntity);
        }

        // ── Update ──────────────────────────────────────────

        private void Update()
        {
            TickActive(Time.deltaTime);
            TickPassives(Time.deltaTime);
            FlushPendingPassives();
            CleanupExpiredCooldowns();
        }

        private void TickActive(float dt)
        {
            _activePipeline.Tick(dt);
            if (!_activePipeline.IsIdle) return;

            var inst = _activePipeline.Context.Instance;
            if (inst != null && inst.Lifecycle == ELifecycle.OneShot)
                _instances.Deactivate(inst);
        }

        private void TickPassives(float dt)
        {
            for (int i = _runningPassives.Count - 1; i >= 0; i--)
            {
                _runningPassives[i].Tick(dt);
                if (_runningPassives[i].IsIdle)
                    _runningPassives.RemoveAt(i);
            }
        }

        private void FlushPendingPassives()
        {
            while (_pendingPassiveStarts.Count > 0)
            {
                var (instance, targets) = _pendingPassiveStarts.Dequeue();
                var pipeline = new AbilityPipeline();
                pipeline.Start(instance, this, Vector3.zero, Vector3.zero,
                    skipAnim: true, guaranteedTargets: targets);
                _runningPassives.Add(pipeline);
            }
        }

        // ── 公开 API ─────────────────────────────────────────

        /// <summary>被动事件入口。匹配持有此触发事件的实例并排队启动 FSM。</summary>
        public void NotifyPassiveEvent(ETriggerEvent trigger, GameObject subject)
        {
            var matches = _instances.GetByTrigger(trigger);
            Debug.Log($"[Passive] NotifyPassiveEvent trigger={trigger} subject={subject?.name} matches={matches.Count}");
            foreach (var inst in matches)
                _pendingPassiveStarts.Enqueue((inst, new List<GameObject> { subject }));
        }

        /// <summary>从 AbilityForest 同步被动实例。先清旧 source 再激活新列表。</summary>
        public void SyncInstances(PassiveAbilitySO[] passives, object source)
        {
            _instances.DeactivateBySource(source);
            Debug.Log($"[Passive] SyncInstances source={source} count={passives?.Length ?? 0} totalActive={_instances.Count}");
            if (passives == null) return;
            foreach (var p in passives)
            {
                if (p != null)
                    _instances.Activate(p, source, ELifecycle.Persistent);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ▼ OLD IMPLEMENTATION — ⛔ 即将废弃，Pipeline 全量接管后删除
        // ═══════════════════════════════════════════════════════════════
        #region OLD_IMPLEMENTATION

        public RdTagContainer OwnedTags { get; } = new();

        [Header("Passives")]
        [SerializeField] private PassiveAbilitySO[] initialPassives;

        private readonly List<PassiveAbilitySO> runtimePassives = new();
        private readonly AbilitySearch _search = new();

        public System.Func<PassiveAbilitySO, GameObject, string> TargetFilterCallback;
        public System.Func<EffectSO, GameObject, float, float> OutgoingDamageCallback;
        /// <summary>命中结算完成通知。caster 侧消费——触发 OnHit 被动、吸血、连招衔接等。</summary>
        public System.Action<SDamageInfo, float> OnHitResolved;
        public System.Func<AbilitySO, string> GatingConditionCallback;
        /// <summary>相位级预检回调。PropertyTable 不存在时接管 Phase 1。null=全部通过, 非null=拒绝原因。</summary>
        public System.Func<CostEffectSO[], string> PreviewCostCallback;
        /// <summary>相位级扣除回调。PropertyTable 不存在时接管 Phase 2。</summary>
        public System.Action<CostEffectSO[]> ApplyCostCallback;

        private readonly Dictionary<string, float> cooldownEndTimes = new();
        private readonly List<string> cooldownExpiredBuffer = new();
        private float cooldownCleanupAccum;
        private readonly List<(string tag, float expiryTime, object owner)> _buffTags = new();

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
            }

            // Pull: 清理 Owner 已失效的标签
            OwnedTags.RemoveTagsWhere(o => o is AbilityInstance { IsActive: false });

            if (_buffTags.Count > 0)
            {
                _buffTags.RemoveAll(t =>
                {
                    if (now >= t.expiryTime
                        || (t.owner is AbilityInstance { IsActive: false }))
                    {
                        OwnedTags.RemoveTag(t.tag);
                        return true;
                    }
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

        /// <summary>
        /// 施加技能冷却。写入 cooldownEndTimes + cooldownAbilityTags 映射，
        /// CleanupExpiredCooldowns 在冷却到期后自动清理 abilityTag。
        /// </summary>
        /// <param name="overrideDuration">覆写冷却时长。>0 时使用覆写值，≤0 时使用 ability.cooldownDuration。</param>
        public void StartCooldown(AbilitySO ability, float overrideDuration = -1f)
        {
            if (ability == null) return;

            float duration = overrideDuration > 0f ? overrideDuration : ability.cooldownDuration;
            if (duration <= 0f) return;

            if (ability.abilityTag != null)
                AddCooldown(ability.abilityTag.FullTag, duration);

            if (ability.sharedCooldownTags != null)
                foreach (var tag in ability.sharedCooldownTags)
                    if (tag != null)
                        AddCooldown(tag.FullTag, duration);
        }

        public bool IsOnCooldown(string tag)
        {
            return !string.IsNullOrEmpty(tag) && cooldownEndTimes.TryGetValue(tag, out var end) && Time.time < end;
        }

        public void AddBuffTags(RdTagDefSO[] tags, float expiryTime, object owner = null)
        {
            if (tags == null || expiryTime <= Time.time) return;
            foreach (var t in tags)
            {
                if (t != null) _buffTags.Add((t.FullTag, expiryTime, owner));
            }
        }

        #region Physics Callbacks

        private void OnTriggerEnter(Collider other)
        {
            NotifyPassiveEvent(ETriggerEvent.OnEnterArea, other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            NotifyPassiveEvent(ETriggerEvent.OnExitArea, other.gameObject);
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
            if (ability.cooldownDuration <= 0f || ability.abilityTag == null) return true;
            return !IsOnCooldown(ability.abilityTag.FullTag);
        }

        private void ApplyCooldown(AbilitySO ability)
        {
            if (ability.cooldownDuration <= 0f || ability.abilityTag == null) return;
            AddCooldown(ability.abilityTag.FullTag, ability.cooldownDuration);
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

            if (GatingConditionCallback != null)
            {
                var reason = GatingConditionCallback(ability);
                if (reason != null)
                {
                    Debug.Log($"[Ability] ② Rejected: {ability.internalName} — condition: {reason}");
                    return false;
                }
            }

            // ── ③ Cost ──
            if (ability.selfEffects != null)
            {
                var costs = new List<CostEffectSO>();
                foreach (var effect in ability.selfEffects)
                {
                    if (effect is CostEffectSO cost && cost.def != null)
                        costs.Add(cost);
                }

                if (costs.Count > 0)
                {
                    var costArray = costs.ToArray();
                    if (PreviewCostCallback == null)
                    {
                        Debug.LogError($"[Ability] PreviewCostCallback is null");
                        return false;
                    }
                    var rejectReason = PreviewCostCallback(costArray);
                    if (rejectReason != null)
                    {
                        Debug.Log($"[Ability] ③ Cost fail: {ability.internalName} — {rejectReason}");
                        return false;
                    }

                    ApplyCostCallback?.Invoke(costArray);
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