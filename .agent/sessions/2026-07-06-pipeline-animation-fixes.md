# 2026-07-06 — Pipeline 动画卡死修复 + 激活资产补全

## Background

Previous session landed the passive trigger pipeline + PassiveBarOverlay UI. Testing revealed
that spamming Q caused the active ability pipeline to freeze permanently. Investigation traced
through three root causes in the animation arbitration layer, plus a missing `recoveryDuration`
on all Activation assets that caused RecoveryState to complete instantly.

## Changes

### DriverArbiter — 队列保留
- Previously: rejected Ability→Ability requests cleared the queue entirely
- Now: `return` instead of `queue.Clear()` when rejecting, so the request retries next frame
- Added `_skipCompletionThisFrame` flag: prevents `CheckCompletion` from reading stale
  `NormalizedTime` in the same frame as `AcceptRequest` (which sets `state.Time=0`)

### AbilityDriver — 事件清理
- `state.Events(ref _fireSequence).Clear()` before re-adding fire marker
- Fixes old clip events persisting across same-clip replays, preventing new fire marker callbacks

### Pipeline 诊断
- `TryUse` logs when blocked, showing current pipeline state
- `CompletedState`/`RejectedState` log on enter
- `WindupState`/`RecoveryState` warn when `_duration=0` and `!SkipAnim` (missing activation)

### 激活资产补全
- `activations_all.json`: added `recoveryDuration` to all 10 activations (computed from clip frame count / 30 FPS - windup - fire)
- `ActivationImportExport.cs`: DTO + export + ApplyFields updated for `recoveryDuration`

### 清理
- Removed debug logs from PassiveBarOverlay, AbilityReactor, CharacterActor, CharacterCombat

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| Rejected requests stay in queue (don't clear) | A: accept and interrupt → causes first skill to be cut short. B: clear queue silently → request lost | Retry next frame is the only correct behavior for same-type non-preemptive arbitration |
| `_skipCompletionThisFrame` flag vs. reading NormalizedTime correctly | A: force Animancer to update time → no public API for this | Simpler to skip one frame; NormalizedTime updates by next Resolve |
| `state.Events.Clear()` rather than `_fireSequence = null` + reassign | A: reassign null → Events(ref) returns existing sequence anyway (null ref ignored by ref return) | Clear() is explicit and guaranteed |

## Known Issues

- [ ] `recoveryDuration` based on 30 FPS assumption — actual frame rate may differ per clip (P2 — verify in Unity)
- [ ] TerminalStates OnEnter logs are kept for debugging; should be reduced or removed before production (P3)
- [ ] OnLowHP/OnDodge triggers still unimplemented (P2)

## Cross-References

### Related Sessions
- [2026-07-05-passive-ability-assets.md](2026-07-05-passive-ability-assets.md) — passive asset layer
- [2026-07-06-passive-pipeline-runtime.md](2026-07-06-passive-pipeline-runtime.md) — pipeline runtime + PassiveBar

### Related Tech Docs
- `tech/.../L3-ability/ability-editor.md` — ActivationImportExport updated
- `tech/.../L3-character/L4-stats/float-state.md` — FloatAdjunct max expansion

### Flag for Design Doc Creation
- [x] No design doc needed — bug fixes and asset data completion.
