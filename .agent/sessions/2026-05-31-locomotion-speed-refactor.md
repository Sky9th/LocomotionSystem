# 2026-05-31 L4_Locomotion 目录重组 & MotionSpeedScale 架构重构

## 改动范围

16 个文件，43 增 / 296 删。覆盖 Locomotion、Animation、Pathfinding、Actor。

## 主要改动

### 目录重组
- `L4_Locomotion/` 新增 `Config/`、`Ground/` 子文件夹
- `LocomotionProfile.cs` → `Config/`
- `GroundLocomotion.cs`、`Motor.cs`、`Stance.cs` → `Ground/`
- 按实现分组：接口在根，实现在 Ground/

### MotionSpeedScale 概念引入
- 新增 `SCharacterDiscrete.MotionSpeedScale` — 有效速度/步态速度比值
- 新增 `SCharacterDiscrete.EffectiveMaxSpeed` — gaitSpeed × scale 最终值
- 由 Stance 内部计算并缓存（gait/posture 变化时重算）
- 基础比值 = `gaitSpeed / animNativeSpeed`，取自两个 Profile
- 数据流方向纠正为 **Locomotion → Pathfinding**（非反向）

### 数据流简化
- `CharacterFrameContext` 承载 `LocomotionProfile` + `LocomotionAnimationProfile`
- `CharacterActor` 两个 Profile 均设为 Inspector 序列化字段
- `PathfindingAgent` 精简为单一入口 `SyncLocomotion(in SCharacterDiscrete)`
- 删除 `PathfindingAgent` 中的 5 个冗余字段/方法
- `AnimationBrain.ApplySpeedMultiplier` 简化为直接读 `ctx.Discrete.MotionSpeedScale`

### 冗余删除
- `SCharacterIntent.MovementSpeedMultiplier`
- `PlayerDirector.ComputeSpeedMultiplier`
- `PathfindingAgent.DesiredSpeedMultiplier`、`locomotionProfile`、`currentGait`、`UpdateGaitSpeed`、`ApplyMotionScale`
- `AnimationBrain.GetMotionSpeedScale`
- `CharacterFrameContext.PathDesiredVelocity`、`BaseSpeedScale`
- `CharacterActor.PlanarSpeed`

## 设计决策

- 速度乘数是 Locomotion 概念，不是 Pathfinding 或 Animation 概念
- 基础比值（gaitSpeed/animNativeSpeed）在 Stance 内部缓存，仅 gait/posture 变化时重算
- PathfindingAgent 只消费 `EffectiveMaxSpeed`，不知道 gait × scale 的乘法
- 两个 Profile 都是角色层面的配置，直接放在 CharacterActor Inspector
