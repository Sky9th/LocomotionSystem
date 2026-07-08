# L3_Building · 建筑系统

> `Assets/Scripts/Services/Modules/L3_Building/` · L3 独立模块
> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

## 层级定位

L3 独立模块，位于 `L2-modules/L3-building/`。全新创建的模块。

建筑预设继承 `PropertyPresetSO`，所有数据全进 PropertyTree。当前仅有类型定义，运行时行为待后续实现。

## 架构

```
L3_Building/
├── BuildingDefSO.cs         # [SO] 建筑预设 — 继承 PropertyPresetSO
└── Editor/
    ├── BuildingEditorWindow.cs  # EntityEditorWindow 子类
    └── BuildingImportExport.cs  # BuildingImportWindow — JSON 导入/导出
```

## 调用链

```
定义时:
  BuildingEditorWindow → PropertyPresetSO.Template/OverridesJson/Prefab

运行时（待实现）:
  EntityService.Spawn → Instantiate(Prefab) → BuildingActor
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyPresetSO, PropertyTreeSO | 属性定义 + 结构 |
| 被消费 | L2_EntityService | Spawn → Instantiate Prefab（未来） |

## 设计决策

| 决策 | 原因 |
|------|------|
| BuildingDefSO 直接继承 PropertyPresetSO | 建筑不是物品的子类——建造/拆除/升级是独立领域 |
| 新模块而非挂在 L3_Item 下 | 建筑数量庞大（墙壁、地板、工作台、防御工事等），需要独立管理 |

## 编辑器工具

| 工具 | 菜单 | 说明 |
|------|------|------|
| BuildingEditorWindow | `RedDust/Building Editor` | EntityEditorWindow 子类 |
| BuildingImportWindow | `RedDust/Building Import-Export` | JSON 导入/导出 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| BuildingActor 运行时行为 | 待做 | EntityService + PropertyTable 落地 |
| 建造系统集成 | 待做 | 设计定案 |
