# L4_Equipment · 装备组件

> `L3_Character/Equipment/` — CharacterActor 子模块。管理角色身体容器——武器槽、防具槽、武器选择。角色插上此模块即具备穿戴装备的能力。

> ⚠ **DRAFT** — 未定稿。设计方向已确定，细节仍在讨论中。
>
> **Last Verified**: 2026-06-23 | **Verification**: DESIGN PHASE — 代码尚未创建

## 层级定位

L4 子系统，隶属于 `L3_Character`。是 CharacterActor 的 ModuleChild。对标 RimWorld 的 `CompEquippable`——一个可插拔的能力模块。

## 职责

| 职责 | 说明 |
|------|------|
| 身体容器管理 | 持有角色的装备槽（右手/左手/头部/背包…），每个槽是一个 `Container<ItemInstance>` |
| 武器选择 | 管理当前激活的武器槽（1/2/3 键切换） |
| GripTag 同步 | 激活武器时写入 `BuildContext.OwnedGripTags`，动画系统读出切换姿态 |
| 技能栏数据源 | 提供当前激活武器的 ItemDefSO，供技能栏求交 |
| UI 数据源 | 装备栏 UI 读取各槽位状态 |

## 架构

```
EquipmentComponent : ModuleChild
  ├── _slots: Container<ItemInstance>[]     ← 身体装备槽
  │     [0] 右手      filter: [Weapon.*, Tool]
  │     [1] 左手      filter: [Weapon.*, Shield]
  │     [2] 头部      filter: [Armor.Head]
  │     [3] 身体      filter: [Armor.Body]
  │     ...
  │
  ├── _activeWeaponIndex: int               ← 当前手持（-1=空手）
  │
  ├── Equip(int slotIndex, ItemInstance item)
  ├── Unequip(int slotIndex) → ItemInstance?
  ├── SetActiveWeapon(int slotIndex)
  ├── GetActiveWeapon() → ItemInstance?
  ├── GetSlotState(int index) → bool        ← UI 读：装备栏高亮
  └── GetActiveGripTags() → GameplayTag[]   ← → BuildContext.OwnedGripTags
```

## 调用链

```
武器切换（按 1 键）:
  PlayerDirector.Evaluate()
    → if input.Equip1Requested:
        equipment.SetActiveWeapon(0)        ← 激活右手槽
          → 从右手槽取出 ItemInstance
          → 读 gripTags → 写入 BuildContext.OwnedGripTags
          → Animation 读出 gripTag → 切换动画集

武器装备（从背包拖到右手槽）:
  UI → inventory.Remove(item)
    → equipment.Equip(RIGHT_HAND, item)
      → slot.Place(item)
      → ItemRegistry.Track(item.Id, this, "RightHand")

武器卸下:
  equipment.Unequip(RIGHT_HAND)
    → item = slot.Remove()
    → ItemRegistry.Untrack(item.Id)
    → 清除 GripTag

UI 查询:
  EquipmentBarOverlay.Update()
    → for i in 0..2:
        equipment.GetSlotState(i)           ← true=有武器 / false=空
        equipment.GetSlotItem(i)?.Def       ← 读图标显示
    → _activeWeaponIndex 高亮当前激活槽
```

## 与现有临时方案的替换

| 现有 | 替换为 |
|------|--------|
| `PlayerDirector.equippedSlots[3]` | `EquipmentComponent._activeWeaponIndex` |
| `PlayerDirector.ProcessEquipInput()` 直接写 `OwnedGripTags` | `EquipmentComponent.SetActiveWeapon()` |
| `CharacterBuildContext.SkillSlot1/2` | 技能栏求交结果，不由 EquipmentComponent 直接管理 |

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| EquipmentComponent | L3_Container（Container\<T\>） | 身体槽 = Container 实例 |
| EquipmentComponent | L3_Item（ItemInstance, ItemRegistry） | 槽位存放物品，移动时更新索引 |
| EquipmentComponent | Animation（GripAnimationTableSO） | 写入 GripTag → 动画读取 |
| EquipmentComponent | PlayerDirector | Director 调 SetActiveWeapon |
| EquipmentComponent | UI（EquipmentBarOverlay） | UI 读槽位状态 |
| EquipmentComponent | UI（AbilityBarOverlay） | 提供当前武器 → 技能栏求交 |

## 设计决策

| 决策 | 原因 |
|------|------|
| ModuleChild 不是 MonoBehaviour | CharacterActor 的所有子模块都是 ModuleChild。保持模式一致 |
| 三个武器槽对应 1/2/3 键 | 设计文档的武器切换方案。1=主武器，2=副武器，3=近战 |
| GripTag 由 EquipmentComponent 写入 | 单一数据源。Director 不再直接操作 OwnedGripTags |
| 不管理技能栏 | 技能栏是独立容器，由武器 × 技能树求交决定。EquipmentComponent 只提供"当前武器是什么" |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| EquipmentComponent 代码实现 | 待做 | Container\<T\> |
| 防具槽（头部/身体/腿） | 待做 | 防具 ItemDefSO |
| 背包槽 + 重量系统 | 远期 | InventoryComponent |
| 双手武器联合占用 | 远期 | 容器 linkedWith 机制 |
| EquipSlotConfigSO 数据驱动 | 远期 | A 测定型后 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| （待创建）equipment-component.md | EquipmentComponent — ModuleChild，身体容器管理 |
