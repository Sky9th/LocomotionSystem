# GameplayTag — Noise 标签域

> `L1_Core/GameplayTag/` · 2026-06-29 · L3_Ability 产出，AI 听觉系统消费
>
> 噪音事件类型。NoiseEventSO.noiseType → SNoiseEvent → AI 行为路由。

## 结构树

```
Noise
├── Combat
│   ├── WeaponFire
│   ├── MeleeSwing
│   ├── Explosion
│   └── Impact
├── World
│   ├── Footstep
│   ├── Door
│   ├── ItemUse
│   └── BodyFall
└── Alert
    ├── Voice
    ├── Death
    ├── Alarm
    ├── TrapTrigger
    └── Distraction
```

## 字段映射

| SO 字段 | 引用节点 | 匹配方式 |
|---------|---------|---------|
| `NoiseEventSO.noiseType` | `Noise.*` | HasTag 前缀 |
