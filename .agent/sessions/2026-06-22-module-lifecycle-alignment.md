# 2026-06-22 — Module Lifecycle Alignment

## Background

The module system's lifecycle documentation (module-lifecycle.md + module-system.md) was finalized in a previous session, but the actual code had drifted. The most critical gap: `OnAssembleAll` ran in `Start` instead of `Awake`, meaning children's `OnEnable` fired before `OnAssemble` — violating the documented timing guarantee. Additionally, `RegisterService` was called in `OnWire` (cross-module wiring phase), creating a fragile ordering dependency where Service A's `TryResolveService<B>` could fail if B happened to be later in the iteration list.

This session aligned all code to the documented lifecycle standard, then further clarified the OnWire contract: "wire the plugs, don't send current."

## Changes

### Core Module System (L1_Core/Modules/)
- Renamed `IInitializable` → `IModuleChild` — Hub no longer implements it
- Renamed `ModuleBehaviour` → `ModuleHub` — added `GetComponentsInChildren` scan in Awake, moved OnAssembleAll from Start to Awake, removed OnAssemble/OnWire virtual methods
- Renamed `ModuleComponent` → `ModuleChildMono` — removed self-registration in Awake (parent discovers children now)
- Renamed `Module` → `ModuleChild` — constructor self-registration unchanged
- `ModuleRegistry` — generic type aligned to `IModuleChild`

### Hub Subclass Migration (3 files)
- **GameService**: Awake creates GameContext before base.Awake(); Start resolves EventDispatcher via TryResolveService (was manual GetComponent+Register), removed _wiredCount + NotifyServiceWired
- **CharacterActor**: C# child creation moved from override OnAssemble to pre-base.Awake; Start does base.Start() + post-wire modifier
- **AnimationBrain**: AddComponent drivers before base.Awake(); Start splits into pre-wire (layer setup) + base.Start() + post-wire (footstep bridge)

### Service Lifecycle Realignment (10 files)
- Moved `RegisterService(this)` from OnWire to OnAssemble in all 10 L1/L2 services
- Removed `NotifyServiceWired()` from all call sites (GameService definition also removed)
- **InputService**: Removed `EnableEvent()` from OnAssemble (runtime activation belongs in OnEnable)
- **PathfindingService**: Moved `graph.Scan()` from OnWire to OnAssemble (initialization, not wiring)
- **GameStateService**: Moved initial `ApplyState` from OnWire to Start; added `UpdateSnapshot` in ApplyState
- **UIService**: Removed broken snapshot fallback (no longer needed — Start publishes after all subscribers ready)

### L3 Character Module (10 files)
- All `ModuleComponent` → `ModuleChildMono`, `ModuleBehaviour` → `ModuleHub`, `Module` → `ModuleChild`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Hub discovers children via GetComponentsInChildren (parent→child) | A: Keep child self-registration (child→parent). Was working but semantically inverted — parent should own its tree. | Doc standard defines parent→child. Also enables static analysis of the tree structure. |
| RegisterService in OnAssemble (not OnWire) | A: Keep in OnWire but sort registration order. B: Two-phase registration (pre-register then confirm). | RegisterService is "I exist" — same as C# ModuleChild constructor self-registration. Putting it in OnAssemble guarantees all services registered before any OnWire runs, eliminating TryResolveService ordering fragility. |
| OnWire = Subscribe only, no Publish | A: Allow initial Publish in OnWire (old pattern). Required subscribers to be earlier in iteration order. | OnWire is "connect the wires." Publish after all connections are made (in Start) eliminates missed-broadcast bugs. |
| Remove wiredCount verification entirely | A: Keep NotifyServiceWired but make it fire from OnAssemble. | Registration is synchronous within base.Awake(). All children are registered by the time Awake returns. A separate count provides no additional safety. |

## Known Issues

- [x] `BaseAnimationDriver.OnEnable/OnDisable` cross-module registration — fixed, moved to OnWire/OnDestroy
- [ ] `PathfindingService._dispatcher` field stored but never used — dead code carried from old version (P2)
- [ ] EventDispatcherService is `[Obsolete]` — EventHub replacement not yet complete (P1 — future session)

### Character Module Lifecycle Fixes (same session)
- **BaseAnimationDriver**: OnEnable/OnDisable cleared, OnDestroy added with `brain?.UnregisterDriver`
- **LocomotionDriver**: removed empty OnEnable/OnAssemble overrides, removed redundant `GetComponent` in OnWire
- **CharacterAudio**: `brain` reference moved to OnAssemble, OnDestroy added to unsubscribe OnFootstep
- **CharacterCombat**: 7 Ability/Reactor callback assignments moved from OnAssemble to OnWire
- **PathfindingAgent**: `Teleport` moved from OnWire to OnAssemble
- **AnimationBrain**: added `: IModuleChild`, self-registers in Awake, layer setup moved from Start to OnWire
- **IModuleChild / ModuleHub / ModuleChildMono**: lifecycle boundary descriptions added to code comments
- **module-lifecycle.md**: added full phase responsibility table (Awake→OnDestroy)

### Decisions (Character fixes)

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| AnimationBrain 显式 `: ModuleHub, IModuleChild` | A: 改 ModuleHub 基类让所有 Hub 都实现 IModuleChild → 破坏 Hub 不实现接口的约定，根 Hub 无父调用。B: AnimationBrain 改为 ModuleChildMono 内嵌 Registry → 混淆 Hub/Child 职责。 | 显式接口是精确手术——只有需要既父又子的 Hub 才加，根 Hub (GameService) 不受影响 |
| OnEnable/OnDisable 清空而非保留空壳 | A: 保留 `base.OnEnable()` 调用链 → 无意义调用。B: 保留注释说明 → 注释会过时。 | 清空是最干净的合约表达 |

## Cross-References

### Related Plans
- [../plans/spicy-sprouting-bachman.md](../plans/spicy-sprouting-bachman.md) — implementation plan for this alignment

### Related Tech Docs
- [../tech/L1-core/module-lifecycle.md](../tech/L1-core/module-lifecycle.md) — lifecycle standard + phase boundary table
- [../tech/L1-core/module-system.md](../tech/L1-core/module-system.md) — updated to reflect current architecture
- [../tech/L2-services/L2-modules/L3-character/lifecycle-audit.md](../tech/L2-services/L2-modules/L3-character/lifecycle-audit.md) — Character module per-phase audit table
- Deleted: `iinitializable.md`, `module-behaviour.md`, `module-component.md`, `module.md`, `module-registry.md` — replaced by above two docs

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor, no player-facing behavior changes.
