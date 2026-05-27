# 05-stats · Stat 数值框架

> 树状嵌套的数值系统。StatDefSO 定义属性，StatsNodeSO 组织层级，StatsTreeSO 产出 StatInstance，StatInstance 持有运行时值并通过 Modifier 支持外部影响。

## 调用链

```
CharacterStats(tree)                  ← 02-character/CharacterStats
  │
  ├── tree.Resolve()
  │     ├── CollectNodes()            ← 递归收集所有节点，支持继承
  │     └── ExtractLeaves()           ← 有效叶子节点 → StatInstance[]
  │
  ├── CharacterStats.Get(path)        ← StatInstance 查询
  │
  └── CharacterStats.Update(ctx, dt)
        ├── rules.Apply()             ← 各 Rule 添加/移除 Modifier
        └── kv.Value.Tick(dt)         ← 各 StatInstance 自 Tick

StatInstance.Tick(dt)
  ├── TickConsume()                   ← isConsumable 时执行
  │     └── CollectModifiers()        ← 遍历 modifiers 收集 Addend/Multiplier
  │           └── Modify(delta)       ← 应用变化 + 事件通知
  └── TickRestore()                   ← isRestorable 时执行
        └── CollectModifiers()
              └── Modify(delta)

外部调用:
  StatInstance.AddModifier(mod)        ← ToggleModifierRule 等
  StatInstance.RemoveByOwner(owner)    ← 效果结束时回收
  StatInstance.Modify(delta)           ← 外部直接加减 (如 DamageRule)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| StatDefSO、StatsTreeSO | 02-character CharacterStats | 构造时传入 StatsTreeSO，Resolve() 产出 StatInstance |
| StatInstance | 02-character Rules | 各 Rule 通过 AddModifier/RemoveByOwner/Modify 操作 |
| 本模块 | UnityEditor | StatsTreeWindow 依赖 Editor API |
| 本模块 | — | 纯 SO + runtime class，无 GameContext 依赖 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 能力勾选代替接口实现 | StatDefSO 的 bool 字段直接控制 Tick 分派，减少类型派生 |
| Modifier 并行槽位（Addend/Multiplier） | 多系统互不知晓时消解顺序冲突 |
| Modifier 带 Owner 引用 | 创建者负责回收，避免悬挂修改器 |
| 树路径做 Key | 沿 Id 拼接路径，查询和引用统一用字符串路径 |
| 帧累加计时 | 无异步/CancellationToken 清理负担 |
| 接口保留为契约文档 | IStatConsumable 等不强制实现，仅作为文档参考 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 长间隔改事件驱动 | 远期 | TimeService | 代码 TODO (StatInstance.cs L42) |
| IStatDerived 派生 Stat 实现 | 待做 | 派生公式系统 | interface 注释 |
| Demo 阶段确定具体数值和生效条件 | 待做 | — | 代码 TODO (CharacterStats.cs L23) |
| 基于 Editor 的实时 Stat 调试面板 | 远期 | StatsTreeWindow 扩展 | 调试需求 |
| 多人同步 StatInstance | 远期 | 网络系统 | 设计文档 stats-system.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [statdefso.md](statdefso.md) | Stat 定义 — Id/Min/Max/Default + 能力勾选 |
| [statsnodeso.md](statsnodeso.md) | 树节点 — 父子关系 + Def 引用 + OverrideValue |
| [statstreeso.md](statstreeso.md) | Stat 树 — InheritsFrom + Resolve() → StatInstance[] |
| [statinstance.md](statinstance.md) | 运行时实例 — Current + modifiers + Tick 分派 |
| [statmodifier.md](statmodifier.md) | 修改器 — Owner + Apply |
| [modifiercontext.md](modifiercontext.md) | 修改器上下文 — Addend + Multiplier |
| [istatconsumable.md](istatconsumable.md) | 可消耗接口契约 |
| [istatcumulative.md](istatcumulative.md) | 可累积接口契约 |
| [istatderived.md](istatderived.md) | 派生接口契约 |
| [istatrestorable.md](istatrestorable.md) | 可恢复接口契约 |
| [statstreewindow.md](statstreewindow.md) | 树编辑器 — EditorWindow 可视化编辑 |
