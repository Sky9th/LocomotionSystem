# TagImportExport

> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

- **菜单**: `RedDust/Tag Import-Export` (priority 26)
- **源文件**: `Assets/Scripts/L1_Core/RdTag/Editor/TagImportExport.cs`
- **命名空间**: `RedDust.Core`
- **相关**: [TagEditorWindow](TagEditorWindow.md)

## 概述

RdTag JSON ↔ .asset 批量导入导出。

- **导入类**: `RdTagImporter` (static) — 核心逻辑，从 JSON 批量创建 `RdTagDefSO` 资产
- **窗口类**: `RdTagImportWindow` (EditorWindow) — 使用共享 `EditorImportExport` 组件渲染 UI
- **资产根目录**: `Assets/Data/Tags`

## JSON 格式

```json
{
  "version": "1.0",
  "description": "...",
  "tags": [
    {
      "name": "Species",
      "parent": null,
      "fullTag": "Species",
      "description": ""
    },
    {
      "name": "Human",
      "parent": "Species",
      "fullTag": "Species.Human",
      "description": ""
    }
  ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `version` | string | 格式版本，当前 "1.0" |
| `description` | string | 导入文件描述（可选） |
| `tags[].name` | string | 标签叶名称（必需），对应资产文件名 `Tag_{name}.asset` |
| `tags[].parent` | string? | 父标签 FullTag（null 表示根标签） |
| `tags[].fullTag` | string | 完整层级路径 |
| `tags[].description` | string | 标签说明（可选，通过反射写入） |

## 导入流程

`RdTagImporter.ImportFromJson(string jsonText)` 返回 `(int created, int updated, int skipped, List<string> errors)`.

**两阶段导入:**

1. **第一轮 — 创建资产文件**: 遍历 `tags[]`，若 `.asset` 不存在则 `CreateInstance<RdTagDefSO>` → `AssetDatabase.CreateAsset` → 反射写入 `description` → `AutoDeriveLeafName` + `RefreshCache`。已存在的资产计入 `skipped`。

2. **第二轮 — 设置父引用**: 按 `parent` 深度排序（dot 数量），多轮迭代直到全部解析或停滞。
   - 根标签（`parent == null`）直接缓存 `FullTag → assetPath`
   - 子标签在缓存中查找父标签 → 反射设置 `parent` 字段 → `RefreshCache`
   - Fallback: 全局 `FindTagByFullTag` 搜索已存在的父标签
   - 未解析的标签报 error

**增量导入**: 已存在的 Tag 跳过不覆盖，支持多次导入追加新标签。

## 导出流程

`RdTagImporter.ExportToJson()` → `string`.

- 扫描 `AssetDatabase` 中所有 `t:RdTagDefSO`
- 遍历 `Parent` 引用链计算 `fullTag`（不走 `cachedFullTag`，避免加载顺序问题）
- 按 `parent` 深度排序（根→叶）

**文件 I/O**:
- `ImportFromFile(string jsonPath)` — 读取文件后委托 `ImportFromJson`
- `ExportToFile(string jsonPath)` — `ExportToJson` → `File.WriteAllText`

## UI 窗口

`RdTagImportWindow` 使用共享 `EditorImportExport.Draw()` 渲染：

```
Header Card:  "Tag Import-Export" + "L1_Core · RdTag · JSON ↔ .asset"
File Card:    文件路径 + "…" 浏览按钮
Preview Card: JSON 摘要 (版本/描述/New N/Exist N)
Buttons:      Import (Success) + Export (Primary)
Result Card:  Created / Skipped / Errors 统计
```

`BuildPreview` 回调解析 `TagImportFile` JSON，统计 new vs exist 数量。

## 数据类型

| 类型 | 定义位置 | 说明 |
|------|---------|------|
| `RdTagImporter.TagEntry` | TagImportExport.cs (嵌套类) | 单个 Tag 条目 DTO |
| `RdTagImporter.TagImportFile` | TagImportExport.cs (嵌套类) | 导入文件根对象 (version + tags[]) |
| `RdTagDefSO` | `Assets/Scripts/L1_Core/RdTag/RdTagDefSO.cs` | 运行时/编辑器 Tag 定义资产 |

## 模组支持

设计上支持玩家/模组作者编写 JSON → 导入 Unity → 自动生成 `.asset` 文件。导入无需手动创建目录，`Directory.CreateDirectory` 自动处理。

## 已知限制

- `parent` 和 `description` 字段通过反射写入私有字段，依赖字段名不变
- `cachedFullTag` 非序列化，导出时走 Parent 链计算，性能可接受（Tag 总量有限）
- 缺少 `updated` 计数（方法签名有但始终为 0），增量导入不覆盖已有属性
