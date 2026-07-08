# EditorImportExport — JSON Import/Export 共享面板

> **源文件**: `Assets/Scripts/Shared/Editor/Components/ImportExport.cs` | **命名空间**: `RedDust.Shared.EditorUI`

> **Last Verified**: 2026-07-08

## 概述

`EditorImportExport` 是一个静态工具类，提供所有 Entity 模块共用的 Import/Export UI 面板。6 个模块的 ImportWindow（Equipment/Ammo/Consumable/Building/Character/SceneItem）全部调用 `EditorImportExport.Draw()` 渲染界面。

## 调用链

```
被谁调:
  XxxImportWindow.OnGUI()  → EditorImportExport.Draw(...)
    (Equipment/Ammo/Consumable/Character/Building/SceneItem — 共 6 处)

调谁:
  EditorLabel.Draw()        → 标题 + 面包屑
  EditorCard.Draw()         → 文件选择 / 预览 / 结果 卡片
  EditorButton.Draw()       → Import / Export 按钮
  buildPreview(path)        → Func<string,string> 委托 — 各模块实现预览
  onImport(path)            → Func → EntityImporter.ImportFromFile
  onExport(path)            → Action → File.WriteAllText
  EditorUtility.OpenFilePanel / SaveFilePanel → 系统文件对话框
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | `EditorTokens` (Shared/EditorUI) | 样式/间距/颜色令牌 |
| 依赖 | `EditorUIUtility` (Shared/EditorUI) | 占位文本样式 |
| 被调用 | 6 个 `XxxImportWindow` | 每个模块一个，通过 onImport/onExport 委托注入 |

## 方法

### Draw()
```csharp
public static void Draw(
    string title, string subtitle,
    string defaultDir, string fileExtension, string defaultFileName,
    ref string filePath, ref string previewText,
    ref (int created, int updated, int skipped, List<string> errors) result,
    Func<string, string> buildPreview,
    Func<string, (int,int,int,List<string>)> onImport,
    Action<string> onExport)
```
- **用途**: 渲染完整的 Import/Export 面板
- **布局**:
  1. **Header**: `title` + `subtitle` (面包屑样式)
  2. **JSON File Card**: 文件路径输入框 + `…` 按钮 (OpenFilePanel)
  3. **Preview Card**: 调用 `buildPreview(filePath)` 生成 HTML 预览 — 仅当文件存在时显示
  4. **Buttons**: Import (绿色, 仅文件存在时可用) / Export (蓝色, SaveFilePanel)
  5. **Result Card**: created/updated/skipped 计数 + 错误列表 (仅当有一次操作后才显示)

## 使用示例

```csharp
// 在 XxxImportWindow.OnGUI() 中:
EditorImportExport.Draw(
    "Equipment Import-Export",           // title
    "L3_Equipment · JSON ↔ .asset",      // subtitle
    Config.DataRoot,                     // defaultDir
    "json",                              // fileExtension
    Config.DefaultFileName,              // defaultFileName
    ref _filePath,                       // mutable state — 文件路径
    ref _previewText,                    // mutable state — 预览文本缓存
    ref _result,                         // mutable state — 操作结果
    Config.BuildPreview,                 // preview 委托
    path => EntityImporter.ImportFromFile(path, Config),  // import 委托
    path => File.WriteAllText(path, EntityImporter.ExportToJson(Config)) // export 委托
);
```

## 状态追踪

| 状态字段 | 类型 | 用途 |
|---------|------|------|
| `filePath` | `ref string` | 用户选择的 JSON 文件路径 |
| `previewText` | `ref string` | 缓存预览 HTML，文件变化时重新生成 |
| `result` | `ref tuple` | Import 结果 (created/updated/skipped/errors)，Export 后不显示 |

## 交叉引用

- [EntityImporter.md](../L2_EntityService/EntityImporter.md) — 导入/导出引擎，`onImport`/`onExport` 委托的实现
- [EntityEditorWindow.md](../L2_EntityService/EntityEditorWindow.md) — EntityEditorWindow 中的 Import/Export 按钮
