# LocomotionEnums · 移动枚举定义

> `Character/Enums/LocomotionEnums.cs` — 3 个移动相关枚举

## 调用链

```
被谁调:
  Stance.Evaluate()              → 枚举判定/输出
  Motor.Evaluate()               → 枚举消费
  BaseLayer / FSM States         → 状态条件判定
  SCharacterDiscrete             → 字段类型
  CharacterActor.Debug           → Gizmo 标签枚举显示
```

## 枚举定义

### ELocomotionPhase
```csharp
public enum ELocomotionPhase
{
    GroundedIdle = 0,     // 地面站立
    GroundedMoving = 1,   // 地面移动
    Airborne = 2,         // 空中
    Landing = 3           // 落地中 (当前未使用)
}
```

### EPosture
```csharp
public enum EPosture
{
    Standing = 0,   // 站立
    Crouching = 1,  // 蹲伏
    Prone = 2       // 趴下
}
```

### EMovementGait
```csharp
public enum EMovementGait
{
    Idle = 0,     // 静止
    Walk = 1,     // 行走
    Run = 2,      // 跑步
    Sprint = 3,   // 冲刺
    Crawl = 4     // 爬行 (当前未使用)
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | SCharacterDiscrete | 字段类型引用 |
| 被依赖 | Stance | 枚举判定逻辑 |
| 被依赖 | BaseLayer FSM States | 状态条件判定 |
| 被依赖 | SprintStaminaRule | Gait 判断 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| ELocomotionPhase.Landing 状态驱动 | 待做 | 代码已定义但未使用 |
| EMovementGait.Crawl 实现 | 待做 | 代码已定义但未使用 |
| ELocomotionCondition — 受伤状态修正 | 待做 | 旧 module-analysis.md |
