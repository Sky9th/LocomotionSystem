# 2026-07-05 — Caster Modifier + OnHit Notification + Callback Rename

## Background

After the DamageEffectSO type split (v0.38.0), two pipeline gaps remained:
1. **S4.1** — `EffectCallback` was removed during the refactor, so Strength did not affect outgoing damage
2. **S4.3** — `AbilityReactor.Resolve()` was fire-and-forget; the caster had no way to know if a hit landed, blocking OnHit passives, lifesteal, and combo logic

Callback naming was also inconsistent — `EffectCallback`/`ConditionCallback`/`PeekStatCallback`/`ModifyStatCallback` were vague or misleading.

## Changes

### S4.1 Caster Attribute Modifier
- `GroundSystemConfigSO` — added `strengthDamageBonus = 0.05` (+5% per point) under "Combat Formula" header, with TODO to migrate to world-level config
- `CharacterConst.PropertyPath.Attributes.Strength` — new path constant
- `CharacterCombat.OnModifyOutgoingDamage` — reads Strength from `ctx.Properties`, applies `outgoingDamage × (1 + strength × bonus)`
- `ExecutionState.BuildDamageInfo` — restored `OutgoingDamageCallback` call after weapon×skill formula

### S4.3 Reactor→Caster OnHit Notification
- `AbilityReactor.Resolve` — return type `void` → `float` (finalAmount)
- `AbilityExecutor.OnHitResolved` — new `Action<SDamageInfo, float>` callback
- `ExecutionState.OnTick` — captures `reactor.Resolve()` return value, invokes `OnHitResolved`
- `CharacterCombat.OnHitResolved` — wires to `NotifyPassiveEvent(ETriggerEvent.OnHit, target)`, activating the already-built passive pipeline (`InstanceManager.GetByTrigger` → `_pendingPassiveStarts` → `TickPassives`)

### Callback Rename
- `EffectCallback` → `OutgoingDamageCallback`
- `ConditionCallback` → `GatingConditionCallback`
- `PeekStatCallback` → `PreviewCostCallback`
- `ModifyStatCallback` → `ApplyCostCallback`
- `OnEffectModify` → `OnModifyOutgoingDamage`

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| `strengthDamageBonus` in GroundSystemConfigSO | A: New CombatConfigSO → over-engineering for one field. B: Hardcode constant → rejected by reviewer. | GroundSystemConfigSO already holds global formula params; marked TODO to move later |
| OnHit notification via ExecutionState callback | A: Reactor directly calls caster's Executor → couples Reactor to caster-side logic | ExecutionState already has both sides' references; cleaner separation |
| `Resolve` returns float instead of adding output parameter | `out float finalAmount` → awkward call site | Simple return value, minimal API change |

## Known Issues

- [ ] `OutgoingDamageCallback` passes `null` for EffectSO parameter — vestigial, not yet needed (P2)
- [ ] `strengthDamageBonus` in Character SO is temporary; belongs in world-level config (P2)
- [ ] `NotifyPassiveEvent(OnHit)` is wired but no passive abilities with OnHit trigger exist yet to test against (P2)

## Cross-References

### Related Sessions
- [2026-07-05-damage-pipeline-refactor.md](2026-07-05-damage-pipeline-refactor.md) — DamageEffectSO type split that removed the EffectCallback this session restores

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S4.1, S4.3 marked complete

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/damage-effect-so.md](../tech/L2-services/L2-modules/L3-ability/damage-effect-so.md) — updated in v0.38.0

### Flag for Design Doc Creation
- [x] No design doc needed — internal pipeline improvements, no player-visible changes.
