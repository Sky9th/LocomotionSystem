# GameplayTag — Character 模块产出 Tag 域

> `L1_Core/RdTag/` · 2026-06-29 · L3_Character 模块产出
>
> 角色的物理身体状态。每帧从枚举单向派生、全量刷新、外部只读。

## 结构树

```
Body
├── Form
│   ├── Relax
│   └── Combat
├── Posture
│   ├── Standing
│   ├── Crouching
│   └── Prone
└── Locomotion
    ├── Idle
    ├── Walk
    ├── Run
    ├── Sprint
    └── Crawl
```

## 字段映射

| SO 字段 | 引用节点 | 匹配方式 |
|---------|---------|---------|
| `CharacterActor` → OwnedTags | `Body.*` | 全量刷新写入，外部只读 |

## 枚举源

| 维度 | 枚举 | 说明 |
|------|------|------|
| Form | `EBodyForm` | 战备形态 |
| Posture | `EPosture` | 高度姿态 |
| Locomotion | `EMovementGait` | 移动步态 |

## 边界

不属于 Body 的概念：
- `ELocomotionPhase`（Airborne 等）→ 环境物理结果，非身体配置
- `State.*` → 角色在做什么，非身体是什么
- `Equip.Grip.*` → Equipment 模块产出，Character 消费
