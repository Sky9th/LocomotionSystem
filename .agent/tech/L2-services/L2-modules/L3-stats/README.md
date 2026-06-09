# L3-stats · Stat 数值框架

> 树状嵌套的数值系统。StatDefSO 定义属性，StatsNodeSO 组织层级，StatsTreeSO 产出 StatInstance，StatInstance 持有运行时值并通过 Modifier 支持外部影响。

## 层级定位

**L3-stats** 位于 L2-services 下的 L3 层，是 Character 系统的数值基础设施。它不是一个独立的 Service，而是被 Character 模块内部持有的工具层模块。其产出（StatInstance）由 CharacterStats 统一管理，不直接暴露给 GameContext 或其他 Service。

层级关系：
- L1-app → GameContext
- L2-services → CharacterService（含 CharacterStats）
- L3-stats → 数值框架（本模块）

## 调用链

```
外部模块
  02-character CharacterStats(tree)    ← 持有 StatsTreeSO，管理所有 StatInstance
    │
    ├── tree.Resolve()
    │     ├── CollectNodes()           ← 递归收集所有节点，支持继承
    │     └── ExtractLeaves()          ← 有效叶子节点 → StatInstance[]
    │
    ├── CharacterStats.Get(path)       ← StatInstance 查询
    │
    └── CharacterStats.Update(ctx, dt)
          ├── rules.Apply()            ← 各 Rule 添加/移除 Modifier
          └── kv.Value.Tick(dt)        ← 各 StatInstance 自 Tick

StatInstance 内部
  Tick(dt)
    ├── TickConsume()                  ← Def.IsConsumable 时执行
    │     └── CollectModifiers()       ← 遍历 modifiers 收集 Addend/Multiplier
    │           └── Modify(delta)       ← 应用变化 + 事件通知
    └── TickRestore()                  ← Def.IsRestorable 时执行
          └── CollectModifiers()
                └── Modify(delta)

外部写入
  StatInstance.AddModifier(mod)         ← ToggleModifierRule 等
  StatInstance.RemoveByOwner(owner)     ← 效果结束时回收
  StatInstance.Modify(delta)            ← 外部直接加减 (如 DamageRule)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| StatDefSO、StatsTreeSO | 02-character CharacterStats | 构造时传入 StatsTreeSO，Resolve() 产出 StatInstance |
| StatInstance | 02-character Rules | 各 Rule 通过 AddModifier/RemoveByOwner/Modify 操作 |
| StatsTreeSO (Equipment 族) | L3-equipment GearDefSO | GearDefSO 引用 StatsTreeSO 获取 stat 集合，通过 overrides 覆写值 |
| StatsTreeWindow | UnityEditor | 依赖 Editor API，运行时不可用 |
| 整个模块 | — | 纯 SO + runtime class，无 GameContext 反向依赖 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 能力勾选代替接口实现 | StatDefSO 的 bool 字段直接控制 Tick 分派，减少类型派生 |
| Modifier 并行槽位（Addend/Multiplier） | 多系统互不知晓时消解顺序冲突 |
| Modifier 带 Owner 引用 | 创建者负责回收，避免悬挂修改器 |
| 树路径做 Key | 沿 Id 拼接路径，查询和引用统一用字符串路径 |
| 帧累加计时 | 无异步/CancellationToken 清理负担 |
| 接口保留为契约文档 | IStatConsumable 等不强制实现，仅作为设计参考 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 长间隔改事件驱动 | 远期 | TimeService | 代码 TODO (StatInstance.cs) |
| IStatDerived 派生 Stat 实现 | 待做 | 派生公式系统 | interface 注释 |
| Demo 阶段确定具体数值和生效条件 | 待做 | — | 代码 TODO (CharacterStats.cs) |
| 基于 Editor 的实时 Stat 调试面板 | 远期 | StatsTreeWindow 扩展 | 调试需求 |
| 多人同步 StatInstance | 远期 | 网络系统 | 设计文档 stats-system.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [definition/stat-def-so.md](definition/stat-def-so.md) | Stat 定义 — Id/Min/Max/Default + 能力勾选 |
| [tree/stats-node-so.md](tree/stats-node-so.md) | 树节点 (旧) — 父子关系 + Def 引用 + OverrideValue |
| [tree/stats-tree-so.md](tree/stats-tree-so.md) | Stat 树 (旧) — InheritsFrom + Resolve() → StatInstance[] |
| [tree/stats-tree-data.md](tree/stats-tree-data.md) | StatsTreeData (新) — JSON 树数据 + 继承合并算法 |
| [tree/actor-tree-design.md](tree/actor-tree-design.md) | Actor Tree 层级设计 — Human/Zombie/Creature/Robot 继承链 + 56 stat 分配 |
| [tree/equipment-tree-design.md](tree/equipment-tree-design.md) | 装备 Tree 层级设计 — Weapon/Armor/Tool + Building/Environment + 全量 109 stat 汇总 |
| [instance/stat-instance.md](instance/stat-instance.md) | 运行时实例 — Current + modifiers + Tick 分派 |
| [modifier/stat-modifier.md](modifier/stat-modifier.md) | 修改器 — Owner + Apply 委托 |
| [modifier/modifier-context.md](modifier/modifier-context.md) | 修改器上下文 — Addend + Multiplier 累加容器 |
| [interfaces/i-stat-consumable.md](interfaces/i-stat-consumable.md) | 可消耗接口契约 |
| [interfaces/i-stat-cumulative.md](interfaces/i-stat-cumulative.md) | 可累积接口契约 |
| [interfaces/i-stat-derived.md](interfaces/i-stat-derived.md) | 派生接口契约 |
| [interfaces/i-stat-restorable.md](interfaces/i-stat-restorable.md) | 可恢复接口契约 |
| [L4-editor/stats-tree-window.md](L4-editor/stats-tree-window.md) | 树编辑器 (旧) — EditorWindow 子资产管理 |
| [L4-editor/stats-tree-editor-window.md](L4-editor/stats-tree-editor-window.md) | 树编辑器 (新) — JSON 驱动 + 继承合并视图 |
| [L4-editor/stat-import-export.md](L4-editor/stat-import-export.md) | Stat 导入导出 — JSON ↔ .asset 批量转换 |

