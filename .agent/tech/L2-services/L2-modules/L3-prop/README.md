# L3_Prop · 道具系统

> `Assets/Scripts/Services/Modules/L3_Prop/` · L3 独立模块
> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

## 层级定位

L3 独立模块，位于 `L2-modules/L3-prop/`。从原 `L3_Item` 拆分而来。

道具预设继承 `PropertyPresetSO`，所有数据全进 PropertyTree。涵盖 6 个子类型。

## 架构

```
L3_Prop/
├── PropDefSO.cs             # [SO] 道具抽象基类 — 继承 PropertyPresetSO
├── ArmorSO.cs               # [SO] 防具 — 零 C# 字段
├── ConsumableSO.cs          # [SO] 消耗品 — Food + Medical
├── AmmoSO.cs                # [SO] 弹药 — 属性由 RangedWeaponSO 沿容器链查询
├── ToolSO.cs                # [SO] 工具
├── ContainerSO.cs           # [SO] 容器物品（背包等）— NestedContainer 由 EntityService 创建
├── MaterialSO.cs            # [SO] 材料
└── Editor/
    ├── PropEditorWindow.cs      # EntityEditorWindow 子类 — 编辑 6 种道具预设
    └── PropImportExport.cs      # PropImportWindow — JSON 导入/导出
```

## 调用链

```
定义时:
  PropEditorWindow → PropertyPresetSO.Template/OverridesJson/Prefab
  PropImportWindow → EntityImporter(EntityImportConfig)

运行时:
  ContainerSO → EntityService.Register → 读 Slots/ 创建 NestedContainer
  ConsumableSO → Ability Pipeline (未来)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyPresetSO, PropertyTreeSO | 属性定义 + 结构 |
| 依赖 | L3_Container (SlotDef) | ContainerSO 的槽位定义 |
| 被消费 | L2_EntityService | Spawn / Register |
| 被消费 | L2_PlayerService | FindItem<ContainerSO> 查找背包 |

## 设计决策

| 决策 | 原因 |
|------|------|
| PropDefSO 为抽象中间类 | 类型标记——6 个子类各自独立，但共享"道具"概念 |
| 6 个子类均为空壳 | 所有数据全进 PropertyTree，C# 仅用于类型区分 + CreateAssetMenu |
| 从 ItemDefSO 独立继承 PropertyPresetSO | 道具不是"物品"的子类——是独立领域概念 |

## 编辑器工具

| 工具 | 菜单 | 说明 |
|------|------|------|
| PropEditorWindow | `RedDust/Prop Editor` | EntityEditorWindow 子类，创建菜单含 6 种子类型。覆写 `GetAssetDirForType` — 按类型路由到 `Props/{Armor,Consumable,Ammo,Tool,Container,Material}/` |
| PropImportWindow | `RedDust/Prop Import-Export` | JSON 导入/导出，支持 6 种 entityType 标签 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| ConsumableSO 运行时行为 | 待做 | Ability Pipeline |
| ContainerSO NestedContainer 集成 | 待做 | EntityService.Register 自动创建 |
