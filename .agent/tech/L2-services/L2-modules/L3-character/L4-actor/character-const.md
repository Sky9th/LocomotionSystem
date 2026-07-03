# CharacterConst — Character 模块全局常量

> `L3_Character/Actor/CharacterConst.cs` — PropertyTree 路径 / rTag FullTag / 槽位 ID 常量集中定义

> **Last Verified**: 2026-07-03

## 职责

消除散落在 Character 各子模块中的硬编码字符串。PropertyTree 结构调整时只需改这一个文件。

## 结构

```
CharacterConst (public static class)
  ├── PropertyPath          ← PropertyTree 路径常量
  │     ├── CommonTags      "Common/Tags"
  │     ├── Slots           "Slots"
  │     ├── Vitals          "Vitals/HP", "Vitals/Hunger"
  │     ├── Attributes      "Attributes/Endurance"
  │     ├── Movement        "Movement/Acceleration", "Movement/MaxSlopeAngle"
  │     └── Body            "Body/Height", "Body/ObstacleProbeVertical", ...
  │
  ├── Slot                  ← 装备槽位 ID（对齐 PropertyTree Slots/ 节点名）
  │     ├── Body slots:     RightHand, LeftHand, Head, Chest, Back, RightLeg, LeftLeg, RightFoot, LeftFoot
  │     └── ContainerSlot   ← Container 内部通用槽位键
  │
  └── GripTag               ← 握持标签（对齐 rTagDefSO FullTag）
        ├── OneHanded       "Grip.Melee.OneHanded"
        └── Pistol2H        "Grip.Ranged.Pistol2H"
```

## 使用者

| 子类 | 使用者 |
|------|--------|
| `PropertyPath.CommonTags` | CharacterEquipment (×2) |
| `PropertyPath.Slots` | CharacterContainer |
| `PropertyPath.Vitals` | CharacterCombat, CharacterActor |
| `PropertyPath.Attributes` | CharacterCombat |
| `PropertyPath.Movement` | CharacterPhysique |
| `PropertyPath.Body` | CharacterPhysique |
| `Slot.*` | SlotBoneMapper, WeaponAttachPoint, PlayerDirector |
| `GripTag.*` | WeaponAttachPoint |

## 设计决策

| 决策 | 原因 |
|------|------|
| 单文件集中定义 | 所有 Character 代码引用同一真相源 |
| 嵌套 static class 分组 | IDE 自动补全时按语义分组，PropertyPath.Vitals.HP 比扁平命名更清晰 |
| tag 常量附层级文档 | XML doc 注释中包含 rTagDefSO 资产完整层级，方便追踪来源 |
| `public static class` | 编译时常量，零运行时开销 |

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| CharacterConst | CharacterEquipment | 引用 CommonTags |
| CharacterConst | CharacterContainer | 引用 Slots |
| CharacterConst | CharacterCombat | 引用 Vitals + Attributes |
| CharacterConst | CharacterActor | 引用 Vitals |
| CharacterConst | CharacterPhysique | 引用 Movement + Body |
| CharacterConst | SlotBoneMapper | 引用 Slot.* |
| CharacterConst | WeaponAttachPoint | 引用 Slot.* + GripTag.* |
| CharacterConst | PlayerDirector | 引用 Slot.* |

## 维护约定

- **新增 PropertyTree 路径** → 在 `PropertyPath` 对应子类加 const
- **新增槽位** → 在 `Slot` 加 const，确保与 PropertyTree `Slots/` 节点名一致
- **新增 grip tag rTagDefSO** → 在 `GripTag` 加 const，值对齐 `rTagDefSO.FullTag`
