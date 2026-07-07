# ItemEditorWindow

- **菜单**: `RedDust/Item Editor`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Item/Editor/ItemEditorWindow.cs`

> **Last Verified**: 2026-07-08 | **Verification**: PASSED — ItemEditorWindow.cs exists, all referenced classes confirmed, signatures match

## UI 结构全图（当前实现）

### Window 总览

```
┌──────────────────────────────────────────────────────────────────────────┐
│  ItemEditorWindow                                                        │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ [1] HEADER CARD  EditorCard                                         │ │
│  │                                                                      │ │
│  │  ┌─ Row 1 ────────────────────────────────────────────────────────┐ │ │
│  │  │ [title-label] "Item Editor"    [breadcrumb] "L3_Item · Editor"  │ │ │
│  │  └─────────────────────────────────────────────────────────────────┘ │ │
│  │                                                                      │ │
│  │  ┌─ Row 2 ────────────────────────────────────────────────────────┐ │ │
│  │  │ [refresh-btn] [import-export-btn]  ←flex→  [+create-btn ▼]     │ │ │
│  │  │                                        [save-btn]  [delete-btn] │ │ │
│  │  └─────────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  ┌─ [2] THREE-COLUMN BODY ─────────────────────────────────────────────┐ │
│  │                                                                      │ │
│  │  ┌ [2a] LEFT ────────┐  ┌ [2b] CENTER ──────────┐  ┌ [2c] RIGHT ─┐ │ │
│  │  │ EditorCard 280px    │  │ helpBox expand         │  │ 200px        │ │ │
│  │  │                    │  │                        │  │              │ │ │
│  │  │ [search-bar]       │  │ ┌ [basic-card] ──────┐ │  │ ┌ [preview]─┐│ │ │
│  │  │ ──gap──            │  │ │ EditorCard "Basic"  │ │  │ │ EditorCard ││ │ │
│  │  │ [tree-view]        │  │ │ ┌─────────────────┐ │ │  │ │           ││ │ │
│  │  │  📁 TemplateA (3)  │  │ │ │ Template:  [SO] │ │ │  │ │ [preview  ││ │ │
│  │  │    📄 Item1        │  │ │ │ Prefab:    [GO] │ │ │  │ │  texture] ││ │ │
│  │  │    📄 Item2        │  │ │ └─────────────────┘ │ │  │ │           ││ │ │
│  │  │  📁 TemplateB (1)  │  │ └────────────────────┘ │  │ └───────────┘│ │ │
│  │  │    📄 Item3        │  │                        │  │              │ │ │
│  │  │                    │  │ ┌ [slots-card] ──────┐ │  │              │ │ │
│  │  │                    │  │ │ EditorCard "Slots"  │ │  │              │ │ │
│  │  │                    │  │ │ Header: SlotId|Tags │ │  │              │ │ │
│  │  │                    │  │ │ ──slot rows──        │ │  │              │ │ │
│  │  │                    │  │ │ [+ Add Slot]        │ │  │              │ │ │
│  │  │                    │  │ └────────────────────┘ │  │              │ │ │
│  │  │                    │  │                        │  │              │ │ │
│  │  │                    │  │ ┌ [Common-card] ─────┐ │  │              │ │ │
│  │  │                    │  │ │ EditorCard "Common"  │ │  │              │ │ │
│  │  │                    │  │ │ Weight   [3.5] Flt  │ │  │              │ │ │
│  │  │                    │  │ │ MaxStack [64 ] Int  │ │  │              │ │ │
│  │  │                    │  │ └────────────────────┘ │  │              │ │ │
│  │  │                    │  │ ┌ [Weapon-card] ─────┐ │  │              │ │ │
│  │  │                    │  │ │ EditorCard "Weapon"  │ │  │              │ │ │
│  │  │                    │  │ │ ATK      [Bld] ARL  │ │  │              │ │ │
│  │  │                    │  │ │ Speed    [1.0] Flt  │ │  │              │ │ │
│  │  │                    │  │ └────────────────────┘ │  │              │ │ │
│  │  │                    │  │ ┌ [Presentation-card] ┐ │  │              │ │ │
│  │  │                    │  │ │ EditorCard "Present" │ │  │              │ │ │
│  │  │                    │  │ │ Name     [Sword] Str│ │  │              │ │ │
│  │  │                    │  │ └────────────────────┘ │  │              │ │ │
│  │  └────────────────────┘  └────────────────────────┘  └──────────────┘ │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ [3] STATUS BAR CARD  EditorCard                                     │ │
│  │  "Type: MeleeWeaponSO · Template: WeaponBase · 2 slots · 8 props"   │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘

POPUPS:
  - CreateNewItem GenericMenu      (ItemDefSO / MeleeWeaponSO / RangedWeaponSO)
  - TagPicker                      (AcceptTags per slot, RdTag/RdTagList values)
  - DeleteConfirmation             (确认删除物品)
  - TemplateChangeWarning          (切换模板 → 覆写丢失警告)
```

### 部位命名表

#### [1] Header Card

| 部件名 | 实现控件 | 代码方法 |
|--------|---------|---------|
| `title-label` | `EditorLabel.Draw("Item Editor", HeaderTitleStyle)` | `DrawHeader` |
| `breadcrumb` | `EditorLabel.Draw("L3_Item · Editor", width, BreadcrumbStyle)` | `DrawHeader` |
| `refresh-btn` | `EditorButton.Default("Refresh", Medium)` | `DrawHeader` |
| `import-export-btn` | `EditorButton.Default("Import/Export", Medium)` (占位) | `DrawHeader` |
| `create-btn` | `EditorButton.Success("+ Create", Medium)` → GenericMenu | `CreateNewItem` |
| `save-btn` | `EditorButton.Primary("Save *"/"Save", Medium, enabled:)` | `Save` |
| `delete-btn` | `EditorButton.Danger("Delete", Medium, enabled:)` | `DeleteSelectedItem` |

#### [2a] Left Panel (EditorCard, 280px)

| 部件名 | 实现控件 | 代码方法 |
|--------|---------|---------|
| `search-bar` | `EditorSearchBar.Draw(_searchFilter)` | `DrawLeftPanel` |
| `tree-view` | `EditorTreeView` (Unity TreeView) | `DrawLeftPanel` → `_treeView.OnGUI(rect)` |
| `tree-node` | 按 Template.name 分组为文件夹 (EditorTreeNode) | `BuildItemTree` |

#### [2b] Center Panel (helpBox, expand)

| 部件名 | 实现控件 | 代码方法 |
|--------|---------|---------|
| `basic-card` | `EditorCard.Draw("Basic", ...)` | `DrawBasicSection` |
| `template-field` | `EditorInput.ObjectField<PropertyTreeSO>` | `DrawBasicSection` |
| `prefab-field` | `EditorInput.ObjectField<GameObject>` | `DrawBasicSection` |
| `slots-card` | `EditorCard.Draw("Slots", ...)` | `DrawSlotsSection` |
| `slot-row` | TextField + TagPicker + IntField + FloatField + Delete btn | `DrawSlotRow` |
| `add-slot-btn` | `EditorButton.Default("+ Add Slot", Small)` | `DrawSlotsSection` |
| `{folder}-card` | `EditorCard.Draw(folderName, ...)` 按一级节点分组 | `DrawPropertyOverrides` |
| `prop-row` | 按 PropertyDefSO.Type 分发控件 | `DrawPropertyRow` → 各 `DrawXxxRow` |
| `prop-override-color` | 覆写=白色, 默认=灰色 (GUI.color) | `DrawPropertyRow` |
| `prop-reset-btn` | `EditorButton.Delete()` 复位到默认值 | `DrawPropertyRow` |

#### [2c] Right Panel (200px)

| 部件名 | 实现控件 | 代码方法 |
|--------|---------|---------|
| `preview-card` | `EditorCard.Draw()` | `DrawPreviewPanel` |
| `preview-texture` | `AssetPreview.GetAssetPreview` → `GUI.DrawTexture` | `DrawPreviewPanel` |
| `preview-empty` | `EditorLabel.Draw("No Prefab assigned.")` | `DrawPreviewPanel` |

#### [3] Status Bar Card

| 部件名 | 实现控件 | 代码方法 |
|--------|---------|---------|
| `status-text` | `EditorLabel.Draw(summary, DimLabelStyle)` | `DrawStatusBar` |

## 属性表单生成（当前实现）

```csharp
// 1. 用 AssetDatabase 构建 DefId→DefSO 查找表（绕过 GameService.Instance）
var defLookup = BuildDefLookup();  // AssetDatabase.FindAssets("t:PropertyDefSO")

// 2. 从 Template 解析全部节点（不依赖 GameService）
var allNodes = tree.ResolveAllNodes();
// → 自建 path → PropertyDefSO 映射

// 3. Slots 特殊处理 — Common/Slots 过滤，走专属 Slots 卡片
if (path == "Common/Slots") continue;

// 4. 其余属性逐行按 PropertyType 分发控件
// Float→FloatField, Int→IntField, Bool→Toggle, String→TextField,
// RdTag→TextField+TagPicker, RdTagList→TagPicker, AssetRef→ObjectField,
// AssetRefList→多行ObjectField, Struct→只读JSON
// 覆写值→白色, 默认值→灰色
```

## 保存

```csharp
// 1. 收集 _overrideValues → OverrideEntry 列表
// 2. 序列化 _slots → SlotListWrap → JSON → 写入 "Common/Slots"
// 3. JsonUtility.ToJson(OverrideContainer) → _selectedItem.OverridesJson
// 4. EditorUtility.SetDirty + AssetDatabase.SaveAssets
```

## 复用 API

| API | 来源 | 用途 |
|-----|------|------|
| `PropertyTreeSO.ResolveAllNodes()` | L3_Properties | 获取全部节点（不含 Def 查找） |
| `PropertyDefSO.Type/Min/Max/DefaultValue` | L3_Properties | 控件类型和约束 |
| `PropertyPresetSO.Template/OverridesJson/Prefab` | L3_Properties | 读写物品数据 |
| `EditorTreeView` / `EditorTreeNode` | Shared/Editor | 左栏树列表 |
| `EditorCard` / `EditorButton` / `EditorLabel` / `EditorInput` | Shared/Editor | UI 组件 |
| `EditorSearchBar` | Shared/Editor | 搜索栏 |
| `EditorTokens` | Shared/Editor | 布局/颜色/字号令牌 |
| `TagPicker` | L1_Core/Editor | 标签选择弹窗 |
| `AssetPreview.GetAssetPreview` | UnityEditor | Prefab 静态预览 |
| `DataLabelTools.EnsureBootLabel` | Shared/Editor | 新建资产标记 Addressables |
