# StatsTreeData · JSON 树数据

> ScriptableObject，替代 StatsTreeSO。每层独立 JSON + defRefs，继承靠读取时向上合并。

## 类型定义

### StatsTreeData : ScriptableObject

| 字段 | 类型 | 说明 |
|------|------|------|
| `InheritsFrom` | `StatsTreeData` | 继承链，null=根树 |
| `treeJson` | `string` | 整个树的 JSON（序列化 `TreeDataContainer`） |
| `defRefs` | `List<StatDefSO>` | Def 查找表，只追加不删除，索引稳定 |

### JsonStatNode

| 字段 | 类型 | 存储 | 说明 |
|------|------|------|------|
| `Id` | `string` | JSON | 唯一标识，文件夹可手动命名，叶节点取自 Def.Id |
| `IsEnabled` | `bool` | JSON | 运行时开关 |
| `IsFolder` | `bool` | JSON | true=文件夹，false=叶节点 |
| `IsOverride` | `bool` | JSON | true=覆盖祖先节点，false=自有节点 |
| `Children` | `string[]` | JSON | 子节点 Id 列表 |
| `Def` | `int` | JSON | `defRefs` 索引，-1=无 |
| `OverrideValue` | `float` | JSON | >=0 时覆盖 Def.Default |
| `Path` | `string` | 运行时 | 完整路径 "Attributes/Strength" |
| `DefRef` | `StatDefinitionSO` | 运行时 | Def 解析后的 SO 引用 |
| `Depth` | `int` | 运行时 | 继承层深度，Base=0, Human=1, Man=2 |

### TreeDataContainer

```csharp
[Serializable]
public class TreeDataContainer { public List<JsonStatNode> Nodes = new(); }
```

## 合并算法

```
Resolve() →
  CollectNodes():
    1. CollectFrom(InheritsFrom 链，自顶向下，depth 递增)
       每层: DeserializeTree → RefreshPaths → MergeNodes(targetList, depth)
    2. MergeNodes(自己的 Nodes, targetList, myDepth)

MergeNodes(sourceNodes, targetList, depth):
  for each node in sourceNodes:
    node.Path = parentPath + "/" + node.Id
    node.Depth = depth
    existing = targetList.FindByPath(node.Path)
    if existing >= 0 → 覆盖（子替代祖先）
    else:
      if node.IsOverride → 孤儿，跳过
      else → 追加
    if node.IsFolder && node.Children != null:
      递归子节点
```

## JSON 示例

### Base（3 节点，Agility 自覆盖=70）
```json
{
  "Nodes": [
    { "Id":"Attributes","IsFolder":true, "IsOverride":false,
      "Children":["Strength","Agility"], "Def":-1, "OverrideValue":-1 },
    { "Id":"Strength","IsFolder":false, "IsOverride":false,
      "Children":null, "Def":0, "OverrideValue":-1 },
    { "Id":"Agility","IsFolder":false, "IsOverride":false,
      "Children":null, "Def":1, "OverrideValue":70 }
  ]
}
```

### Human（2 节点，Strength 覆盖=150，Mana 自有）
```json
{
  "Nodes": [
    { "Id":"Strength","IsFolder":false, "IsOverride":true,
      "Children":null, "Def":0, "OverrideValue":150 },
    { "Id":"Mana","IsFolder":false, "IsOverride":false,
      "Children":null, "Def":2, "OverrideValue":-1 }
  ]
}
```

### 合并结果 (Human.Resolve() 内存)
```
Attributes  Path="Attributes"           Depth=0  OverrideValue=-1   (Base)
Strength    Path="Attributes/Strength"   Depth=1  OverrideValue=150  (Human覆盖)
Agility     Path="Attributes/Agility"    Depth=0  OverrideValue=70   (Base自覆盖)
Mana        Path="Mana"                  Depth=1  OverrideValue=-1   (Human自有)
```

## 关键设计

| 决策 | 原因 |
|------|------|
| 每层独立 JSON | 修改子不影响父，版本控制可 diff |
| Def 索引 + defRefs 查表 | JSON 不存 GUID，运行时 O(1) 还原 SO 引用 |
| Children 存 Id 非索引 | 删除不偏移，跨树不翻译，JSON 自描述 |
| IsOverride 标记 | 区分"覆盖"和"自有"，父删子随 |
| Depth 运行时计算 | 编辑器判断覆盖归属（自己的才粗体） |
| 覆盖不建父文件夹链 | 靠 Path 匹配祖先结构，子 JSON 最小化 |
