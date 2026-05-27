# TimeService · 时间管理

> `Core/Time/TimeService.cs` — Gameplay/UI 时间线分离，继承 BaseService

## 调用链

```
被谁调:
  GameService.Bootstrap()                    → Register()
  EventDispatcher                            → HandleTimeScaleRequested / HandleSceneLoadStart / HandleSceneLoadComplete / HandleGameStateChanged
  Unity Engine                               → OnDestroy()

调谁:
  GameContext                                → RegisterService()
  Time (Unity)                               → timeScale 读写
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册自身 |
| 依赖 | EventDispatcher | 订阅 4 种事件 |
| 依赖 | 03-input | 接收 SIActionWorldSpeed（慢放/快进） |
| 依赖 | SceneService | 接收 SSceneLoadStart/Complete（加载时暂停） |
| 依赖 | GameStateService | 接收 SGameState（暂停/恢复） |

## 公开属性

```csharp
(无 — 纯内部逻辑)
```

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 注册自身 + 保存当前 `Time.timeScale` 为 `defaultScale`

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 4 种事件
  - `SIActionWorldSpeed` → 速度控制
  - `SSceneLoadStart` / `SSceneLoadComplete` → 加载中冻结
  - `SGameState` → 暂停/恢复

### HandleTimeScaleRequested()
```csharp
private void HandleTimeScaleRequested(SIActionWorldSpeed action, MetaStruct meta)
```
- **用途**: 更新 `defaultScale`（clamp 到 `[minScale, maxScale]`），应用 timeScale
- **备注**: 加载中或暂停时不响应（`isSceneLoading || isGamePaused` 时 return）

### HandleSceneLoadStart / HandleSceneLoadComplete()
- **用途**: 设置 `isSceneLoading` 标志 → 调用 `ApplyFreeze()`

### HandleGameStateChanged()
```csharp
private void HandleGameStateChanged(SGameState state, MetaStruct meta)
```
- **用途**: `isGamePaused = (CurrentState == Paused)` → 调用 `ApplyFreeze()`

### ApplyFreeze()
```csharp
private void ApplyFreeze()
```
- **用途**: 根据 `isSceneLoading || isGamePaused` 设置 `Time.timeScale = 0` 或恢复 `defaultScale`

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消所有订阅 + `RestoreDefaultScale()`

### RestoreDefaultScale()
```csharp
private void RestoreDefaultScale()
```
- **用途**: 恢复 `Time.timeScale = defaultScale`（仅在 PlayMode 下）

## 时间控制优先级

```
Time.timeScale 受三因素控制 (优先级从高到低):
  1. 场景加载中          → timeScale = 0       (最高优先级)
  2. 游戏暂停 (Paused)   → timeScale = 0
  3. 正常游戏             → timeScale = defaultScale (SIActionWorldSpeed 可调整)
```

## UI 时间分离

```csharp
// GameService.Awake() 中:
DOTween.defaultTimeScaleIndependent = true;
```

这保证所有 DOTween 动画使用 `unscaledDeltaTime`，不受 `Time.timeScale` 影响。暂停时 UI 动画（按钮悬浮、面板过渡）仍然流畅。

`SIActionWorldSpeed` — `{ float TargetScale }`，范围 `[0.2, 1.0]`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 慢放/快进时音效 Pitch 联动 | 待做 | AudioManager 需要知道当前 timeScale 来调整音高 |
| 时间倍率预设 (子弹时间 0.2x / 快进 2x) | 待做 | 当前 SIActionWorldSpeed 只支持 0.2~1.0 |
