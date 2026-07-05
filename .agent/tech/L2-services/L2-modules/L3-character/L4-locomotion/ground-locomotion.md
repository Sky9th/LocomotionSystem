# GroundLocomotion · 地面移动仿真

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code
>
> `L3_Character/Locomotion/Ground/GroundLocomotion.cs` — ILocomotionSimulator 实现，串联 Motor → Stance
>
> v0.36.11: 新增 ComputeMotionSpeedScale 公式，接入 Agility/CarryWeight/Acceleration 三个 PropertyTable 属性。

## 调用链

```
被谁调:
  CharacterActor.Update() → locomotionSimulator.Simulate(ref frameCtx, input, buildCtx, dt)

调谁:
  PropertyTable.GetFloat()             → Agility / CarryWeight / Acceleration
  GroundSystemConfigSO                 → agilitySpeedBonus / weightPenaltyRatio
  RdContainer.CurrentTotalWeight       → 当前负重
  Motor.Evaluate()                     → ctx.Motor
  Stance.Evaluate()                    → ctx.Discrete (传入 computed motionSpeedScale)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 消费 | PropertyTable | 按需读取 Agility、CarryWeight、Acceleration |
| → 消费 | GroundSystemConfigSO | 公式系数（agilitySpeedBonus、weightPenaltyRatio）|
| → 消费 | Container.RdContainer | 当前负重（CurrentTotalWeight）|
| → 消费 | Motor | 速度/转角计算 |
| → 消费 | Stance | 离散状态判定 + 写入 MotionSpeedScale |
| ← 被调用 | CharacterActor | 每帧 Simulate() |

## 方法

### Simulate()
```csharp
public void Simulate(ref SCharacterFrameContext frameCtx, in SCharacterInputState input, CharacterBuildContext buildCtx, float dt)
```
- 从 `buildCtx.Properties` 读取 Agility、CarryWeight、Acceleration
- 调用 `ComputeMotionSpeedScale()` 计算速度系数
- `desiredSpeed = rawNativeSpeed × motionSpeedScale`（之前未乘系数）
- 将 `motionSpeedScale` 传入 `stance.Evaluate()`

### ComputeMotionSpeedScale()
```csharp
private static float ComputeMotionSpeedScale(float agility, float carryWeight, Container.RdContainer container, GroundSystemConfigSO config)
```
- 公式: `motionSpeedScale = 1 + agility × config.agilitySpeedBonus − weightPenalty`
- 负重惩罚: `weightPenalty = clamp(currentWeight / carryWeight, 0, 1) × config.weightPenaltyRatio`
- 系数由 GroundSystemConfigSO 全局配置驱动（默认 agilitySpeedBonus=0.03, weightPenaltyRatio=0.2）

## MotionSpeedScale 数据流

```
PropertyTable（Agility / CarryWeight / Acceleration）
  + GroundSystemConfigSO（agilitySpeedBonus / weightPenaltyRatio）
  + Container.CurrentTotalWeight
    → ComputeMotionSpeedScale()
      → desiredSpeed = rawNativeSpeed × motionSpeedScale
        → Motor.Evaluate()
      → motionSpeedScale → Stance.Evaluate()
        → SCharacterDiscrete.MotionSpeedScale
        → SCharacterDiscrete.EffectiveMaxSpeed = nativeSpeed × motionSpeedScale
          → PathfindingAgent.aiMaxSpeed
          → AnimationBrain.SpeedMultiplier
          → BaseMovingState blend parameter
```

## 设计决策

| 决策 | 理由 |
|------|------|
| 公式系数放在 GroundSystemConfigSO | 全局共享物理参数，数据驱动调参，不硬编码 |
| 姿势不参与速度系数 | LocomotionAnimationSetSO.GetNativeSpeed(gait) 已编码 Crawl=1.0 vs Walk=1.5，再乘会双重惩罚 |
| 公式输出不加 clamp | 输入受 PropertyDef Min/Max 约束（Agility∈[1,10], CarryWeight∈[10,200]），输出天然在安全范围 |
