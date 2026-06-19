# 2026-06-19 — Properties 接管角色物理配置

## 背景

`CharacterPhysicsProfileSO` 包装 `LocomotionProfileSO` + `KinematicProfileSO`，承载了速度矩阵、物理探测参数、头部转角等 30+ 字段。与此同时 `PropertyAgent` 已建成，管着 HP/Stamina/Agility 等动态数值。两套属性系统并存，且 locomotion 速度与 AnimationSet 的 `animNativeSpeed` 存在语义重叠。

## 决策过程

1. **四轮辩论**（正反方）确认：速度矩阵与 animNativeSpeed 重复，物理参数天然是角色属性，合并进 Properties 不违反职责边界
2. 先拆分系统级参数（`GroundSystemConfigSO`），再消除速度矩阵，最后将角色属性并入 `CharacterPhysique` struct
3. 运行时方案：Init 时从 `PropertyAgent` 读一次填 struct → hot path 零字符串开销

## 变更摘要

- 删 3 个 SO：`CharacterPhysicsProfileSO`, `LocomotionProfileSO`, `KinematicProfileSO`
- 新建：`GroundSystemConfigSO`（8 世界参数）, `CharacterPhysique`（9 字段 struct 缓存）
- `properties_all.json`：+9 PropertyDef（Movement/ +2, Body/ +7）
- Human 树：新增 `Body/` 文件夹
- 更新 12 个消费者文件（Motor, Stance, CharacterKinematic, CharacterHeadLook 等）
