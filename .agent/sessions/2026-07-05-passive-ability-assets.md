# 2026-07-05 — Passive Ability Asset Layer

## Background

The project had zero passive abilities. The `AbilityForest` and `InstanceManager`
infrastructure was built but never populated — every AbilityTree node had `passive: null`,
every `AbilityTreeSO` referenced only `ActiveAbilitySO` assets. To enable the passive
trigger pipeline (OnHit/OnKill/OnDamaged/etc.) and the PassiveBarOverlay UI, we first
needed the full asset chain: JSON definitions → Effect/Buff/Tag `.asset` files →
AbilityTree integration.

## Changes

### L3_Ability — JSON Asset Definitions
- `abilities_all.json` — added 5 Passive ability entries (ToughBody/DeepWound/BerserkerBlood/LastStand/Caltrop), covering OnEquip/OnHit/OnKill/OnLowHP/OnEnterArea trigger types
- `effects_all.json` — added 4 Buff effects: Buff_ToughBody_MaxHP (MaxHP ×1.2), Buff_BerserkerBlood (AttackSpeed ×1.15 + MoveSpeed ×1.1), Buff_Debuff_Slow_40 (MoveSpeed ×0.6), Buff_LastStand_DR (Endurance ×1.5 + KnockdownResist +25)
- `tags_all.json` — added 5 ability tags under `Ability.Definition.Passive.*` + 4 effect tags under `Ability.Effect.Buff.*`
- `abilityTrees_all.json` — added `Human_InnatePassives` tree (no weapon/grip restrictions, 5 root nodes)

### L3_Ability — Editor Tool Fixes
- `AbilityImportExport.cs` — fixed passive `.asset` path from `Passives/` to `Definition/Passives/`, matching the `Definition/Actives/` convention
- `AbilityTreeImportExport.cs` — existing trees now update (via `ApplyTreeFields`) instead of skipping; extracted shared field-application logic

### L2_UI — Enum Pre-plumb
- `UIOverlayId.cs` — added `PassiveBarOverlay` enum value (script + prefab wiring pending next session)

### Generated Assets (Unity Editor auto-import)
- 5 × `Passive_*.asset` in `Definition/Passives/`
- 4 × Buff `.asset` files in `Effects/Buff/`
- 9 × tag `.asset` files in `Tags/Ability/`
- 1 × `Human_InnatePassives.asset` in `AbilityTrees/Innate/`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Passive assets under `Definition/Passives/`, not root `Passives/` | A: keep `Passives/` at root → inconsistent with `Definition/Actives/` | Follows existing directory convention |
| Separate `Human_InnatePassives` tree — passives not mixed into weapon trees | A: add passive nodes to existing Human_Innate tree → passives would only work when unarmed (grip restriction) | Passives with empty weapon/grip tags work regardless of equipment |
| LastStand uses Endurance multiplier, not new DamageTaken PropertyDef | A: add DamageTaken property → more precise but heavyweight for initial test passives | Endurance already has DR formula (×0.03/pt); new PropertyDef deferred |
| 5 passive skills cover 5 distinct trigger types | A: fewer skills with same trigger → less test coverage | Each trigger type has different code paths; need full coverage for pipeline verification |

## Known Issues

- [ ] MoveSpeed PropertyDef lives in LegArmor tree, not Actor tree — Buff adjuncts referencing MoveSpeed may have no target property on the character's PropertyTable (P2 — add MoveSpeed to Actor tree or create separate actor-speed property)
- [ ] No DamageTaken/DamageReduction property exists — precise DR scaling requires a new PropertyDef (P3 — deferred to post-pipeline-verification)
- [ ] Runtime trigger pipeline not yet wired — `SyncInstances` has zero callers, passive skills cannot actually fire (P0 — next session: CharacterActor bridge + NotifyPassiveEvent dispatch points)
- [ ] PassiveBarOverlay.cs script not yet created — only the enum and prefab skeleton exist (P1 — next session alongside pipeline wiring)

## Cross-References

### Related Sessions
- [2026-07-05-passive-ability-pipeline.md](2026-07-05-passive-ability-pipeline.md) — planned follow-up: pipeline wiring + PassiveBar UI

### Related Plans
- [../plans/json-foamy-backus.md](../plans/json-foamy-backus.md) — detailed implementation plan for this session

### Related Tech Docs
- `tech/L3-ability/ability-forest.md` — AbilityForest architecture (ResolvedPassives output added)
- `tech/L3-ability/passive-ability-so.md` — PassiveAbilitySO config structure
- `tech/L3-ability/instance-manager.md` — InstanceManager trigger indexing

### Flag for Design Doc Creation
- [ ] No design doc needed — passive skill system design was already established; this session was asset population and tool fixes.
