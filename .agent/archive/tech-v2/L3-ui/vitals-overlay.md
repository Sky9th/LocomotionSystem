# VitalsOverlay · 生命体征 HUD

> `Assets/Scripts/UI/HUD/VitalsOverlay.cs` — 继承 UIOverlay。每帧拉取 PlayerService 的角色数值，实时刷新 HP/Hunger/Thirst/Stamina 进度条。

## 调用链

```
UIService.HandleGameState(Playing)
  └── ShowOverlay(VitalsOverlay)
      └── PlayEnterSequence()

每帧 Update()
  └── refreshTimer += DeltaTime
  └── if (timer >= refreshRate)
      └── uiService.TryGetPlayerStats(out stats)
          └── PlayerService.TryGetPlayerStats()
              └── CharacterStats 读取
      └── TryUpdateBar(hpBar, "Vitals/HP", stats)
      └── TryUpdateBar(hungerBar, "Vitals/Hunger", stats)
      └── TryUpdateBar(thirstBar, "Vitals/Thirst", stats)
      └── TryUpdateBar(staminaBar, "Vitals/Stamina", stats)
          └── bar.SetValue(current, max)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIOverlay | 基类 |
| 依赖 | UIService | TryGetPlayerStats |
| 依赖 | UIStatBar | 4 条 StatBar 组件 |
| 依赖 | 01-core (PlayerService) | 间接，通过 UIService 代理 |

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
- **调用者**: Unity Engine (每帧)
- **备注**: 使用 DeltaTime (unscaled)，暂停时继续刷新

### TryUpdateBar()
```csharp
private void TryUpdateBar(UIStatBar bar, string path, Dictionary<string, (float current, float max)> stats)
```
- **用途**: 按 stat path 从字典取值更新 StatBar
- **参数**: `bar` — StatBar 组件；`path` — 数值路径如 "Vitals/HP"；`stats` — 数值字典
- **调用者**: Update()

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `refreshRate` | float | 0.1f | 刷新间隔 (秒) |
| `hpStatPath` | string | "Vitals/HP" | HP 数值路径 |
| `hungerStatPath` | string | "Vitals/Hunger" | 饥饿数值路径 |
| `thirstStatPath` | string | "Vitals/Thirst" | 口渴数值路径 |
| `staminaStatPath` | string | "Vitals/Stamina" | 体力数值路径 |

## 未来规划

无。
