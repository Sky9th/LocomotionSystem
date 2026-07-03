# GameplayTag — Entity 系统产出 Tag 域

> `L1_Core/RdTag/` · 2026-06-29 · Entity 系统产出
>
> 武器类型（Weapon）+ 物品分类（Item），通过 PropertyPreset → Common/Tags 定义。

## Weapon

```
Entity.Weapon
├── Melee
│   ├── Blade
│   ├── Axe
│   ├── Club
│   ├── Dagger
│   ├── Greatsword
│   ├── Polearm
│   └── Rapier
└── Ranged
    ├── Pistol
    ├── Rifle
    ├── Shotgun
    ├── Bow
    ├── Launcher
    └── Heavy
```

消费方：`AbilityTreeSO.compatibleWeaponTags`，HasTag 前缀匹配。

## Item

```
Entity.Item
├── Weapon
├── Armor
│   ├── Head
│   ├── Chest
│   ├── Legs
│   ├── Feet
│   └── Hands
├── Ammo
├── Consumable
├── Medical
├── Tool
├── Material
└── Component
```

消费方：`SlotDef.AcceptTags` + UI 分类。

## 武器 Entity 示例

```
Common/Tags = [Entity.Weapon.Melee.Blade, Grip.OneHanded, Entity.Item.Weapon]
```
