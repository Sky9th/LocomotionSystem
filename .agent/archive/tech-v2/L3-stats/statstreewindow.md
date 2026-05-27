# StatsTreeWindow · Stat 树编辑器

> `Stats/Editor/StatsTreeWindow.cs` — EditorWindow，可视化编辑 Stat 树的嵌套结构

## 调用链

```
被谁调:
  Unity Editor Menu              ← "Window/Stats Tree Editor" 菜单项
  OnGUI() 每帧绘制               ← Unity Editor 循环

调谁:
  StatsTreeSO                    ← 读写 Children/InheritsFrom
  StatsNodeSO                    ← 读写 Id/IsEnabled/IsFolder/Def/OverrideValue/Children
  AssetDatabase                  ← SaveAssets / AddObjectToAsset / DestroyImmediate
  EditorUtility                  ← SetDirty / DisplayDialog
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | StatsTreeSO | 编辑树对象的所有属性 |
| 依赖 | StatsNodeSO | 创建/删除/修改节点属性 |
| 依赖 | UnityEditor | 依赖 Editor API（仅 Editor 下可用） |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| (无公开属性) | — | 所有字段 private |

## 方法

### Open()
```csharp
[MenuItem("Window/Stats Tree Editor")]
private static void Open()
```
- **用途**: 打开编辑器窗口
- **调用者**: Unity Editor 菜单
- **备注**: 窗口标题 "Stats Tree"

### OnGUI()
```csharp
private void OnGUI()
```
- **用途**: 绘制编辑器 GUI
- **调用者**: Unity Editor 循环
- **备注**: 显示树选择器、继承关系、合并后树结构、新增/删除按钮

### MergeTrees()
```csharp
private List<MergedEntry> MergeTrees(StatsTreeSO inheritedTree, StatsTreeSO localTree)
```
- **用途**: 合并继承树和本树为统一的展示结构
- **参数**: `inheritedTree` — 继承的基树；`localTree` — 本树
- **返回**: MergedEntry 列表，每个条目标记是 override 还是 localOnly
- **调用者**: `OnGUI()`

### DrawMergedEntry()
```csharp
private void DrawMergedEntry(MergedEntry entry, int depth)
```
- **用途**: 递归绘制一个合并条目（toggle + Id + Def + value + 操作按钮）
- **参数**: `entry` — 条目；`depth` — 缩进层级
- **调用者**: `OnGUI()`（递归自身处理子条目）

### CreateNode()
```csharp
private static StatsNodeSO CreateNode(string id, bool isFolder, string parentPrefix)
```
- **用途**: 创建新的 StatsNodeSO 实例
- **参数**: `id` — 节点 Id；`isFolder` — 是否为目录；`parentPrefix` — 父路径前缀
- **返回**: 新创建的节点实例
- **调用者**: 各添加按钮回调

### AddChildToTree()
```csharp
private void AddChildToTree(StatsNodeSO child)
```
- **用途**: 将子节点添加到树的根级 Children
- **参数**: `child` — 新节点
- **调用者**: 添加按钮回调
- **备注**: 调用 AssetDatabase.AddObjectToAsset 关联到树

### AddChildToNode()
```csharp
private void AddChildToNode(StatsNodeSO parent, StatsNodeSO child)
```
- **用途**: 将子节点添加到指定父节点的 Children
- **参数**: `parent` — 父节点；`child` — 新节点
- **调用者**: `AddFolderButtons()`、`AddChildToInheritedFolder()`

### RemoveNode()
```csharp
private void RemoveNode(StatsNodeSO target)
```
- **用途**: 递归删除节点及其所有子节点
- **参数**: `target` — 要删除的节点
- **调用者**: 删除按钮回调（递归自身）
- **备注**: 确认弹窗后执行，不可撤销

### FindOrCreateLocalFolder()
```csharp
private StatsNodeSO FindOrCreateLocalFolder(string folderId)
```
- **用途**: 查找或创建与继承树同名的本地文件夹节点
- **参数**: `folderId` — 文件夹 Id
- **返回**: 找到或新建的节点
- **调用者**: 在继承树文件夹添加子节点时

### CanDeleteNode()
```csharp
private bool CanDeleteNode(StatsNodeSO target)
```
- **用途**: 检查节点是否可以被删除（没有子树覆盖它）
- **参数**: `target` — 待检查节点
- **返回**: 是否可以删除
- **调用者**: 删除按钮前校验

### ApplyPendingOverrides()
```csharp
private void ApplyPendingOverrides()
```
- **用途**: 将暂存的继承树覆盖值写入为本地节点
- **调用者**: Save 按钮
- **备注**: 对每个有挂起修改的继承节点进行 Instantiate + 写入本地树

### FindInheritingTrees()
```csharp
private List<StatsTreeSO> FindInheritingTrees()
```
- **用途**: 查找所有继承自当前树的其他树
- **返回**: 继承树列表
- **调用者**: `CanDeleteNode()`
- **备注**: 使用 AssetDatabase.FindAssets 查询所有 StatsTreeSO

### TreeInheritsFrom()
```csharp
private static bool TreeInheritsFrom(StatsTreeSO candidate, StatsTreeSO ancestor)
```
- **用途**: 检查 candidate 是否继承自 ancestor（沿 InheritsFrom 链）
- **参数**: `candidate` — 候选树；`ancestor` — 祖先树
- **返回**: 是否继承
- **调用者**: `FindInheritingTrees()`

### TreeOverridesNode()
```csharp
private static bool TreeOverridesNode(StatsTreeSO childTree, StatsNodeSO target)
```
- **用途**: 检查子树是否覆盖了指定节点
- **参数**: `childTree` — 子树；`target` — 目标节点
- **返回**: 是否覆盖
- **调用者**: `CanDeleteNode()`

## 使用规则

- 仅 Editor 下可用，运行时不可用
- 修改后需点击 Save 按钮（或等待自动保存标记）
- 删除节点前会自动检查是否有子树覆盖，防止破坏继承链
- 覆盖继承树的节点值需通过 ApplyPendingOverrides 创建本地副本

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 实时 Stat 调试面板 | 远期 | 调试需求 |
