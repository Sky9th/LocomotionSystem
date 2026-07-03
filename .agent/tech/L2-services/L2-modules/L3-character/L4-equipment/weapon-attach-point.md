# WeaponAttachPoint — 武器附着点 Soft Wave

> `L3_Character/Equipment/WeaponAttachPoint.cs` — 纯 C# static class，按 (slotKey, gripTag) 查 offset，骨骼下创建 `_EquipSocket` GO

> **Last Verified**: 2026-07-03

## 职责

武器 Prefab pivot 和角色骨骼位置都准确，但骨骼中心到握持面存在微小间隙。此类提供统一的 local-space 偏移，同一种武器类型共享同一个 offset，无需逐武器或逐模型调整。

| 职责 | 说明 |
|------|------|
| 查表解析 offset | `(slotKey, gripTag)` 字典查 (pos, rot Euler) |
| Socket GO 管理 | 在骨骼下创建/复用 `_EquipSocket_{slotKey}` GO |
| Offset 应用 | `SetLocalPositionAndRotation` 应用偏移 |

## 调用链

```
CharacterEquipment.SpawnView()
  └─ WeaponAttachPoint.GetOrCreateSocket(bone, slotKey, entityTags)
       ├─ bone.Find("_EquipSocket_{slotKey}")  → 复用或新建 GO
       ├─ Resolve(slotKey, entityTags)         → 查 _table
       │     ├─ (slotKey, tag) 精确匹配
       │     ├─ (slotKey, null) 默认回退
       │     └─ Vector3.zero 最终回退
       └─ socket.SetLocalPositionAndRotation(pos, rot)
```

## 公开 API

| 方法 | 签名 | 说明 |
|------|------|------|
| `GetOrCreateSocket` | `Transform GetOrCreateSocket(Transform bone, string slotKey, string[] entityTags)` | 获取或创建骨骼下的 Socket GO，应用 offset，返回 Socket Transform |

## 数据结构

```
_table: Dictionary<(string slotKey, string gripTag), (Vector3 pos, Vector3 rot)>

当前条目:
  (RightHand, Grip.Melee.OneHanded)  → pos(0.0865, 0.0455, -0.0335), rot(353.9, 175.3, 273.2)
  (RightHand, Grip.Ranged.Pistol2H)  → pos(0.0880, 0.0245, -0.0589), rot(12.5, 75.1, 251.1)
```

## 层级定位

`_EquipSocket_{slotKey}` GO 在 Hierarchy 中的结构：

```
RightHand (bone)
  └── _EquipSocket_RightHand  ← WeaponAttachPoint 创建，offset 在此
        └── Sword_Wooden_xxx  ← 武器 Prefab Instantiate 至此
```

同槽换武器时 Socket 复用，只更新 transform。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| WeaponAttachPoint | CharacterConst | 引用 Slot.* + GripTag.* 常量 |
| WeaponAttachPoint | CharacterEquipment | 被 SpawnView 调用 |
| WeaponAttachPoint | PropertyTable (via entity.Properties) | 读 Common/Tags → 匹配 gripTag |

## 设计决策

| 决策 | 原因 |
|------|------|
| 纯 C# static class | 用户选择不搞 SO，很长时间不需要资产灵活性 |
| 硬编码字典 | 条目少（当前 2 条），加一条改一行代码即可 |
| Socket GO 复用不销毁 | 同槽武器切换频繁，避免 GC 抖动。模型替换时由 ReplaceModel 清理 |
| offset 用 `SetLocalPositionAndRotation` | 原子设置，避免 IDE hint（比两次赋值更优） |
| gripTag 用 FullTag 字符串 | 对齐 rTagDefSO 层级，通过 CharacterConst.GripTag 约束一致性 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| 内容量大时迁 SO | 远期 | 武器类型 ≥ 10 种 |
| 左手槽 offset | 待做 | 左手武器模型 |
| 非人形 rig socket | 远期 | 非人形怪物需求 |
