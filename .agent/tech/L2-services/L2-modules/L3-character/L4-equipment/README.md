# L4_Equipment · 装备组件

> `L3_Character/Equipment/` — CharacterActor 子模块。每帧以 BodyContainer 为数据源同步武器 GO 和 GripTag。

> **Last Verified**: 2026-07-03

## 层级定位

L4 子系统，隶属于 `L3_Character`。是 CharacterActor 的 ModuleChild。

## 文件清单

| 文件 | 类 | 说明 |
|------|-----|------|
| `CharacterEquipment.cs` | `CharacterEquipment : ModuleChild` | 装备 GO 生命周期：diff → Spawn/Despawn → GripTag 同步 |
| `SlotBoneMapper.cs` | `SlotBoneMapper` (static) | SlotId → HumanBodyBones 静态映射 |
| `WeaponAttachPoint.cs` | `WeaponAttachPoint` (static) | 武器附着点 Soft Wave — (slotKey, gripTag) → offset → Socket GO |

## 调用链

```
CharacterActor.Update()
  └─ CharacterEquipment.SyncEquipment()
       ├─ ReadSlotState() → diff _slotSnapshot
       ├─ Added → SpawnView()
       │     ├─ SlotBoneMapper.GetBoneForSlot(animator, slotKey)   → bone Transform
       │     ├─ WeaponAttachPoint.GetOrCreateSocket(bone, slotKey, entityTags)  → socket Transform
       │     └─ Object.Instantiate(prefab, socket, worldPositionStays: false)
       ├─ Removed → DespawnView() → Object.Destroy(go)
       └─ SyncGripTags() → ctx.OwnedGripTags
```

## 武器 Hierarchy

```
RightHand (bone)
  └── _EquipSocket_RightHand        ← WeaponAttachPoint 创建/复用，承载 offset
        └── Revolver_xxx            ← 武器 GO
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| CharacterEquipment | CharacterContainer.BodyContainer | 读装备实体 |
| CharacterEquipment | SlotBoneMapper | 槽位→骨骼映射 |
| CharacterEquipment | WeaponAttachPoint | 获取挂点 Socket |
| CharacterEquipment | CharacterBuildContext.OwnedGripTags | 写入标签 |
| WeaponAttachPoint | CharacterConst | 引用 Slot.* + GripTag.* |
| SlotBoneMapper | CharacterConst | 引用 Slot.* |

## 设计决策

| 决策 | 原因 |
|------|------|
| WeaponAttachPoint 纯 C# 硬编码 | 很长时间不需要 SO 资产灵活性，条目少改一行即可 |
| Socket GO 运行时创建不复用销毁 | 同槽换武器只更新 transform，避免 GC 抖动 |
| GripTag 由 CharacterEquipment 写入 | 单一数据源，Director 不直接操作 OwnedGripTags |
| 槽位/标签常量集中在 CharacterConst | PropertyTree 结构调整只改一处 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [character-equipment.md](character-equipment.md) | CharacterEquipment — 装备 GO 生命周期 |
| [slot-bone-mapper.md](slot-bone-mapper.md) | SlotBoneMapper — SlotId→HumanBodyBones |
| [weapon-attach-point.md](weapon-attach-point.md) | WeaponAttachPoint — Soft Wave offset + Socket |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| 防具槽（头部/身体/腿） | 待做 | 防具 ItemDefSO |
| 背包槽 + 重量系统 | 远期 | InventoryComponent |
| 模型替换时清理 Socket | 待做 | Phase 3 ReplaceModel |
| WeaponAttachPoint 迁 SO | 远期 | 武器类型 ≥ 10 种 |
