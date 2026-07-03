# Entity.Command & Entity.Query — 实体命令/查询门面

> **Last Verified**: 2026-07-03 | **Verification**: New modules, all files exist

> `L2_EntityService/` — EntityCommandModule, EntityQueryModule, VitalsQuery, InventoryQuery, EquipmentQuery

## Layer Position

L2 — Entity Service 扩展。挂在 Entity 上，外部系统通过这两个门面与实体交互。

## Architecture

```
Entity
 ├── Command (EntityCommandModule) — 写门面
 │     ├── MoveTo / StopMoving → CharacterActor.Pathfinding
 │     ├── UseActiveAbility → CharacterActor.Ability
 │     └── CycleEquip → CharacterActor.Container
 └── Query (EntityQueryModule) — 读门面，始终可用（无需 GO）
       ├── L0 Identity — Id / Preset / Properties / NestedContainer
       ├── L1 Vitals — VitalsQuery (HP, MaxHP, Hunger, MaxHunger, IsAlive)
       ├── L2 Inventory — InventoryQuery (AllItems, FindItem, HasItem, CountItem)
       ├── L3 Equipment — EquipmentQuery (GetEquipped, RightHand) — null if not character
       └── L5 State — LastKnownPosition (View.transform, null if no GO)
```

## Design Decisions

| 决策 | 理由 |
|------|------|
| Command 通过 View.GetComponent<CharacterActor>() 找运行时模块 | 不需要 Actor 注册自己（避免 L3→L2 反向依赖） |
| Query 不依赖 CharacterActor | 读的是 Entity 自己的数据（Properties/NestedContainer/View.transform） |
| 不定义接口 | 只有一个实现者时不抽象 |
| 无 null guard | 不支持的命令直接 NPE，fail fast |
