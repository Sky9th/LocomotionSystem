# VitalsOverlay
> **源文件**: `Assets/Scripts/Services/L2_UI/HUD/VitalsOverlay.cs`

继承 UIOverlay。每帧拉取 PlayerService 的角色数值，实时刷新 HP/Hunger/Thirst/Stamina 进度条。

## 调用链

```
UIService.HandleGameState(Playing)
  └── ShowOverlay(VitalsOverlay)
      └── PlayEnterSequence()

每帧 Update()
  └── refreshTimer += DeltaTime
  └── if (timer >= refreshRate)
      └── uiService.TryGetPlayerProps(out props)
          └── Entity.Query.Properties 读取
      └── TryUpdateBar(hpBar, "Vitals/HP", props)
      └── TryUpdateBar(hungerBar, "Vitals/Hunger", props)
      └── TryUpdateBar(thirstBar, "Vitals/Thirst", props)
      └── TryUpdateBar(staminaBar, "Vitals/Stamina", props)
          └── bar.SetValue(current, max)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIOverlay | 基类 |
| 依赖 | UIService | TryGetPlayerProps（通过 PlayerEntity.Query.Properties） |
| 依赖 | UIStatBar | 4 条 StatBar 组件 |
| 依赖 | PropertyTable | 间接，通过 PlayerEntity.Query.Properties 读取 |

## 公开属性

无公开属性。所有字段通过 `[SerializeField]` 在 Inspector 中配置。

## 方法

### OnInitialize()
```csharp
protected override void OnInitialize()
```
- **用途**: 设置各 StatBar 标签名 (HP/Hunger/Thirst/Stamina)
- **调用者**: UIOverlay.Initialize()

### Update()
```csharp
private void Update()
```
- **用途**: 按 refreshRate 定时刷新数值
- **调用者**: Unity Engine（每帧）
- **备注**: 使用 `DeltaTime` (unscaled)，暂停时继续刷新

### TryUpdateBar()
```csharp
private void TryUpdateBar(UIStatBar bar, string propertyPath, PropertyTable props)
```
- **用途**: 按 property path 从 PropertyTable 取值更新 StatBar
- **参数**: `bar` — StatBar 组件；`propertyPath` — 属性路径如 "Vitals/HP"；`props` — 玩家 PropertyTable
- **调用者**: Update()

## 内部机制

- **MonoBehaviour**: 继承 UIOverlay，UIOverlay 继承 MonoBehaviour
- **定时刷新**: 使用 refreshTimer 累积 + refreshRate 限频，避免每帧查询
- **uiService 空保护**: 未初始化时跳过刷新

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `refreshRate` | float | 0.1f | 刷新间隔（秒） |
| `hpStatPath` | string | "Vitals/HP" | HP 数值路径 |
| `hungerStatPath` | string | "Vitals/Hunger" | 饥饿数值路径 |
| `thirstStatPath` | string | "Vitals/Thirst" | 口渴数值路径 |
| `staminaStatPath` | string | "Vitals/Stamina" | 体力数值路径 |
| `hpBar` | UIStatBar | — | HP 进度条 |
| `hungerBar` | UIStatBar | — | 饥饿进度条 |
| `thirstBar` | UIStatBar | — | 口渴进度条 |
| `staminaBar` | UIStatBar | — | 体力进度条 |

## 未来规划

无。
