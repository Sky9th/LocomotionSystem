# StatDefSO · Stat 定义

> `Stats/StatDefSO.cs` — ScriptableObject，定义单个 Stat 的类型、数值范围、能力勾选

## 调用链

```
被谁调:
  StatsTreeSO.ExtractLeaves()   → 读取 Def 属性创建 StatInstance
  StatInstance.Tick()           → 读取 IsConsumable/IsRestorable 判断 Tick 分支
  StatInstance.TickConsume()    → 读取 consumeRate / consumeInterval
  StatInstance.TickRestore()    → 读取 restoreRate / restoreInterval
  CharacterStats 外部查询       → 通过 StatInstance.Def 访问

调谁: (无 — 纯数据容器)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | StatsNodeSO | 树节点通过 Def 引用指向 StatDefSO |
| 被依赖 | StatInstance | 运行时持有 Def 引用，读取数值范围和能力标记 |
| 被依赖 | 02-character CharacterStats | 通过 StatInstance.Def 获取 Stat 元信息 |
| 依赖 | — | 无 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Id` | `string` | Stat 唯一标识，用于路径拼接和查询 |
| `Min` | `float` | 最小值 / 下限，Modify 时 clamp 到此值 |
| `Max` | `float` | 最大值 / 上限，Modify 时 clamp 到此值 |
| `Default` | `float` | 默认值，StatInstance 构造时如果没有 OverrideValue 则使用此值 |
| `isConsumable` | `bool` | 可消耗能力勾选（饥饿/口渴/体力等） |
| `consumeRate` | `float` | 每秒消耗量（isConsumable 时有效） |
| `consumeInterval` | `float` | 消耗间隔秒数（0 = 每帧） |
| `isRestorable` | `bool` | 可恢复能力勾选（体力/生命等） |
| `restoreRate` | `float` | 每秒恢复量（isRestorable 时有效） |
| `restoreInterval` | `float` | 恢复间隔秒数（0 = 每帧） |
| `isCumulative` | `bool` | 可累积能力勾选（经验/熟练度等，仅标记，无 Tick 行为） |
| `IsConsumable` | `bool` | 计算属性 — `isConsumable && consumeRate > 0` |
| `IsRestorable` | `bool` | 计算属性 — `isRestorable && restoreRate > 0` |

## 方法

无方法。

## 使用规则

- 通过 `CreateAssetMenu` 创建：`Game/Stats/Stat Definition`
- 能力通过 Inspector 勾选 + 填写速率字段，不通过接口实现
- 外部判断能力用 `Def.IsConsumable` / `Def.IsRestorable`，不直接读字段

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 增加图标/显示名称等 UI 字段 | 远期 | UI 系统需求 |
| 增加派生公式字段 | 待做 | IStatDerived 实现时 |
