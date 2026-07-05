# 2026-07-06 — Passive Pipeline Runtime + Max Expansion + PassiveBar UI

## Background

The previous session (07-05) established the passive ability asset layer (5 passives, 4 buffs, 9 tags, 1 tree). This session wires the runtime: bridging AbilityForest→InstanceManager, adding trigger dispatch points for OnHit/OnKill/OnDamaged, fixing the effect application pipeline so Buff adjuncts actually land, and extending FloatAdjunct to support max expansion (needed for +MaxHP passives). Also builds the PassiveBarOverlay UI.

## Changes

### L3_Ability — Pipeline Wiring
- `CharacterActor.Start()` → `SyncPassivesFromForest()` bridges `ResolvedPassives` to `SyncInstances` + fires `OnEquip` trigger for Persistent passives
- `CharacterCombat`: added OnKill dispatch (HP≤0 → caster's executor), OnDamaged dispatch, null guards on all trigger paths
- `AbilityExecutor`: `OnTriggerEnter/Exit` migrated from legacy `runtimePassives` iteration to `NotifyPassiveEvent`
- `AbilityReactor.Resolve`: moved non-damage effects (Buff/Tag) BEFORE the early-return on empty Damage[], so passives without weapon channels still apply buffs
- `AbilityReactor.ApplyBuff`: uses `PropertyTable.TryGetPath` to convert PropertyDefSO→tree path; guards `maxMultiply<=0`→1 for backward compat
- `ExecutionState.CollectEntityChannels`: now also collects `DamageEffectSO` from ability's `targetEffects` for passive damage sources (e.g., bleed, caltrop)

### L3_Properties — Max Expansion
- `FloatAdjunct`: added `MaxAdd`/`MaxMultiply` fields
- `FloatState.Effective`: computes `effectiveMax = Max×ΠMaxMultiply+ΣMaxAdd`, clamps to expanded max instead of fixed `Max`

### L2_UI — PassiveBarOverlay
- `PassiveBarOverlay.cs`: new overlay, patterned after `AbilityBarOverlay` (no keybinds, no selected state)
- `UIOverlayId`: added `PassiveBarOverlay` enum
- `UIService.HandleGameState`: registered `ShowOverlay(PassiveBarOverlay)`
- `AbilityQuery`: added `PassiveAbilities` property + `GetPassiveCooldownRemaining()`

### Config & Editor
- `SBuffAdjunct`: added `maxAdd`/`maxMultiply` fields (C#9 compat: no field initializers)
- `EffectImportExport`: `BuffAdjunctEntry` updated for import/export of new fields

### Asset Adjustments
- BerserkerBlood: changed adjuncts from AttackSpeed+MoveSpeed → Agility×1.5 (both unavailable in player PropertyTable)
- ToughBody: changed property from MaxHP→HP, added `maxMultiply=1.2`
- All 7 Buff adjunct entries in JSON updated with `maxAdd`/`maxMultiply` fields

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| Max expansion via FloatAdjunct fields, not separate property | A: add MaxHP to PropertyTable→needs HP-MaxHP linkage code; B: modify FloatState to accept dynamic max | MaxAdd/MaxMultiply is minimal, backward-compatible, works with existing Buff system |
| BerserkerBlood uses Agility not AttackSpeed/MoveSpeed | A: add AttackSpeed+MoveSpeed to Actor PropertyTree | Agility already in tree, drives both attack speed and move speed via existing formulas — no new properties needed |
| OnEquip fires explicitly after SyncInstances | A: auto-fire on Activate→would require lifecycle awareness in InstanceManager | Explicit is clearer; OnEquip is a one-shot trigger, Persistent lifecycle means instance persists but FSM runs once |
| OnKill dispatched from victim's OnApplyDamage to caster's executor | A: dispatch from caster side→caster's pipeline already completed by then | Victim side is the only reliable detection point for HP≤0 |

## Known Issues

- [ ] OnLowHP trigger has no dispatch point — needs per-frame HP threshold check with on/off toggle (P1)
- [ ] OnDodge/OnComboStage triggers have no dispatch points — depend on unimplemented systems (P2)
- [ ] DoT damage entries (IsDot=true) are not ticked — reactor only sums instant entries (P1)
- [ ] Caltrop OnEnterArea untested — no Trap entity in current test scene (P2)
- [ ] MoveSpeed adjuncts (Slow_30/Slow_40) may fail if target PropertyTable lacks MoveSpeed def (P2 — same root cause as AttackSpeed)

## Cross-References

### Related Sessions
- [2026-07-05-passive-ability-assets.md](2026-07-05-passive-ability-assets.md) — asset layer for the 5 passive skills

### Related Plans
- [../plans/json-foamy-backus.md](../plans/json-foamy-backus.md) — implementation plan

### Related Tech Docs
- `tech/L2-services/L2-modules/L3-ability/ability-editor.md` — updated last session
- `tech/L2-services/L2-modules/L3-properties/` — FloatState/FloatAdjunct updated
- `tech/L2-services/L2-modules/L3-ability/ability-reactor.md` — Resolve flow changed
- `tech/L2-services/L2-modules/L3-ability/execution-state.md` — CollectEntityChannels extended

### Flag for Design Doc Creation
- [x] No design doc needed — passive skill system design already established; this session is runtime implementation and UI.
