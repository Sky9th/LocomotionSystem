# 2026-06-01 Pathfinding Motor 集成

## 目标

打通 AIPath `desiredVelocity` → Motor 数据链路，让 A* 的平滑速度直接驱动角色移动，替代之前的 heading-based（固定步态全速）方案。

## 改了什么

| 文件 | 改动 |
|------|------|
| `SCharacterIntent.cs` | +`OverrideMovementVelocity`(bool) + `ExternalMovementVelocity`(Vector3)，构造函数可选参数 |
| `Motor.cs` | +override 分支（跳过自身 Smooth）+ `ConvertToLocal()` helper |
| `PlayerDirector.cs` | `ComputeHeading()` 改为 `desiredVelocity.normalized`，`Evaluate()` 传入 override 字段 |

## 设计决策

- **传递路径**: Intent 可选参数 → Motor，不新增接口或 Context 字段。向后兼容，非 override 路径完全不变。
- **跳过 Motor Smooth**: AIPath 内部已有加速/减速平滑（`CalculateAccelerationToReachPoint` + `ClampVelocity`），Motor 不应二次平滑。
- **heading 改用 velocity 方向**: 比 `PathDirection`（指向 waypoint）更平滑——AIPath 的 velocity 方向在转角时渐变而非突变。

## 链路验证

```
AIPath.desiredVelocity (world, smoothed, ≤ EffectiveMaxSpeed)
  → Motor: ConvertToLocal → (0, |velocity|) in heading space
  → BaseMovingState: blend = |velocity| / gaitSpeed
  → AnimationBrain: SpeedMultiplier = MotionSpeedScale
  → root motion deltaPosition ∝ blend × MotionSpeedScale ∝ desiredVelocity ✓
```

常见配置（motionSpeedScale=1）下 AIPath 任意速度均正确映射。clamp 边界（motionSpeedScale>1 时 blend clamp 到 1）是 Stance.cs EffectiveMaxSpeed 计算的既有问题，不阻塞本次集成。

## 已知问题

- `Stance.ComputeBaseSpeedScale()` 中的 `EffectiveMaxSpeed = gaitSpeed × motionSpeedScale` 在 motionSpeedScale>1 时会 double-scale，导致 AIPath maxSpeed 虚高。当前配置下 motionSpeedScale=1 不触发。后续修正。
- `BaseMovingState:34` 的 `parameter.sqrMagnitude > 1f → Normalize()` clamp 与上述问题连锁。

## 关联

- Plan: `short-term.md` Section 0.2
- Tech: `L4-director/s-character-intent.md`, `L4-locomotion/ground/motor.md`, `L4-director/player/player-director.md`, `L3-pathfinding/README.md`
- Version: `v0.6.2`
