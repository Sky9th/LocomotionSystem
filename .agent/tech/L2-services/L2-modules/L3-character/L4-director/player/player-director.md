# PlayerDirector · 玩家意图控制器

> `Character/Director/Player/PlayerDirector.cs` — ICharacterDirector 实现，组合输入 + 寻路

## 调用链

```
被谁调:
  CharacterActor.Update() → director.Evaluate()

调谁:
  PlayerInput → 读取鼠标/按键状态
  PathfindingAgent → SetDestination / DesiredVelocity / HasPath / HasReachedDestination
  SCharacterIntent → 构造并返回
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 调用 Evaluate() |
| 依赖 | PlayerInput | 聚合输入事件（右键/Shift/姿态键） |
| 依赖 | PathfindingAgent | 设置目的地 + 读取速度/状态 |
| 创建 | SCharacterIntent | 返回结构体 |

## 公开属性

```csharp
// ICharacterDirector 实现
SCharacterIntent Evaluate();
```

## 方法

### Evaluate()
```csharp
public SCharacterIntent Evaluate()
```
- **用途**: 每帧评估玩家意图，返回 SCharacterIntent
- **流程**:
  1. `ProcessClickToMove()` — 右键时调用 `agent.SetDestination(mousePos)`
  2. 计算 `hasActivePath = agent.HasPath && !agent.HasReachedDestination`
  3. 构造 Intent，传入 `externalMovementVelocity = agent.DesiredVelocity`, `overrideMovementVelocity = hasActivePath`
  4. 清除帧信号
- **调用者**: `CharacterActor.Update()`

### ComputeHeading()
```csharp
private Vector3 ComputeHeading()
```
- **用途**: 计算 LocomotionHeading
- **逻辑**: 当寻路激活时返回 `agent.DesiredVelocity.normalized`（平滑方向），否则返回 `modelRoot.forward`
- **备注**: 使用 `DesiredVelocity` 而非 `PathDirection`——AIPath 速度方向在转角时渐变，而 PathDirection 直接指向 waypoint 可能突变

### ProcessClickToMove()
```csharp
private void ProcessClickToMove()
```
- **用途**: 右键点击地面 → `agent.SetDestination(mouseGroundPosition)`，步态设为 Run

### ResolveGait()
```csharp
private EMovementGait ResolveGait()
```
- **用途**: 根据寻路状态和输入切换步态（Idle/Run/Sprint）
- **逻辑**: 有活跃路径或右键请求 → Run/Sprint；否则 → Idle

### ComputeAim()
```csharp
private Vector3 ComputeAim()
```
- **用途**: 返回鼠标地面位置方向（供 HeadLook 使用）

### ResolvePosture()
```csharp
private EPosture ResolvePosture()
```
- **用途**: 根据 Stand/Crouch/Prone 输入切换姿态

## 设计决策

| 决策 | 原因 |
|------|------|
| heading 用 `desiredVelocity.normalized` 替代 `PathDirection` | 速度方向经 AIPath 平滑，转向时自然过渡 |
| `hasActivePath` 同时检查 `!HasReachedDestination` | 到达终点时 override 和 gait 同帧切回 Idle |
| `externalMovementVelocity` 用 `?.` null-safe | 无 PathfindingAgent 时回退到 gait-based，不报错 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| WASD 移动输入支持 | 待做 | Phase 4+ |
| 左键战斗输入 | 待做 | Phase 4.1 |
