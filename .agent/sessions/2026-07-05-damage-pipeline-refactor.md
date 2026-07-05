# 2026-07-05 — Damage Pipeline Refactor: DamageEffectSO Type Split + SDamageInfo per-Channel

## Background

The damage pipeline had three long-standing structural issues:
1. **Cartesian product pollution** — `ComputeDamage` iterated all weapon channels × all ability modifiers, applying e.g. a Slash +15 ×2 modifier to a Fire DOT channel
2. **Single Amount flattening** — `SDamageInfo.Amount` was a single float, so the Reactor couldn't distinguish Slash from Fire damage for per-tag resistance routing
3. **Semantic conflation** — `DamageEffectSO` served both as entity damage channel (weapon `baseValue`) and skill modifier (ability `modAdd`/`modMult`), with no type-level distinction

This is part of the ability pipeline architecture cleanup tracked in the short-term plan (S3.5 → S4).

## Changes

### Core Architecture
- **New `DamageModifierEffectSO`** — skill-side damage modifier with `targetTag` + `modAdd` + `modPercent`. Replaces `DamageEffectSO.modAdd`/`modMult` on the ability side
- **New `DamageEntry`** — per-channel damage data struct (`Tag` + `Amount` + `Duration` + `Interval`), replacing the flattened `Amount` + `EffectTags`
- **`DamageEffectSO`** — slimmed to `baseValue` only; semantically now exclusively an entity damage channel (weapon/body/trap)

### SDamageInfo
- `Amount` (float) + `EffectTags` (RdTag[]) → `Damage` (DamageEntry[]) + `TotalAmount` (convenience property)
- `EffectTags` was never read anywhere — clean removal

### ExecutionState
- `ComputeDamage`: cartesian product replaced by tag-matching loop; formula changed from `(base+add)×mult` to `base×(1+Σ%)+Σadd` to prevent multiplicative explosion
- `MatchTag`: exact reference equality → hierarchical `RdTag.IsAncestorOf()`, so modifier `Damage.Physical.Slash` matches channel `Damage.Physical.Slash.Heavy`
- Entity channels collected from weapon + body (unarmed fallback still TODO)

### AbilityReactor
- `Resolve`: iterates `Damage[]`, sums instant entries before calling `ResolutionCallback` once (fixed per-entry loop bug), DOT entries skipped with TODO
- Marked TODOs for DOT landing, per-tag resistance routing, Avoidance/Mitigation/Absorption split, damage type conversion

### CharacterCombat
- `OnResolveDamage`: `hit.Amount` → `hit.TotalAmount`

### UI
- `DamageNumberOverlay`/`DamageNumberWidget`: `hit.Amount` → `hit.TotalAmount`

### Editor
- `ActiveAbilitySOEditor`/`PassiveAbilitySOEditor`: display `DamageModifierEffectSO` entries with new formula preview
- `EffectImportExport`: added `DamageMod` type support (CreateInstance, validTypes, EffectTypeString, summary count)

### Assets
- Deleted 19 old `DamageEffectSO` modifier assets (baseValue=0, modMult>1.0)
- Created 19 `DamageModifierEffectSO` assets (DamageMod_*)
- Entity channel assets stripped of `modAdd`/`modMult`/`priority`
- `Blade.asset` → `MeleeWeaponSO`, `Pistol.asset` → `RangedWeaponSO` (were `ItemDefSO`)
- `abilities_all.json`: removed no-op `DamageEffectSO` refs, HeavyChop uses `DamageMod_Physical_Slash_Heavy`
- `effects_all.json`: migrated to `DamageMod` type with `modPercent`/`targetTag`

### Deletions
- `AbilityEffects.cs` — deprecated, zero references, old constructor incompatible with refactored `SDamageInfo`

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| Split DamageEffectSO / DamageModifierEffectSO | A: Keep single type, use field convention (baseValue=0 means modifier) — fragile, caused Cartesian bug. B: Fully generic EffectSO[] with runtime casting — already the plan for other effects | Type-level distinction makes the architecture self-documenting; Editor can validate that DamageEffectSO never appears in skill targetEffects |
| Additive percent stacking instead of multiplicative | Keep old `(base+add)×mult` — causes ×3×4 explosion with multiple modifiers | Additive `base×(1+Σ%)+Σadd` prevents runaway scaling; flat add bonus is not multiplied, preventing double-dipping |
| Keep `EffectSO[]` polymorphic array, no split | Fix types within the array via `OfType<>()` | New Effect types zero-cost to add; serialization stable; order within array may matter for future priority-based effects |
| Exe side computes outgoing damage, Reactor side mitigates | Move all computation to Reactor — Reactor would need caster's weapon entity and CharacterCombat, creating cross-character coupling | Caster knows weapon/skill stats, target knows armor/resistance. Clean separation of concerns matching UE GAS pattern |

## Known Issues

- [ ] DOT channels carried in `DamageEntry[]` but not landed — needs `FloatModifier` ExpiryTime + Reactor DOT registration (P1)
- [ ] Body/Unarmed channel still TODO — no weapon = zero damage (P1)
- [x] `ResolutionCallback` was called per-entry in a loop, producing wrong results — fixed to call once with summed instant amount
- [ ] Per-tag resistance routing still single callback — `OnResolveDamage` applies uniform Endurance mitigation to `TotalAmount`, ignores per-channel tags (P2)
- [ ] `RangedWeaponSO.GetDamageEffects` leaks temporary `ScriptableObject.CreateInstance` (P2 — on low-priority list)

## Cross-References

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S3.5 (旧代码清理), S4 (Combat 管线补完), low-priority tech debt

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md) — ExecutionState / Reactor pipeline reference

### Related Sessions
- [2026-07-05-ability-instance-model.md](2026-07-05-ability-instance-model.md) — AbilityInstance unified model (same session)
- [2026-07-05-hit-reaction-pipeline.md](2026-07-05-hit-reaction-pipeline.md) — hit reaction pipeline (same session)
- [2026-07-05-properties-depth-integration.md](2026-07-05-properties-depth-integration.md) — Properties depth integration (same session)

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactor. No player-visible behavior change. Formula change is preventive (no current skills stack multiple modifiers).
