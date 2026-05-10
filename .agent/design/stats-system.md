# 通用数值系统设计

> 日期: 2026-05-10
> 状态: Phase 2 已实现
> 定位: 项目级共享基础设施

## 核心: StatsTree SO 树

一个子系统一棵 `StatsTree`。节点通过 `StatsNodeSO` 递归嵌套。派生树通过 `InheritsFrom` 继承 + `IsEnabled`/`OverrideValue` 覆盖。

## 三类 SO

| 类 | 作用 |
|----|------|
| `StatsTreeSO` | 树根，`InheritsFrom` + `Children` + `Resolve()` |
| `StatsNodeSO` | 节点，`IsFolder`/`IsEnabled`/`Def`/`OverrideValue`/`CustomBehaviors` |
| `StatDefSO` | 叶子定义（不变量：Id, Type, Min, Max, Default, Behaviors） |

## 继承与覆盖

```
CharacterTree:   Vitals → HP(100), Hunger(100) ...
HumanTree:       InheritsFrom=Character, 不做修改
ZombieTree:      InheritsFrom=Character
  覆盖 Vitals.IsEnabled=false, HP.OverrideValue=30
```

`Resolve()`: CollectFrom(父树) → MergeNodes(本级覆盖) → ExtractLeaves(只取启用的叶子)

输出 `ResolvedStat{Def, OverrideDefault, EffectiveBehaviors}`——不修改原始 SO。

## 运行时

```
CharacterActor.Awake():
  stats = new CharacterStats(statsTree)   ← tree.Resolve() → StatFactory.Create → Container
  stats.Dump()                             ← 打印去重后的有效属性列表

CharacterActor.Update():
  stats.TickAll(dt)
```

## 目录

```
Assets/Scripts/Stats/
├── StatType.cs                [enum]
├── StatDefSO.cs               [SO] { Id, Type, Min, Max, Default, Behaviors[] }
├── StatsTreeSO.cs             [SO] { InheritsFrom, Children[], Resolve() }
├── StatsNodeSO.cs             [SO] { Id, IsFolder, IsEnabled, Def, Children[], OverrideValue }
├── StatInstance.cs            [class] { Def, Current, Modify, Tick }
├── StatFactory.cs             static Create(ResolvedStat)
├── ResolvedStat.cs            [struct] { Def, OverrideDefault, EffectiveBehaviors }
├── StatModifier.cs            [struct] 未来
├── Behaviors/
│   └── ConsumeOverTime, RestoreOverTime, DepleteTarget, ThresholdLevel
└── Editor/
    └── StatsTreeWindow.cs     EditorWindow: 可视化编辑树

Assets/Scripts/Character/Stats/
└── CharacterStats.cs          容器: Dictionary + TickAll
```
