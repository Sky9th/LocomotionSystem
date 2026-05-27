# 新旧代码模块覆盖对比

> 日期: 2026-05-09
> 对比范围: `.agent/backup/character-backup/` (旧) vs `Assets/Scripts/Character/` (新)

## 已完整覆盖

| 模块 | 旧 | 新 | 说明 |
|------|-----|-----|------|
| Locomotion FSM (7状态) | `BaseLayer` + States | 移植 | 落地系统有增强 |
| Motor / Stance | `LocomotionMotor` + Aspects | `Motor.cs` + `Stance.cs` | 条件内联，turning auto-reset 增强 |
| Ground Detection | BoxCast + Raycast 双探头 | **SphereCast 单探头** | 重构，斜坡接触更稳 |
| Obstacle Detection | 前方射线 + 高度探针 | 移植 + 改进 | 高度从脚底算，minClimbHeight |
| Kinematic | `CharacterKinematic` + `CharacterHeadLook` | 移植 | |
| DriverArbiter | 单仲裁 | 扩展 | Evaluate 接口 + OnStarted/Completed |
| TraversalDriver | 桩代码 | **完整实现** | 攀爬物理 + 生命周期 |
| CharacterRig | `LocomotionMotor` 物理集成 | **统一入口** | SuppressGroundLock/SetKinematic/IgnoreCollision/ZeroVelocity |
| AnimationRequest | `OnCompleted` 回调 | 扩展 | OnComplete/OnInterrupted/FadeIn/FadeOut |
| Landing 落地 | 单级 AirLand | **三级分级** | LandLight/Medium/Hard + 坠落距离追踪 + Y解锁 |
| Input | `CharacterInputModule` | 移植 + 快照输入 | SCharacterSnapshot.Input |
| Posture/Gait/Phase | Enums + Aspects | `Stance.Evaluate*` | 移植 |
| Debug Gizmos | `CharacterLocomotion.Debug` | `CharacterActor.Debug` | 适配 SphereCast |
| Ground Lock | FreezePositionY + SetGroundedY | 移植 + ZeroVelocity | |
| Turning | `LocomotionTurningGraph` | `Stance.EvaluateTurning` | auto-reset 增强 |
| 动画过渡 | 无 | **移除 layer.Stop** | 自然交叉淡入淡出 |

## 有骨架但无实现

| 模块 | 现状 | 工作量 |
|------|------|--------|
| **HeadLook 头部注视** | Vector2Mixer 已创建，`UpdateHeadLook()` 空 TODO | 中 |
| **Footstep 脚步声** | Layer 6 + mask 已绑，无驱动逻辑 | 中 |
| **UpperBody/Additive/Facial 多层仲裁** | Layer 1-3 + mask 已绑，无 Arbiter | 大 |

## 两者都未实现

| 功能 | 说明 | 工作量 |
|------|------|--------|
| **Vault / StepOver** | canVault/canStepOver 字段返回 false | 大 |
| **Crawl 爬行** | Gait 枚举有 Crawl，Stance 从未赋值 | 中 |
| **姿势物理联动** | canCrouch/canProne 有开关，碰撞体/速度不变 | 中 |

## 刻意移除的旧设计

| 旧模块 | 原因 |
|------|------|
| 可组合条件系统 (ICheck/And/Or/Not) | 条件内联到 CanEnterState |
| LocomotionTraversalGraph 状态机 | TraversalDriver 直接处理 |
| ELocomotionCondition (受伤等) | 未使用 |
| 事件分发器发布快照 | 未使用 |
| 旧 ICharacterAnimationSource 接口 | 简化为组件自注册 |
| ELocomotionTraversalType/Stage 枚举 | TraversalDriver 内联 |
