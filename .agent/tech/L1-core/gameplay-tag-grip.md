# GameplayTag — Grip 标签域

> `L1_Core/RdTag/` · 2026-06-29 · Character/动画域，GripAnimationTableSO 消费
>
> 纯握法。HasTagExact 精确匹配。

## 结构树

```
Grip
├── Melee
│   ├── Unarmed
│   ├── OneHanded
│   ├── TwoHanded
│   ├── DualWield
│   ├── Fencing
│   └── Shield
└── Ranged
    ├── Pistol2H
    ├── DualPistol
    ├── Rifle
    ├── Shotgun
    ├── Bow
    ├── Launcher
    └── Heavy
```

## 字段映射

| SO 字段 | 引用节点 | 匹配方式 |
|---------|---------|---------|
| `GripAnimationTableSO.gripTag` | `Grip.*` | HasTagExact 精确 |
| `AbilityTreeSO.compatibleGripTags` | `Grip.*` | HasTag 前缀 |
