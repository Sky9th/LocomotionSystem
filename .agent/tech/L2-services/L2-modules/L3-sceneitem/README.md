# L3_SceneItem · 场景物品系统

> `Assets/Scripts/Services/Modules/L3_SceneItem/` · L3 独立模块
> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

## 层级定位

L3 独立模块，位于 `L2-modules/L3-sceneitem/`。从原 `L3_Item` 拆分而来。

场景物品涵盖家具、装饰物、场景物体。当前仅有 `SceneItemDefSO` 类型定义，未来将承载 672 个 PolygonApocalypse 资产。

## 架构

```
L3_SceneItem/
├── SceneItemDefSO.cs        # [SO] 场景物品预设 — 继承 PropertyPresetSO
└── Editor/
    ├── SceneItemEditorWindow.cs    # EntityEditorWindow 子类
    └── SceneItemImportExport.cs    # SceneItemImportWindow — JSON 导入/导出
```

## 调用链

```
定义时:
  SceneItemEditorWindow → PropertyPresetSO.Template/OverridesJson/Prefab

运行时:
  EntityService.Spawn → Instantiate(Prefab) → 静态场景物体（无 AI/物理交互）
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyPresetSO, PropertyTreeSO | 属性定义 + 结构 |
| 被消费 | L2_EntityService | Spawn → Instantiate Prefab |

## 设计决策

| 决策 | 原因 |
|------|------|
| SceneItemDefSO 走完整 PropertyTree | 未来可能有可破坏/可拾取/可燃烧等交互属性 |
| 独立 L3 模块（不从属于 Item/Prop） | 场景物品是不同的概念——静态世界装饰 vs 可携带物品 |

## 编辑器工具

| 工具 | 菜单 | 说明 |
|------|------|------|
| SceneItemEditorWindow | `RedDust/Scene Item Editor` | EntityEditorWindow 子类 |
| SceneItemImportWindow | `RedDust/Scene Item Import-Export` | JSON 导入/导出 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| 672 PolygonApocalypse 资产批量导入 | 待做 | SceneItemImporter + PropertyTree 模板 |
| 交互属性 PropertyTree 设计 | 待做 | 设计定案 |
