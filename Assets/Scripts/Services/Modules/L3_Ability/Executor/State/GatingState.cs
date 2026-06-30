using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// ② 门控 State。冷却 → 互斥 → 外部条件，三道闸门串联。
    /// 通过 → _next（BeforeExe）；任一失败 → _rejected。
    /// </summary>
    public class GatingState : AbilityState
    {
        public override EActiveAbilityState Id => EActiveAbilityState.CanEnter;

        private readonly AbilityState _next;
        private readonly AbilityState _rejected;

        public GatingState(AbilityState next, AbilityState rejected)
        {
            _next = next;
            _rejected = rejected;
        }

        public override IState<SActiveAbilityContext> OnTick(SActiveAbilityContext ctx, float dt)
        {
            var a = ctx.Ability;
            var e = ctx.Executor;

            // ── 1. 冷却 ──
            var cdKey = ResolveCooldownKey(a);
            if (cdKey != null && e.IsOnCooldown(cdKey))
            {
                Debug.LogWarning($"[Gating] Rejected: {a.internalName} — on cooldown ({cdKey})");
                return _rejected;
            }

            // ── 2. 互斥 ──
            if (!a.overrideExclusion)
            {
                if (a.abilityTag?.Parent != null && e.OwnedTags.HasTag(a.abilityTag.Parent.FullTag))
                {
                    Debug.LogWarning($"[Gating] Rejected: {a.internalName} — mutual exclusion ({a.abilityTag.Parent.FullTag})");
                    return _rejected;
                }

                if (a.extraExclusionTags != null)
                {
                    foreach (var tag in a.extraExclusionTags)
                    {
                        if (tag != null && e.OwnedTags.HasTag(tag.FullTag))
                        {
                            Debug.LogWarning($"[Gating] Rejected: {a.internalName} — extra exclusion ({tag.FullTag})");
                            return _rejected;
                        }
                    }
                }
            }

            // ── 3. 外部条件 ──
            if (e.ConditionCallback != null)
            {
                var reason = e.ConditionCallback(a);
                if (reason != null)
                {
                    Debug.LogWarning($"[Gating] Rejected: {a.internalName} — condition: {reason}");
                    return _rejected;
                }
            }

            Debug.Log($"[Gating] Passed: {a.internalName} → BeforeExe");
            return _next;
        }

        private static string ResolveCooldownKey(AbilitySO ability)
        {
            if (ability == null || ability.cooldownDuration <= 0f) return null;
            return ability.sharedCooldownTag != null
                ? ability.sharedCooldownTag.FullTag
                : $"Ability.Cooldown.{ability.internalName}";
        }
    }
}
