# 2026-06-14 回滚删除按钮组件化改动

## 背景

commit `70092a73` (Editor UI 组件化) 引入了 `EditorButton` / `EditorUIUtility.DeleteButton()` 统一删除按钮渲染，替换了各编辑器中手动 `GUI.backgroundColor = ColorDelete` + `GUILayout.Button("x", EditorStyles.miniButton)` 的模式。

## 问题

新 `EditorUIUtility.DeleteButton()` 通过 `EditorButton.Draw("x", EditorButtonStyle.Danger, EditorButtonSize.Small, 22f)` 渲染，使用 `StyleSmall`（基于 `GUI.skin.button`），其灰色渐变背景纹理削弱了 `GUI.backgroundColor` 红色着色，导致删除按钮显示为灰底，丢失了原有的红底视觉。

## 处理

全量回滚删除按钮相关改动，恢复手动红底模式：

| 文件 | 操作 |
|---|---|
| `UIUtility.cs` | 整文件回滚 — `DeleteButton()` 恢复旧签名，搜索清除按钮恢复 `EditorStyles.miniButton` |
| `PropertyTreeListView.cs` | 整文件回滚 — 删除按钮恢复手动红底 |
| `PropertyTreeEditorWindow.cs` | 部分回滚 — 3 处删除按钮（文件夹/属性/def池）+ `ColorDelete` 字段恢复 |
| `AbilityEditorWindow.cs` | 部分回滚 — 移除树节点 `onDeleteLeaf` 回调 + `DeleteAbility` 方法 |
| `AbilityEditorMiddlePanel.cs` | 部分回滚 — `✕` 清空按钮恢复 `EditorButtonStyle.Danger` |

**保留不改**（非删除按钮的组件化改动）：
- `PropertyImportExport.cs` — `EditorImportExport` 组件化重写
- `PropertyTreeEditorPopups.cs` — Create/Cancel/Close 按钮改用 `EditorButton`
- 所有 `*ImportExport.cs` — 导入导出窗口重构
- `PropertyTreeEditorWindow.cs` — 搜索行/Add Folder/详情按钮组件化
- `EffectEditorWindow.cs` / `NoiseEditorWindow.cs` — Tag 按钮改用 `EditorButton`

## 结论

`EditorButton` + `EditorButtonStyle.Danger` 的删除按钮方案需要重新设计渲染方式，确保红底正确显示后，再逐编辑器迁移。
