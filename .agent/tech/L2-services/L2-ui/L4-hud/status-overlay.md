# StatusOverlay
> **源文件**: `Assets/Scripts/Services/L2_UI/HUD/StatusOverlay.cs`

继承 UIOverlay。显示 Buff/Debuff 图标列表（当前为骨架实现，等待 Stats 系统就绪）。

## 调用链

```
UIService.HandleGameState(Playing) → ShowOverlay(StatusOverlay)
  └── PlayEnterSequence()
  └── OnInitialize() → (空，等待 Stats 系统)

每帧 Update()
  └── refreshTimer += DeltaTime
  └── if (timer >= refreshRate)
      └── RefreshStatuses() → (空，等待 Stats 系统)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIOverlay | 基类 |
| 依赖 | UIService | — |
| 待依赖 | Stats 系统 | 读取角色状态效果 |

## 公开属性

无公开属性。所有字段通过 `[SerializeField]` 在 Inspector 中配置。

## 方法

### OnInitialize()
```csharp
protected override void OnInitialize()
```
- **用途**: 预留，等待角色状态快照就绪
- **调用者**: UIOverlay.Initialize()
- **备注**: TODO — 订阅角色条件/Buff 快照

### Update()
```csharp
private void Update()
```
- **用途**: 定时刷新，当前为空
- **调用者**: Unity Engine

### RefreshStatuses()
```csharp
private void RefreshStatuses()
```
- **用途**: 预留 — 读取 GameContext 快照中的状态效果数据
- **调用者**: Update()
- **备注**: TODO — 按活跃状态效果实例化 statusEntryPrefab

## 内部机制

- **MonoBehaviour**: 继承 UIOverlay，受 Unity 生命周期管理
- **定时轮询**: 使用 refreshTimer + refreshRate 限频，避免每帧查询
- **骨架实现**: 当前仅预留框架，具体功能等待 Stats 系统就绪后填充

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `statusContainer` | RectTransform | — | 状态条目容器 |
| `statusEntryPrefab` | GameObject | — | 状态条目 Prefab |
| `refreshRate` | float | 1f | 刷新间隔 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Buff/Debuff 图标显示 | 待做 | Stats 系统 | 代码 TODO |
| 持续时间环形倒计时 | 待做 | Stats 系统 | 代码 TODO |
| 状态效果 Tooltip | 待做 | Stats 系统 | 代码 TODO |
