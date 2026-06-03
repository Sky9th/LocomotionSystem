# StatsTreeEditorWindow · 树编辑器 (JSON)

> `Window/Stats Tree Editor (JSON)`。partial class 拆分为 4 个文件。

## 文件结构

| 文件 | 职责 |
|------|------|
| `StatsTreeEditorWindow.cs` | 窗口框架、OnGUI、Header/Toolbar/Body、LoadTree/Save、ComputeDepth、FindDuplicateIds、Types |
| `StatsTreeEditorWindow.Tree.cs` | 树合并 (MergeTrees/MergeNodesForDisplay/FindNodeByPath) |
| `StatsTreeEditorWindow.Actions.cs` | 增删节点、覆盖提交 (ApplyPendingOverrides)、DefRefs 管理 |
| `StatsTreeEditorWindow.Draw.cs` | DrawEntry 及所有绘制方法、pending helpers |

## 数据结构

窗口侧维护：
- `workingNodes: List<JsonStatNode>` — 当前树的节点（LoadTree 加载，Save 持久化）
- `mergedRoots: List<MergedEntry>` — 与继承链合并后的展示树
- `pendingEnabled: Dictionary<string, bool>` — 暂存的 IsEnabled 修改（key=Path）
- `pendingOverride: Dictionary<string, float>` — 暂存的 OverrideValue 修改（key=Path）
- `foldouts: Dictionary<string, bool>` — 文件夹展开状态（key=Path）

MergedEntry：
```csharp
class MergedEntry {
    JsonStatNode node;
    bool isOverride;    // 本地有同 Id 覆盖了祖先
    bool isLocalOnly;   // 本地独有，祖先无此节点
    List<MergedEntry> children;
    string path;
}
```

## 合并算法 (MergeTrees)

```
1. 收集继承链: 从 InheritsFrom 自顶向下组成 chain (Base→Human→Man)
2. 每层 DeserializeNodes → MergeNodesForDisplay(targetList, depth)
3. 自己的 workingNodes → MergeNodesForDisplay(targetList, myDepth)

MergeNodesForDisplay:
  对每个 sourceNode:
    设 Path, Depth=depth, 解析 DefRef
    在 targetList 按 Path 查找:
      有匹配 → 覆盖（targetList[existing] = node）
      无匹配:
        IsOverride=false → 追加
        IsOverride=true  → 跳过（孤儿覆盖）
    文件夹递归子节点
```

## 树形结构可视化（核心）

编辑器最关键的一点：**让用户一眼看出层级关系**。每个节点是独立卡片，子节点通过缩进嵌套在父节点下方。

### 层级渲染规则

```
DrawEntry(node, depth):

  1. 绘制自己的 helpBox 卡片:
     BeginVertical(helpBox)
     Space(pad)                          ← 顶部留白
     Row 1 (BeginHorizontal)
     Space(2) [仅叶节点]                 ← Row1↔Row2 间隙
     Row 2 (BeginHorizontal) [仅叶节点]
     Space(pad)                          ← 底部留白
     EndVertical

  2. 若 node.IsFolder && 展开 && 有子节点:
     Space(2)                            ← 父卡片→首个子
     for each child:
       BeginHorizontal
       Space(childIndent × childDepth)   ← 层级缩进（depth=1→20px, depth=2→40px）
       BeginVertical
       Space(2) if i>0                   ← 兄弟间隙
       DrawEntry(child, depth+1)         ← 递归
       EndVertical
       EndHorizontal
```

### 缩进规则

| 层级 | indent | 效果 |
|------|--------|------|
| 根节点（depth=0） | 0 | 贴左 |
| 一级子节点（depth=1） | childIndent × 1 | 向右偏移 20px |
| 二级子节点（depth=2） | childIndent × 2 | 向右偏移 40px |

**关键：子节点的 Visual 起点对齐父节点文字区**。即子节点 toggle 位置 = 父节点 contentX 位置附近，形成视觉连接。

### 整体布局

```
┌─ Header (helpBox) ──────────────────────────────┐
│ Tree: [▾]                            [Save/Saved]│
│ Inherits From: [▾]                               │
└──────────────────────────────────────────────────┘

    [＋ Folder]  [＋ Leaf]              📁 TreeName

┌─ helpBox ── 根节点 ───────────────────────────┐
│ [6] [✓] [6] [▸] [4] 📁 Attributes     [＋ Add][✕]│
└────────────────────────────────────────────────┘
    ┊  ← 父节点卡片下方，向右缩进 20px
    ┊
    ┌─ helpBox ── 子节点 ──────────────────────┐
    │ [6] [✓] [6] [  ] [4] Strength   Val [100][✕]│
    │ [40]       [StatDefSO ▾]                  │  ← Def 行
    └───────────────────────────────────────────┘
    ┊
    ┌─ helpBox ── 继承子节点 ──────────────────┐
    │ [6] [✓] [6] [  ] [4] Agility (inherited)  │  ← 灰色
    │ [40]       HP                             │  ← 灰色 Def 标签
    └───────────────────────────────────────────┘
```

### 层级嵌套示意（三层）

```
📁 Attributes            ← 根文件夹，depth=0
  📁 Core                ← 一级子文件夹，depth=1，缩进 20px
    🍃 Strength          ← 二级叶节点，depth=2，缩进 40px
    🍃 Agility           ← 二级叶节点，depth=2，缩进 40px
  🍃 Mana                ← 一级叶节点，depth=1，缩进 20px
```

每个节点是独立卡片，层级靠**缩进 + 折叠三角**传达，无需连线。

## 空间常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `pad` | 6 | 统一内边距 — 卡片内 **上/下** 留白 + Row 1 **左/右** 留白 |
| `toggleW` | 14 | IsEnabled 开关宽度 |
| `foldoutW` | 14 | 折叠三角宽度 |
| `textPad` | 4 | **LabelField 专用补偿** — LabelField 无内部 padding，加此偏移后文字与 TextField 对齐 |
| `contentX` | pad + toggleW + pad + foldoutW = 40 | TextField / ObjectField 控件左缘起始 x |
| `childIndent` | 20 | 子节点每层缩进量 |

> **textPad 不是固定布局间距。** TextField 和 ObjectField 自带 ~4px 内部 padding（文字在 44px），所以它们从 contentX=40 起始即可。LabelField 无此 padding，需额外 `Space(textPad)` 把文字推到同样位置。在 DrawNodeLabel / DrawDefField 内按分支添加。

## 卡片间距规范

`pad` 定义为"统一内边距（左/右/上/下）"——不止 Row 1 的左右，卡片内部上下也要。

### 卡片内部

```
┌─ BeginVertical(helpBox) ─────────────────────────┐
│                                                   │
│   Space(pad)             ← 顶部留白（卡片上边距）  │
│   Row 1: [pad][Toggle][pad][Foldout][Id][Flex][Val][Actions][pad] │
│   Space(2)               ← Row1↔Row2 间隙（仅叶）  │
│   Row 2: [contentX][Def][pad]    （仅叶节点）      │
│   Space(pad)             ← 底部留白（卡片下边距）  │
│                                                   │
└───────────────────────────────────────────────────┘
```

### 卡片之间

```
父卡片 EndVertical
│
├─ Space(2)         ← 父卡片 → 第一个子节点（紧凑，表示归属关系）
│
├─ 子节点循环 ──────────────────────────────────────┐
│   BeginHorizontal                                  │
│     Space(childIndent × childDepth)  ← 层级缩进    │
│     BeginVertical                                  │
│       Space(2) if i>0                ← 兄弟间隙    │
│       DrawEntry(child, depth+1)      ← 递归卡片    │
│     EndVertical                                    │
│   EndHorizontal                                    │
└───────────────────────────────────────────────────┘
│
（循环结束，返回 DrawMergedTree）

根节点之间:
  Space(4)           ← 根↔根间隙（DrawMergedTree 中 i>0）
```

### 间距速查

| 位置 | 值 | 语义 |
|------|-----|------|
| 卡片上留白 | `Space(pad=6)` | 内容与卡片上缘间距 |
| 卡片下留白 | `Space(pad=6)` | 内容与卡片下缘间距 |
| Row1 → Row2 | `Space(2)` | 叶节点两行间，紧凑 |
| 父卡片 → 首个子 | `Space(2)` | 紧凑，表示子归属于父 |
| 兄弟子节点间 | `Space(2)` | 同层级紧凑排列 |
| 根节点间 | `Space(4)` | 跨组，稍大以区分 |

## DrawEntry 参数

| 参数 | 含义 | 用途 |
|------|------|------|
| `depth` | 树层级嵌套深度 | 控制缩进，根=0，每级 +1 |
| `myDepth` | 当前树在继承链的深度 | 判断覆盖归属，Base=0, Human=1 |

**注意区分**：`depth`（DrawEntry 参数）= 视觉缩进层数；`node.Depth`（JsonStatNode 运行时字段）= 继承层深度。两者不同。

## 每行绘制详解

### Row 1: `[pad] [Toggle 14] [pad] [Foldout 14] [textPad] [Id] [FlexibleSpace] [Val] [Actions] [pad]`

| 元素 | 样式 | 说明 |
|------|------|------|
| Toggle | `EditorGUILayout.Toggle` | IsEnabled，继承节点的修改进 pendingEnabled |
| Foldout | `EditorStyles.foldout` | 文件夹三角，默认展开 |
| 可编辑文件夹名 | `EditorGUILayout.TextField` | 仅本地独有文件夹可编，继承/覆盖用 Label |
| 文件夹 Label | `EditorStyles.boldLabel`(覆盖) / `EditorStyles.label`(继承) | 前缀 `📁 `，继承追加 ` (inherited)` 灰色 |
| 叶节点 Label | `EditorStyles.boldLabel`(覆盖) / `EditorStyles.label`(本地) / 灰色(继承) | 文字左加 `textPad` 补偿 |
| FlexibleSpace | — | 把 Val + 按钮推到右侧 |
| Val 标签 | `EditorStyles.boldLabel`(覆盖) / `EditorStyles.miniLabel`(默认) | "Val"，覆盖时加粗 |
| Val 输入框 | `EditorStyles.textField`，右对齐，覆盖时加粗 | FloatField，未覆盖显示 Def.Default，改回默认自动清为 -1 |
| ＋ Add Child | `EditorStyles.miniButton` 自适应宽度 | 仅文件夹显示，点击调用 AddChildToFolder |
| ✕ 删除 | `EditorStyles.miniButton`，红色背景 | 仅本地节点显示 |
| ✕ Clear Val | `EditorStyles.miniButton`，仅 `isOwnNode && isOverridden` 可点 | 恢复到 Def.Default |

### Row 2（仅叶节点）: `[pad + toggleW + pad + foldoutW] [Def 字段] [pad]`

| 元素 | 样式 | 说明 |
|------|------|------|
| 本地节点 Def | `EditorGUILayout.ObjectField` | 拖入 StatDefinitionSO，赋值后 Id 自动改为 Def.Id |
| 继承节点 Def | 灰色 `EditorStyles.miniLabel` | 只读 Def 名，文字左加 `textPad` |

### 节点类型视觉区分

| 类型 | 样式 | 何时出现 |
|------|------|---------|
| 自有文件夹 | 📁 Id 可编辑 (TextField) | 本地新建的文件夹 |
| 自有叶节点 | Id 可编辑 (TextField) | 本地新建的叶节点，分配 Def 后 Id 自动改为 Def.Id |
| 覆盖文件夹 | 📁 Id 加粗 (BoldLabel) | 本地覆盖了祖先的文件夹 |
| 覆盖叶节点 | Id 加粗 (BoldLabel) | 本地覆盖了祖先的叶节点 |
| 继承文件夹 | 📁 Id 灰色 + "(inherited)" | 来自祖先链的文件夹 |
| 继承叶节点 | Id 灰色 + "(inherited)" | 来自祖先链的叶节点 |

### 子节点区域

- 仅文件夹 + 展开状态显示
- 整体右缩进 `childIndent`（20px）
- 子节点间间隙 2px（紧凑）
- 每个子节点包裹独立 helpBox

## 操作流程

### 新增节点
```
AddRoot / AddChildToFolder
  → MakeNode (IsOverride=false)
  → 叶节点 Id=NewLeaf_N（占位），分配 Def 后改为 Def.Id
  → 文件夹 Id=NewFolder_N（可手动改名）
  → workingNodes.Add
  → 文件夹下添加: parent.Children 追加 child.Id
  → RebuildMergedView → Repaint
```

### 删除节点
```
DeleteNodeById(id)
  → 所有节点的 Children 移除该 Id
  → workingNodes 移除该节点
  → RebuildMergedView
```

### 保存
```
Save →
  1. FindDuplicateIds() → 有重复则弹窗阻止
  2. ApplyPendingOverrides()
     → 遍历 pendingEnabled / pendingOverride
     → FindOrCreateLocalByPath(path) 创建本地覆盖节点
       - IsOverride=true
       - Def 索引从继承树 defRefs 翻译到本地 defRefs
       - 不建父文件夹链（靠 Path 匹配）
  3. JsonUtility.ToJson → treeData.TreeJson
  4. SetDirty + SaveAssets
  5. Clear pending 字典
  6. RebuildMergedView
```

### 覆盖值判断
```
myDepth = ComputeDepth()  // 沿 InheritsFrom 链计数
isOwnNode = node.Depth >= myDepth
defVal = node.DefRef.Default
isOverridden = isOwnNode && OverrideValue >= 0 && OverrideValue ≠ defVal

→ isOverridden: Val 粗体 + Clear 按钮启用
→ !isOwnNode:  继承值，只读，不粗体，不可 Clear
```

## 视觉规则

| 规则 | 实现 |
|------|------|
| 每个节点独立卡片 | `EditorGUILayout.BeginVertical(EditorStyles.helpBox)` |
| 卡片内上留白 | `Space(pad=6)` 在 Row 1 之前 |
| 卡片内下留白 | `Space(pad=6)` 在 Row 2 之后（或 Row 1 之后，若文件夹） |
| 叶节点行间 | `Space(2)` 在 Row1 与 Row2 之间 |
| 父卡片→首个子 | `Space(2)` 在卡片 EndVertical 后、children 循环前 |
| 根节点间间距 | `EditorGUILayout.Space(4)` |
| 子节点间距 | `EditorGUILayout.Space(2)`（紧凑） |
| 继承节点灰色 | `GUI.contentColor = Color.gray` |
| 覆盖节点加粗 | `EditorStyles.boldLabel` |
| 保存按钮有改动 | 绿色背景，文字 "Save" |
| 保存按钮无改动 | 灰色背景，文字 "Saved"，disabled |
| 删除按钮红色 | `GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f)` |
| TextField/ObjectField | 从 `contentX`（40px）起始，自带内部 padding，文字 ≈44px |
| LabelField / BoldLabel | 从 `contentX + textPad`（44px）起始，无内部 padding |
| 重复 Id 检测 | OnGUI 顶部 Error HelpBox + Save 弹窗阻止 |

## 工具栏

```
EditorGUILayout.Space(4)
[＋ Folder] (Height 24)   [＋ Leaf] (Height 24)   FlexibleSpace   📁 TreeName (Bold)
EditorGUILayout.Space(4)
```

## Header

```
EditorGUILayout.BeginVertical(EditorStyles.helpBox)
  Row 1: Tree ObjectField + Save Button
  Row 2: InheritsFrom ObjectField
EditorGUILayout.EndVertical()
```

## 重复 Id 检测

```
FindDuplicateIds() → 扫描 workingNodes 中 Id 出现次数 > 1
→ OnGUI 顶部红色 Error HelpBox 列出重复 Id
→ Save 时检测到重复则 EditorUtility.DisplayDialog 阻止，不保存
```
