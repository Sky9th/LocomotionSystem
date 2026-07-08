# CharacterEquipment — 装备 GO 生命周期

> `L3_Character/Equipment/CharacterEquipment.cs` — ModuleChild，每帧 diff BodyContainer 管理武器 GO 创建/销毁 + 同步 GripTag

> **Last Verified**: 2026-07-08

## 职责

| 职责 | 说明 |
|------|------|
| 武器 GO 生命周期 | 每帧 diff BodyContainer → Spawn/Despawn 武器 Prefab |
| GripTag 同步 | 从装备 Entity 的 `Common/Tags` 提取标签 → 写入 `OwnedGripTags` |
| 附着点委托 | 调 `WeaponAttachPoint.GetOrCreateSocket()` 获取挂点 |

## 调用链

```
CharacterActor.Update()
  └─ CharacterEquipment.SyncEquipment()
       ├─ ReadSlotState() → diff _slotSnapshot
       ├─ Added → SpawnView()
       │     ├─ SlotBoneMapper.GetBoneForSlot(animator, slotKey) → Transform
       │     ├─ WeaponAttachPoint.GetOrCreateSocket(bone, slotKey, entityTags) → socket Transform
       │     └─ Object.Instantiate(prefab, socket, worldPositionStays: false)
       ├─ Removed → DespawnView() → Object.Destroy(go)
       └─ SyncGripTags() → ctx.OwnedGripTags
```

## 公开 API

| 方法 | 签名 | 说明 |
|------|------|------|
| `SyncEquipment` | `void SyncEquipment()` | 每帧由 CharacterActor.Update 调用，在 anim set 解析之前 |

## 内部方法

| 方法 | 说明 |
|------|------|
| `ReadSlotState(container)` | 读 Container → `Dict<SlotKey, EntityId>` |
| `FindInSlot(container, slotKey, entityId)` | 按 ID 查找 Entity |
| `SpawnView(slotKey, entity)` | 挂载武器 GO（委托 WeaponAttachPoint 获取 Socket） |
| `DespawnView(slotKey)` | 销毁武器 GO |
| `SyncGripTags(container)` | 从所有装备 Entity 读 Common/Tags → 写入 OwnedGripTags |

## 数据结构

```
_slotSnapshot: Dict<string, string>        ← 上一帧 SlotKey→EntityId，用于 diff
_spawnedViews:  Dict<string, GameObject>    ← 已生成武器 GO，用于精准 Destroy
_animator: Animator                         ← OnWire 缓存
```

## 层级定位

`CharacterEquipment` 是 `CharacterActor` 的 `ModuleChild`，对标 RimWorld 的 `CompEquippable`。

武器在 Hierarchy 中的结构：
```
RightHand (bone)
  └── _EquipSocket_RightHand        ← WeaponAttachPoint 创建
        └── Revolver_xxx            ← 武器 GO（prefab.name_entityId）
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| CharacterEquipment | CharacterContainer.BodyContainer | 读装备实体 |
| CharacterEquipment | SlotBoneMapper | 槽位→骨骼映射 |
| CharacterEquipment | WeaponAttachPoint | 获取挂点 Socket |
| CharacterEquipment | CharacterBuildContext.OwnedGripTags | 写入标签 |
| CharacterEquipment | CharacterActor | 每帧调用 SyncEquipment |
| RangedWeaponSO | Entity.NestedContainer | 沿容器链递归查找弹药 Entity |
| RangedWeaponSO | AmmoSO.GetDamageEffects | 读取弹药 Weapon/ATK DamageEffectSO |

## 设计决策

| 决策 | 原因 |
|------|------|
| 不创建 `_EquipSocket` 中间 GO | 委托 WeaponAttachPoint，职责分离 |
| 删除 `GetAttachPoint()` | `_EquipSocket` 查找逻辑移入 WeaponAttachPoint |
| Per-frame diff | 用 GoF 做 diff 避免每帧 Instantiate/Destroy |
| ModuleChild 不是 MonoBehaviour | 与 CharacterActor 所有子模块保持一致 |
| RangedWeaponSO 伤害来自弹药 | 沿容器链递归查找弹药 Entity，调用 ammo.Preset.GetDamageEffects()。与 MeleeWeaponSO（直读自身 Weapon/ATK）对称 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| 防具槽（头部/身体/腿） | ✅ 已实现 | ArmorSO + ArmorBase/HeadArmor/BodyArmor/LegArmor Tree |
| 模型替换时 Despawn 全部 + 刷新 _animator + Respawn | 待做 | Phase 3 ReplaceModel |
| 背包槽 + 重量系统 | 远期 | InventoryComponent |
| 可拆卸配件系统 | 延后 | 独立弹匣/瞄具/消音器 Entity + 装卸 UI |
