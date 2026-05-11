# 通用数值系统设计

> 日期: 2026-05-11
> 状态: Phase 2 重构完成
> 定位: 项目级共享基础设施

## 核心设计

### 能力勾选

Stat 的能力在 Inspector 上直接勾选，不挂在 class 级接口：

`isConsumable` / `isRestorable` / `isCumulative` — 勾上即生效，Tick() 检查 bool。
接口文件（`IStatConsumable` 等）保留作为文档契约，StatDefSO 不实现它们。外部判断用 `Def.IsConsumable`。

### 修改器 — 并行槽位

不同系统独立添加修改器，不串行调用。每个修改器写独立槽位：

```
ModifierContext { Addend, Multiplier }
```

合并公式：`(baseRate + Addend) * Multiplier`

多系统互不知晓，槽位类型消解顺序冲突。护甲（Addend += 5）和天气（Multiplier *= 1.5）同时生效，结果确定。

### 修改器生命周期

每个修改器带 `Owner`，创建者负责回收：

```csharp
stat.AddModifier(new StatModifier { Owner = this, Apply = (s, ctx) => ctx.Multiplier = 0 });
// ... 效果结束
stat.RemoveByOwner(this);
```

### 树路径做 Key

沿 SO 树节点 `Id` 拼接路径（`"Vitals/HP"`、`"Attributes/Strength"`），Resolve 时生成。字典、对外查询、DepleteTarget 引用都用路径。

### 决策权

- Stat 根据自身能力自行 Tick，Character 只调 `TickAll(dt)`
- 外部系统不直接调 Modify，而是通过 AddModifier/RemoveModifier 注入影响
- 角色自身状态（冲刺等）同样走修改器

### 计时策略

统一帧累加，无异步/CancellationToken 清理负担：

| 间隔 | 策略 | 公式 |
|------|------|------|
| `0`（每帧）| dt 缩放 | `rate × dt` |
| `>0`（定时）| 帧累加 + catch-up | `rate × ticks` |

长间隔后续接入 TimeManager 再改为事件驱动。`StatInstance.Tick()` 无外部生命周期依赖。

## 历史

- 去掉 `ResolvedStat`、`StatFactory` — 树直接产出 `StatInstance[]`，无中间 struct
- 去掉 `Behaviors/` 目录 — 能力数据回归 `StatDefSO` 的 bool 勾选 + 字段
- 去掉 `StatType` enum — 能力组合 > 互斥分类
- 去掉 `ConditionId/Condition` — 业务逻辑收敛在 Character

## 运行时

```
CharacterActor.Awake():
  stats = new CharacterStats(statsTree)   ← tree.Resolve() 直接产出 StatInstance[]

CharacterActor.Update():
  stats.TickAll(dt)                        ← 各 stat 按能力勾选自行分派
```

## 三类实体

| 实体 | 作用 |
|------|------|
| `StatDefSO` | 定义 + 能力勾选，Inspector 编辑 |
| `StatsTreeSO` / `StatsNodeSO` | 层级组织 + 继承覆盖，Resolve 时生成路径并直接 new StatInstance |
| `StatInstance` | 运行时实例，Current + modifiers[] + Tick 分派 |

## 目录

```
Assets/Scripts/Stats/
├── Interfaces/               (契约文档，SO 不实现)
│   ├── IStatConsumable.cs
│   ├── IStatRestorable.cs
│   ├── IStatCumulative.cs
│   └── IStatDerived.cs
├── StatDefSO.cs               [SO] Id/Min/Max/Default + isConsumable/isRestorable/isCumulative
├── StatsTreeSO.cs             [SO] InheritsFrom + Resolve() → StatInstance[]
├── StatsNodeSO.cs             [SO] Id/IsFolder/Def/OverrideValue + Path
├── StatInstance.cs            Current + Path + modifiers[] + Tick
├── StatModifier.cs            {Owner, Apply(stat, ctx)}
├── ModifierContext.cs         {Addend, Multiplier}
└── Editor/
    └── StatsTreeWindow.cs     EditorWindow

Assets/Scripts/Character/Stats/
├── CharacterStats.cs          容器: Dictionary<path, StatInstance> + TickAll + All
└── Rules/                     业务逻辑层（Rule 模式）
    ├── CharacterStatRule.cs   抽象基类
    ├── ToggleModifierRule.cs  持续状态 ± 修改器
    ├── DepleteChainRule.cs    归零链
    ├── BatchDamageRule.cs     一次性事件攒批
    ├── PassiveGainRule.cs     被动增加
    └── ...                    具体 Rule（SprintStamina, HungerDeplete 等）

详见 tech/modules/character/stats-rule-system.md
```
