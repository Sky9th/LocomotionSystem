# StatsTreeSO · Stat 树

> `Stats/StatsTreeSO.cs` — ScriptableObject，Stat 树的根入口，序列化嵌套结构，Resolve() 产出运行时 StatInstance 列表

## 调用链

```
被谁调:
  CharacterStats 构造函数        ← new CharacterStats(tree)，传入 StatsTreeSO
  CharacterStats 内部            ← tree.Resolve() 产出 List<StatInstance>
  StatsTreeWindow (Editor)      ← 可视化编辑时读取 Children/InheritsFrom

调谁:
  Resolve() → CollectNodes() → ExtractLeaves()
    ├── CollectFrom()           ← 递归处理继承树 InheritsFrom
    └── MergeNodes()            ← 递归合并子节点，去重（后覆盖前）
         └── new StatInstance()  ← 有效叶子节点 → 运行时实例
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 02-character CharacterStats | 构造时传入，Resolve 产出所有 StatInstance |
| 依赖 | StatsNodeSO | Children 和 InheritsFrom 链中的节点引用 |
| 依赖 | StatInstance | Resolve 时构造实例 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `InheritsFrom` | `StatsTreeSO` | 继承的基树，先收集基树节点再覆盖为本树节点 |
| `Children` | `StatsNodeSO[]` | 根级子节点数组 |

## 方法

### Resolve()
```csharp
public IReadOnlyList<StatInstance> Resolve()
```
- **用途**: 解析整棵树，产出运行时 StatInstance 列表
- **返回**: 所有有效叶子节点的 StatInstance 只读列表
- **调用者**: `CharacterStats` 构造函数
- **备注**: 每次 Resolve() 重新构建，不缓存

### CollectNodes()
```csharp
private List<StatsNodeSO> CollectNodes()
```
- **用途**: 收集所有节点到扁平列表，支持继承树合并
- **返回**: 合并后的 StatsNodeSO 列表
- **调用者**: `Resolve()`
- **备注**: 先递归 InheritsFrom，然后用本树 Children 覆盖

### CollectFrom()
```csharp
private static void CollectFrom(StatsTreeSO tree, List<StatsNodeSO> list)
```
- **用途**: 递归收集继承树的节点
- **参数**: `tree` — 要收集的树；`list` — 输出的扁平列表
- **调用者**: `CollectNodes()`（递归自身）
- **备注**: 先处理 tree.InheritsFrom，再处理 tree.Children

### MergeNodes()
```csharp
private static void MergeNodes(StatsNodeSO[] nodes, List<StatsNodeSO> list, string parentPath = "")
```
- **用途**: 将一组节点合并入扁平列表，按 Path 去重
- **参数**: `nodes` — 待合并的节点数组；`list` — 目标列表；`parentPath` — 父路径前缀
- **调用者**: `CollectFrom()`、`CollectNodes()`（递归自身处理文件夹的子节点）
- **备注**: `node.Path = $"{parentPath}/{node.Id}"`；已有相同 Path 的节点被覆盖（后覆盖前）

### ExtractLeaves()
```csharp
private static IReadOnlyList<StatInstance> ExtractLeaves(List<StatsNodeSO> nodes)
```
- **用途**: 从扁平节点列表中提取有效叶子节点，构造 StatInstance
- **参数**: `nodes` — 合并后的节点列表
- **返回**: StatInstance 列表
- **调用者**: `Resolve()`
- **备注**: 跳过 `!node.IsEnabled || node.IsFolder || node.Def == null` 的节点

## 使用规则

- 通过 `CreateAssetMenu` 创建：`Game/Stats/Stats Tree`
- 支持多层继承：TreeA → TreeB → TreeC，后覆盖前
- 相同 Path 的节点自动去重，子节点覆盖继承树的同名节点
- 不缓存 Resolve 结果 — 每次调用重新构建

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Resolve 结果缓存 + 脏标记 | 远期 | 性能优化需求 |
