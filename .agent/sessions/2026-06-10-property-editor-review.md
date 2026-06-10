# 2026-06-10 Property Tree Editor Review

## 背景

L3_Properties 编辑器在 feature/ability-pipeline 分支上已完成基础功能开发。本轮 Review 阶段聚焦三项工作：代码去重、代码分割、代码审查。

## 完成内容

### 1. 死代码清理 (P0)

**PropertyTreeEditorWindow.cs**:
- 删除 `ReplaceDef()` — 设计为拖拽替换 Def，实际流程是 Add+Delete 两步，从未使用
- 删除 `DefSummary()` — 静态 formatting 函数，无调用者
- 删除 `RenameLeaf()` — leaf 重命名未能从 UI 触发
- 删除 `ColorSave`, `ColorDrop`, `ColorLocal`, `SubLabelStyle` — 随重构变为死字段

### 2. PropertyTreeSO 性能优化 (P0)

- **O(n²) → O(n)** 子节点遍历: `BuildChildrenIndex()` 预建 `ParentId → List<child>` 索引
- `MergeAllNodes()` 提取: `ResolveStructure` 与 `ResolveAllNodes` 共享合并逻辑
- 新增 `ResolveAllNodes(out ancestorConflicts)` 重载，返回被祖先遮盖的 NodeId 集合

### 3. 代码分割 (P1)

- **PropertyTreeEditorPopups.cs** (新建): `NewTreeDialog` + `CreateDefDialog` 从主窗口独立
- `PropertyTreeEditorWindow.cs` 1796 → 1573 行 (-12%)
- `EditorUIUtility.DrawHeaderCard()`: Header 卡片模式提取到共享工具

### 4. GUIStyle 静态缓存 (P2)

- 7 个 `static` lazy-init 属性替换每帧 `new GUIStyle()` 分配
- 修复 `static readonly` → `static` lazy property (Unity `EditorStyles` 初始化时序问题)

### 5. NodeId 冲突系统 (P2-P3)

- **冲突检测**: `MergeAllNodes` 记录被祖先遮盖的本地 NodeId
- **IsLocal 精确判断**: 用 `!ancestorConflicts.Contains()` 替代 `_ownNodes.FindIndex`（后者对文件夹无效）
- **SortTreeNodes 修复**: 用 `IsLocal` 属性分组排序，不再依赖有歧义的 `_ownNodes.FindIndex`
- **冲突预防**: `AddFolder`, `TryRenameFolder`, `AddDefToFolder` 三个入口均检查继承节点名，自动后缀或弹窗拒绝
- **警告去重**: `_warnedConflicts` HashSet 每 session 每冲突只报一次

### 6. 数据修复

- `test_import.json` 移除 Firearm/Pistol/Rifle/Shotgun/Bow 中与祖先重复的 `Combat` 文件夹声明
- 导出 JSON 校验: 50 defs + 19 trees，继承链/DefId/orphan 全通过

### 7. UI 增强

- 左侧 Tree 列表新增绿色 `+` 按钮，快速创建子 Tree（父 Tree 预填 InheritsFrom）
- `NewTreeDialog.Show()` 支持可选 `parent` 参数

## 技术决策

- 惰性 GUIStyle 初始化: Unity Editor 中 `EditorStyles` 在 static ctor 阶段未就绪，使用 lazy property (`??=`) 替代 `static readonly`
- 冲突检测作为"预防"而非"修复": 创建/重命名时前置拦截，而非事后警告
- `ancestorConflicts` 通过 `MergeAllNodes` 的 out 参数传递，避免重复全量合并

## 文件清单

| 文件 | 变更类型 |
|------|---------|
| `PropertyTreeEditorWindow.cs` | 重构 (-294 / +123) |
| `PropertyTreeEditorPopups.cs` | 新建 |
| `PropertyTreeListView.cs` | 修改 |
| `PropertyTreeSO.cs` | 重构 (+103 / -xxx) |
| `ResolvedPropertyBag.cs` | 优化 |
| `EditorUIUtility.cs` | 扩展 |
| `test_import.json` | 修冲突 |
