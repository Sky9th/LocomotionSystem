# Character Enums · 角色枚举定义

> `L3_Character/Enums/` — 4 个枚举，独立文件。从 `LocomotionEnums.cs` 拆分而来（v0.20.2）
>
> **Last Verified**: 2026-06-20 | **Verification**: All referenced files exist, signatures match code

## 调用链

```
被谁调:
  Stance.Evaluate()              → Phase/Posture/Gait 枚举判定
  Motor.Evaluate()               → Gait 枚举消费
  BaseLayer / FSM States         → Phase/Gait/Posture 状态条件判定
  SCharacterDiscrete             → Phase/Posture/Gait 字段类型
  SCharacterIntent               → Gait/Posture/BodyForm 字段类型
  GripAnimationTableSO.Resolve() → EBodyForm 参数
  CharacterActor                 → BodyForm 缓存
  CharacterActor.Debug           → Gizmo 标签枚举显示
```

## 枚举定义

### ELocomotionPhase (`Enums/ELocomotionPhase.cs`)
```csharp
public enum ELocomotionPhase { GroundedIdle=0, GroundedMoving=1, Airborne=2, Landing=3 }
```
- **Phase**: 物理派生的运动相——地面/空中/落地

### EPosture (`Enums/EPosture.cs`)
```csharp
public enum EPosture { Standing=0, Crouching=1, Prone=2 }
```
- **Posture**: 高度姿态——站/蹲/趴

### EMovementGait (`Enums/EMovementGait.cs`)
```csharp
public enum EMovementGait { Idle=0, Walk=1, Run=2, Sprint=3, Crawl=4 }
```
- **Gait**: 移动步态——静止/走/跑/冲刺/爬行

### EBodyForm (`Enums/EBodyForm.cs`) — v0.20.2 新增
```csharp
public enum EBodyForm { Relax=0, Combat=1 }
```
- **BodyForm**: 战备形态——放松/战斗。Director 产出 → BuildContext 缓存 → GripTable 动画集选择

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | SCharacterDiscrete | Phase/Posture/Gait 字段类型 |
| 被依赖 | SCharacterIntent | Gait/Posture/BodyForm 字段类型 |
| 被依赖 | Stance | Phase 判定输出 |
| 被依赖 | BaseLayer FSM States | Phase/Gait/Posture 条件判定 |
| 被依赖 | GripAnimationTableSO | BodyForm 参数匹配 combatSet |
| 被依赖 | CharacterActor | BodyForm 缓存到 BuildContext |

## 设计决策

| 决策 | 理由 |
|------|------|
| 从 LocomotionEnums.cs 拆为 4 独立文件 | 一个枚举一个文件，模块根级 `Enums/` 目录 |
| BodyForm 不进 Discrete | 非 FSM 热路径消费方，放 BuildContext 更合适 |
| BodyForm 用枚举不用 Tag | Character 内部统一走枚举，Tag 留给外部系统查询 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Crawl 动画接入 | 待做 | 枚举已定义，动画未就位 |
| Landing Phase 落地驱动 | 待做 | 枚举已定义，逻辑未接入 |
