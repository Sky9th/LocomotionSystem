# GameplayTag — Identity 模块产出 Tag 域

> `L1_Core/RdTag/` · 2026-06-29 · L3_Identity 模块产出
>
> 回答"这个 GameObject 在游戏世界里是谁"。四个正交维度。

## 结构树

```
Identity
├── Species
│   ├── Human
│   ├── Mutant
│   ├── Robot
│   └── Creature
├── Kind
│   ├── Player
│   ├── Companion
│   ├── NPC
│   └── Hostile
├── Faction
│   ├── Survivor
│   ├── Raider
│   ├── Mercenary
│   ├── Enclave
│   └── Nomad
└── Role
    ├── Scout
    ├── Guard
    ├── Brute
    ├── Sniper
    ├── Medic
    ├── Engineer
    ├── Trader
    └── Leader
```

## 字段映射

| SO 字段 | 引用节点 | 匹配方式 |
|---------|---------|---------|
| `Identity.initialTags` | `Identity.*` | 写入 Tags 容器 |
| `PassiveAbilitySO.targetRequiredTag` | `Identity.*` | HasTag 前缀 |

## 维度说明

| 维度 | 问题 | 可变性 |
|------|------|--------|
| Species | 什么物种？ | 不可变 |
| Kind | 游戏系统怎么对待它？ | 通常不变 |
| Faction | 属于哪个阵营？ | 可变 |
| Role | 承担什么职能？ | 可变 |
