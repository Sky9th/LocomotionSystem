# Character Stats Rule 系统

> 日期: 2026-05-11
> 状态: Phase 2.5 设计中
> 关联: `design/stats-system.md`

## 定位

CharacterActor 中所有 stat 业务逻辑的组织方式。解决 40+ stat 时代码散落、难查改的问题。

## 核心思想

每种**行为模式**一个 Rule 基类，具体逻辑继承实现。CharacterActor 不膨胀，只跑 `foreach (var r in rules) r.Apply(stats, ctx, dt)`。

## 行为模式基类

| 基类 | 模式 | 关键方法 |
|------|------|----------|
| `ToggleModifierRule` | 持续状态 — 条件满足挂修改器、不满足撤 | `ShouldActivate(ctx)`, `StatPath()` |
| `DepleteChainRule` | 归零链 — A 归零时对 B 持续伤害 | `SourcePath()`, `TargetPath()`, `DamagePerSec()` |
| `BatchDamageRule` | 一次性事件 — 外部攒批每帧统一执行 | `TargetPath()`, `Add(amount)` |
| `PassiveGainRule` | 被动增加 — 外部累积无帧逻辑 | `TargetPath()`, `Gain(amount)` |
| `CharacterStatRule` | 抽象基类，定制逻辑 | `Apply(stats, ctx, dt)` |

## CharacterActor 集成

```csharp
// Awake
rules.Add(new SprintStaminaRule(this));
rules.Add(new HungerDepleteRule());
rules.Add(damageRule = new DamageRule());

// UpdateStats
foreach (var r in rules) r.Apply(stats, ctx, dt);
stats.TickAll(dt);
```

## 跨系统通信

外部系统不调 Character 方法，走 EventDispatcher 发布事件。CharacterActor 在事件回调中调对应 Rule 的入口方法。

```
CombatSystem → Publish(DamageEvent) → CharacterActor → damageRule.Add(amount)
BuffSystem   → Publish(BuffEvent)   → CharacterActor → poisonRule.SetPoison(true)
```

## Rule 文件位置

```
Assets/Scripts/Character/Stats/Rules/
├── CharacterStatRule.cs
├── ToggleModifierRule.cs
├── DepleteChainRule.cs
├── BatchDamageRule.cs
├── PassiveGainRule.cs
├── SprintStaminaRule.cs
├── HungerDepleteRule.cs
└── DamageRule.cs
```

## 设计权衡

- **优**: CharacterActor 不随 stat 线性膨胀，Rule 独立可查，模式复用
- **劣**: 相比收敛方法多一层抽象，小量时稍过度
- **选择时机**: Phase 2.5 只有 3 个 Rule，但框架先搭好。后续新 stat 照模式填空
