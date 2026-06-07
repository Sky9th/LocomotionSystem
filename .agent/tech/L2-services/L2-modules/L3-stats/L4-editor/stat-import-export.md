# StatImportExport · Stat 导入导出

> `RedDust/Stat Import-Export`。单文件，static 工具类 + EditorWindow，JSON ↔ .asset 批量转换。

## 文件

| 文件 | 职责 |
|------|------|
| `StatImportExport.cs` | StatImporter (static 工具类) + StatImportWindow (EditorWindow) |

## 数据结构

### DTO（纯 POCO，用于 JSON 序列化）

```csharp
[Serializable] class StatDefEntry        // id, category, min, max, defaultValue, isConsumable, consumeRate, consumeInterval, isRestorable, restoreRate, restoreInterval, isCumulative
[Serializable] class StatsTreeEntry      // name, directory, inheritsFrom, defRefs(List<string>), nodes(List<JsonStatNode>)
[Serializable] class StatExportFile      // version, description, definitions, trees
```

### JSON Schema

```json
{
  "version": "1.0",
  "description": "...",
  "definitions": [{ "id": "HP", "category": "Vital", "min": 0.0, ... }],
  "trees": [{ "name": "Human", "directory": "Archetypes/Human", "inheritsFrom": null, "defRefs": ["HP", ...], "nodes": [...] }]
}
```

- `category`: 从 `Definitions/{category}/{id}.asset` 父目录提取
- `defRefs`: 使用 Stat Id 字符串而非 GUID 索引
- `nodes`: 直接复用 `JsonStatNode`（已是 `[Serializable]`）
- `inheritsFrom`: Tree name 字符串，空 = 根树

## 调用链

```
RedDust/Stat Import-Export → StatImportWindow.ShowWindow()
  ├── 导出: StatImporter.ExportToFile(jsonPath)
  │     └── ExportToJson()
  │           ├── FindAssets("t:StatDefinitionSO") → StatDefEntry[]
  │           └── FindAssets("t:StatsTreeSO") → StatsTreeEntry[]
  │
  └── 导入: StatImporter.ImportFromFile(jsonPath)
        └── ImportFromJson(jsonText)  [五阶段]
              ├── Phase 1: 创建 StatDefinitionSO .asset（路径冲突 + 全局查重 + 批次内重复）
              ├── Phase 2: 解析 defRefs Id 字符串 → StatDefinitionSO 引用（含 Phase 1 回填）
              ├── Phase 3: 创建 StatsTreeSO .asset（不含 InheritsFrom 链接）
              ├── Phase 4: 链接 InheritsFrom（含循环检测 WouldCreateCycle）
              └── Phase 5: AssetDatabase.SaveAssets() + Refresh()
```

## 设计决策

| 决策 | 原因 |
|------|------|
| DTO 独立于 ScriptableObject | SO 直接序列化含 m_Name/m_Enabled 等继承字段 |
| defRefs 用 Id 字符串 | JSON 可读可编辑，跨项目移植时 GUID 无效 |
| nodes 复用 JsonStatNode | 避免重复定义 DTO，字段结构与 treeJson 完全一致 |
| 导入为非破坏性 | 已存在资产跳过（幂等），避免覆盖手工修改 |
| 五阶段导入 | 解耦创建与链接，支持依赖排序 |
| Phase 2 额外加载 idToAssetPath | FindAssets 可能在 SaveAssets 前看不到 Phase 1 新创建的定义 |
| 树 name 不可重复（批次内） | 避免 treeNameToAssetPath key collision 损坏 InheritsFrom 链接 |

## 边界条件

| 场景 | 处理 |
|------|------|
| 同路径同 Id | Skip |
| 同路径不同 Id | Error（路径冲突）|
| 同 Id 不同路径 | Skip + warning |
| 批次内重复 Id | Skip + error（batchCreatedIds HashSet）|
| 批次内重复 Tree name | Skip + error（treeNamesInBatch HashSet）|
| defRefs 引用不存在的 Id | null 占位（保留索引）+ error |
| 循环继承 | WouldCreateCycle 检测拒绝 + error |
| inheritsFrom 指不存在的树 | 不链接 + error |
| treeJson 损坏 | LogWarning + 空 nodes 列表（不终止导出）|
