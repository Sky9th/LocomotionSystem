# 2026-06-27-event-system-unification

## Background

Event 系统两套并存：`EventChannelBase.OnRaised`（无参拉）和 `GameEvent<T>.Raise(T)`（推）。
Input 用拉模式（OnRaised 通知后回读 SO 属性），Entity 用推模式。同一继承链两个子类互不理睬。
统一为推模式，删 `EventChannelBase` 和 `InputEventBase`，`GameEvent<T>` 成为唯一通道。

## Changes

### 删除
- `EventChannelBase.cs` + `InputEventBase.cs` — 拉模式根
- `ButtonInputEventSO.cs` / `Vector2InputEventSO.cs` / `FloatInputEventSO.cs` — 中间空壳

### 事件核心
- `GameEvent.cs` — 新增非泛型 `GameEvent` 标记类 + `GameEvent<T>` 直继 `ScriptableObject`
- `InputService.cs` — 重写，`InputActionAsset` + `GameEvent[]` 名字自动匹配绑定
- 新增 3 个 payload struct：`SButtonInputPayload`, `SVector2InputPayload`, `SFloatInputPayload`

### 叶子 SO
- 21 叶子 SO 类头改写：`ButtonInputEventSO` → `GameEvent<SButtonInputPayload>`
- 2 叶子 SO：`Vector2InputEventSO` → `GameEvent<SVector2InputPayload>`

### 订阅方
- `PlayerInput.cs`：10 处 `OnRaised +=` → `Register(Action<T>)`，handler 接收 payload
- `GameStateService.cs`：1 处同模式

### 引用更新
- `EventHub.cs` / `PassiveAbilitySO.cs` / `AbilityEditorMiddlePanel.cs` / `AbilityImportExport.cs` — `EventChannelBase` → `GameEvent`

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| `GameEvent` 非泛型标记类 | A: `ScriptableObject` 直接做类型约束 — 太宽。B: 接口 — SO 字段不能存接口。 | 比 SO 窄，Unity 可序列化 |
| InputService 名字匹配 | A: 显式 ButtonBinding[] 数组 — 维护成本高。B: 全自动 FindAssets — Runtime 不可用。 | 拖一次 GameEvent[]，名字自动匹配 |
| 删中间 SO，叶子直继 GameEvent<T> | A: 保留空壳 — 无意义。 | 叶子 = 事件类型，中间 = 冗余 |
| 不带日志的干净版本 | | 调试日志已移除，保留 LogError 快定位配置缺失 |

## Known Issues

- [ ] `Esc` 和 `RightClick` action 无对应 SO — 在 eventChannels 中缺失 (P2)
- [ ] GameManager.prefab 上 InputService 需要手动重建（序列化字段类型变更）(P1 — Unity Editor 步骤)
- [ ] 旧 InputEventBase SO 资产需在 Unity 中清理 (P2)

## Cross-References

### Related Sessions
- [2026-06-27-entity-service-data-model.md](2026-06-27-entity-service-data-model.md) — EntityService + Entity 数据模型落地，事件通道用户

### Related Tech Docs
- tech/L1-core/events/ — GameEvent 需更新
- tech/L2-services/L2-input/ — InputService 需更新

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactoring, no player-facing changes.
