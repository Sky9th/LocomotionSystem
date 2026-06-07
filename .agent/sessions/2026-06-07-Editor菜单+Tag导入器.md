# 2026-06-07 Editor菜单+Tag导入器+AbilityEditor修复

## 改动

1. **Editor 菜单统一** — 6 个窗口从 `Tools/`、`Window/` 统一到 `RedDust/` 根
2. **GameplayTagImporter** — JSON → .asset 批量导入，依赖排序+反射设置 parent
3. **tags_closed_loop.json** — 35 个闭环测试 Tag 的导入定义文件
4. **AbilityEditor 编译修复** — 删 categoryTag 时遗留的 `var v` 引用
5. **回滚误导入** — Ability/Noise/Stat Tag 目录清理

## 关联文件

| 文件 | 状态 |
|------|------|
| `GameplayTagImporter.cs` | 新建 |
| `tags_closed_loop.json` | 新建 |
| `TagEditorWindow.cs` | 改 |
| `TagPicker.cs` | 改 |
| `AbilityEditorWindow.cs` | 改 |
| `AbilityEditorMiddlePanel.cs` | 改 |
| `StatsTreeEditorWindow.cs` | 改 |
| `SyntyPrototypeMenu.cs` | 改 |
| 10+ .meta files | 删 (Noise/Stat 目录回滚) |
