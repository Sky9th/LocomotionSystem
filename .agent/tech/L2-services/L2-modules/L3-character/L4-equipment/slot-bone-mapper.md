# SlotBoneMapper — 槽位→骨骼映射

> `L3_Character/Equipment/SlotBoneMapper.cs` — static utility，SlotId → HumanBodyBones 人形抽象

> **Last Verified**: 2026-07-03

## 职责

将装备槽位 ID 映射到 Unity Mecanim `HumanBodyBones`，通过 `Animator.GetBoneTransform()` 获取骨骼 Transform。不管底层骨骼命名差异，对人形怪通用。

## 映射表

| SlotId | HumanBodyBones |
|--------|---------------|
| `Slot.RightHand` | `HumanBodyBones.RightHand` |
| `Slot.LeftHand` | `HumanBodyBones.LeftHand` |
| `Slot.Head` | `HumanBodyBones.Head` |
| `Slot.Chest` | `HumanBodyBones.Chest` |
| `Slot.RightLeg` | `HumanBodyBones.RightUpperLeg` |
| `Slot.LeftLeg` | `HumanBodyBones.LeftUpperLeg` |
| `Slot.RightFoot` | `HumanBodyBones.RightFoot` |
| `Slot.LeftFoot` | `HumanBodyBones.LeftFoot` |

## 公开 API

| 方法 | 签名 | 说明 |
|------|------|------|
| `GetBoneForSlot` | `Transform GetBoneForSlot(Animator animator, string slotId)` | slotId → HumanBodyBones → bone Transform。非 humanoid 或未映射 → null |
| `HasMapping` | `bool HasMapping(string slotId)` | 检查 slotId 是否在映射表中 |

## 调用链

```
CharacterEquipment.SpawnView()
  └─ SlotBoneMapper.GetBoneForSlot(_animator, slotKey)
       ├─ animator.isHuman? → Yes: _map.TryGetValue(slotId, out bone)
       │                       └─ animator.GetBoneTransform(bone) → Transform
       └─ No → null
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| SlotBoneMapper | CharacterConst.Slot | 引用槽位 ID 常量 |
| SlotBoneMapper | CharacterEquipment | 被 SpawnView 调用 |
| SlotBoneMapper | Unity Animator | 调 GetBoneTransform(HumanBodyBones) |

## 设计决策

| 决策 | 原因 |
|------|------|
| static class | 纯工具函数，无状态 |
| HumanBodyBones 抽象 | 不管底层骨骼命名，所有 humanoid rig 通用 |
| 非人形返回 null | 调用方自行处理（CharacterEquipment 已处理） |
| 槽位键用 CharacterConst.Slot.* | 消除硬编码，PropertyTree 结构调整时同步更新 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| 非人形 rig 映射表 | 远期 | 非人形怪物需求 |
