# 2026-07-03 — WeaponAttachPoint Soft Wave + CharacterConst 全局常量收敛

## Background

**问题**: Equipment 模块的 `_EquipSocket_{slotKey}` 设计依赖美术在角色模型 Prefab 上手摆挂点，且零武器类型区分能力。武器 Prefab 的 pivot 和模型骨骼位置都准确，但骨骼中心到握持面存在微小间隙——需要一种数据驱动的"Soft Wave"偏移方案。

**目标**: 1) 创建 `WeaponAttachPoint` 纯 C# 类，按 (slotKey, gripTag) 查表提供统一 offset，替代 Prefab 手摆；2) 将 Character 模块内散落的 PropertyTree 路径、rTag、槽位 ID 硬编码字符串全部收敛到 `CharacterConst` 全局常量文件。

## Changes

### 新增

**WeaponAttachPoint** (`L3_Character/Equipment/WeaponAttachPoint.cs`)
- 纯 C# static class，无 Unity 依赖，零分配
- `GetOrCreateSocket(bone, slotKey, entityTags)` — 在骨骼下创建/复用 `_EquipSocket_{slotKey}` GO，应用 offset
- 硬编码查表 `(slotKey, gripTag) → (pos, rot)` — 含单手剑 + 手枪实测值
- gripTag 字符串对齐 rTagDefSO FullTag（`Grip.Melee.OneHanded`, `Grip.Ranged.Pistol2H`）

**CharacterConst** (`L3_Character/Actor/CharacterConst.cs`)
- `PropertyPath` — PropertyTree 路径（CommonTags, Slots, Vitals, Attributes, Movement, Body）
- `Slot` — 槽位 ID（9 个身体槽 + ContainerSlot）
- `GripTag` — 握持标签常量，附 rTagDefSO 层级文档

### 修改

| 文件 | 说明 |
|------|------|
| `CharacterEquipment.cs` | SpawnView 接入 `WeaponAttachPoint.GetOrCreateSocket()`；删除 `GetAttachPoint()` |
| `SlotBoneMapper.cs` | 硬编码 `"RightHand"` 等 → `CharacterConst.Slot.*` |
| `PlayerDirector.cs` | `"RightHand"` ×5, `"Back"`, `"ContainerSlot"` ×3 → `CharacterConst.Slot.*` |
| `CharacterPhysique.cs` | 9 个 `GetFloat("Body/...")` → `CharacterConst.PropertyPath.Body.*` |
| `CharacterCombat.cs` | `"Vitals/HP"`, `"Attributes/Endurance"` → `CharacterConst.PropertyPath.*` |
| `CharacterContainer.cs` | `"Slots"` → `CharacterConst.PropertyPath.Slots` |
| `CharacterActor.cs` | 删除 T 键测试代码；`"Vitals/Hunger"` → `CharacterConst` |
| Revolver/Machete prefab | 根节点 transform 归零（offset 改由 WeaponAttachPoint 接管） |

### 删除

| 文件 | 说明 |
|------|------|
| `EquipSlot.cs` | 合并入 `CharacterConst.Slot` |
| `GripTag.cs` | 合并入 `CharacterConst.GripTag` |

## Decisions

| 决策 | 替代方案（被拒绝） | 理由 |
|------|-------------------|------|
| 纯 C# 硬编码查表，不做 SO | SocketPoseSO / GripOffsetTableSO | 很长时间不需要资产灵活性。后面需要时再迁 SO，当前硬编码改动快、零依赖 |
| offset 直接应用在 Socket GO | 直接 apply 到武器 GO localPosition | 用户要求 Hierachy 中可见 Socket 中间对象，方便调试 |
| gripTag 用 rTagDefSO FullTag 字符串匹配 | 引用 rTagDefSO 资产 | 用户选择纯 C# 不搞 SO；通过 GripTag 常量文件约束一致性 |
| 常量收敛到 `CharacterConst` 单文件 | 分散在各子模块各自的 Constants.cs | 统一管理 PropertyTree 结构调整时只需改一处 |
| 命名为 `WeaponAttachPoint` | GripOffsetTable, SocketPose, EquipSocket | 用户选定——比 GripOffsetTable 更直观表达"武器附着点" |
| 版号 0.33.1 | 0.34.0 | 装备系统仍标记为临时方案，未形成完整子系统落地 |

## Known Issues

- **硬编码查表** — 新武器类型需改代码加一行。未来内容量大时考虑迁 SO
- **Rotation 值偏大** — 单手剑 Euler ≈ (354°, 175°, 273°)，可能是武器 prefab 默认朝向与骨骼不一致。offset 叠加方式 (`*=` ) 已验证正确，但原因待查
- **CharacterPhysique.From() 手动维护** — 属性路径映射仍是手动，后续可用 source generator 或自动化
- **`_EquipSocket` GO 生命周期** — Socket GO 创建后不销毁，模型替换时需清理（Phase 3 ReplaceModel）

## Cross-References

- Plan: [plans/equipsocket-offset-soft-wave.md](../plans/equipsocket-offset-soft-wave.md)
- Tech: [L4-equipment/weapon-attach-point.md](../tech/L2-services/L2-modules/L3-character/L4-equipment/weapon-attach-point.md)
- Tech: [L4-actor/character-const.md](../tech/L2-services/L2-modules/L3-character/L4-actor/character-const.md)
- Tech: [L4-equipment/README.md](../tech/L2-services/L2-modules/L3-character/L4-equipment/README.md)
- Version: [v0.33.1](../versions/v0.33.1.md)

### Flag for Design Doc Creation
- [x] No design doc needed — internal implementation details, no player-visible behavior changes.
