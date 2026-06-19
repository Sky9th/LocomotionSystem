# Character Profile SO · 角色配置资产

> L4 Config — 角色配置资产清单。原有 `CharacterPhysicsProfileSO` 已删除，角色物理属性统一接入 Properties 系统。

## 当前配置资产

| 资产 | 职责 |
|------|------|
| `CharacterDefSO` | 实体定义（绑定 PropertyTree） |
| `CharacterAnimationProfileSO` | 动画配置（Locomotion 动画集、Mode 配置、GripTable） |
| `CharacterAudioConfigSO` | 音效配置 |
| `GroundSystemConfigSO` | **世界级**地面探测/锁地参数，所有角色共享 |

## 角色物理属性 — Properties

原 `LocomotionProfileSO` + `KinematicProfileSO` 已迁移至 Properties 系统，运行时通过 `CharacterPhysique` struct 缓存读取。

```
PropertyAgent (9 个 Float PropertyDef)
     ↓  Init 时读一次
CharacterPhysique struct
     ↓  每帧零开销
Motor / CharacterKinematic / CharacterHeadLook
```

详见 [CharacterPhysique](../L4-kinematic/character-physique.md) 和 [GroundSystemConfigSO](../L4-kinematic/config/ground-system-config-so.md)。

## 历史

- v0.18 前：`CharacterPhysicsProfileSO` 包装 `LocomotionProfileSO` + `KinematicProfileSO`
- v0.19：三个 SO 删除，角色物理属性统一接入 Properties
