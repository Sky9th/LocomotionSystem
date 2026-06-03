# StatsNodeSO
> **源文件**: `Assets/Scripts/Stats/StatsNodeSO.cs`

ScriptableObject，Stat 树的层级节点，定义父子关系和叶子属性引用。

## 调用链

```
被谁调:
  StatsTreeSO.MergeNodes()        ← 递归遍历 Children 构建扁平列表
  StatsTreeSO.ExtractLeaves()     ← 读取 Def 和 OverrideValue
  StatsTreeWindow (Editor)        ← 可视化编辑时读写所有属性

调谁: (无 — 纯数据容器)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | StatsTreeSO | 树通过 Children 数组持有节点引用 |
| 依赖 | StatDefSO | Def 引用指向 Stat 定义（非叶子节点可空） |
| 依赖 | StatsNodeSO[] | Children 子节点数组（自引用） |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Id` | `string` | 节点唯一标识，沿树拼接为路径（如 "Vitals/HP"） |
| `IsEnabled` | `bool` | 是否启用，false 的节点在 Resolve 时被跳过 |
| `IsFolder` | `bool` | 是否为目录节点（true=无 Def 的容器，false=有 Def 的叶子） |
| `Def` | `StatDefSO` | 叶子节点引用的 Stat 定义（IsFolder=true 时可空） |
| `Children` | `StatsNodeSO[]` | 子节点数组 |
| `OverrideValue` | `float` | 覆盖默认值（>=0 时覆盖 Def.Default），-1 = 不覆盖，带 `[Min(-1f)]` 约束 |
| `Path` | `string` | 运行时设置的完整路径（`[NonSerialized]`，不序列化） |

## 方法

无方法。

## 内部机制

派生自 `ScriptableObject`，通过 `CreateAssetMenu` 创建：
- 菜单路径: `Game/Stats/Stats Node`
- 文件名默认值: `StatsNode`

Path 属性在运行时由 `StatsTreeSO.MergeNodes()` 在 Resolve 过程中设置，不持久化。

## 使用规则

- `IsFolder = true` 时 `Def` 可为 null，节点仅作容器
- `IsFolder = false` 时为叶子节点，必须设置 `Def`
- `OverrideValue` 默认 -1 表示不覆盖；>= 0 时在构造函数中覆盖 Def.Default
- 禁用节点 (`IsEnabled = false`) 在 Resolve 时跳过，不产生 StatInstance

## 未来规划

无具体规划。
