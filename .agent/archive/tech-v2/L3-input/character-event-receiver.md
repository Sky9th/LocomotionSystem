# CharacterEventReceiver · 角色输入桥接

> `Assets/Scripts/Character/Input/CharacterEventReceiver.cs` — 非 MonoBehaviour，由 CharacterActor 持有。订阅所有 SIActionXxx 事件，帧内缓存，每帧聚合为 SCharacterInputActions 供 Actor 消费。

## 调用链

```
CharacterActor.Initialize()
  └── new CharacterEventReceiver(this)

CharacterActor.OnEnable()
  └── receiver.Subscribe()
      └── EventDispatcher.Subscribe<SIActionMove/Look/Crouch/...>(Handler)

每帧 EventDispatcher.Publish(SIActionXxx)
  └── receiver.PutAction(payload)
      └── 类型匹配 → 写入对应字段

CharacterActor.Evaluate() 帧循环
  └── receiver.ReadActions(out SCharacterInputActions)
      └── 聚合所有动作为单个 struct
      └── 调用每个按钮 ClearFrameSignals()
  └── receiver.ReadPrimaryInteract(out action)
  └── receiver.ReadSecondaryInteract(out action)
  └── receiver.ReadCameraControl(out SCameraSnapshot)
  └── receiver.ReadMouseGroundPosition(out worldPosition)

CharacterActor.OnDisable()
  └── receiver.Unsubscribe()

CharacterActor.Reset()
  └── receiver.Reset() — 清空所有缓存
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | 01-core (EventDispatcherService) | 订阅/取消订阅输入事件 |
| 依赖 | 01-core (SCameraSnapshot) | 读取摄像机快照 |
| 依赖 | 03-input (所有 SIActionXxx) | 订阅全部输入动作 struct |
| 持有 | 02-character (CharacterActor) | Actor 创建、持有、驱动生命周期 |
| 产出 | SCharacterInputActions | ReadActions 聚合输出 |

## 公开方法

### CharacterEventReceiver()
```csharp
internal CharacterEventReceiver(CharacterActor owner)
```
- **用途**: 构造函数，注册所有订阅（Move 除外，TODO 中被禁用）
- **参数**: `owner` — 所属 CharacterActor
- **调用者**: CharacterActor.Initialize()
- **备注**: SIActionMove 的 Register 被注释，由 A* Pathfinding 替代

### Subscribe()
```csharp
internal void Subscribe()
```
- **用途**: 注册所有订阅到 EventDispatcher
- **调用者**: CharacterActor.OnEnable()
- **备注**: 幂等，isSubscribed 后跳过

### Unsubscribe()
```csharp
internal void Unsubscribe()
```
- **用途**: 取消所有订阅
- **调用者**: CharacterActor.OnDisable()

### Reset()
```csharp
internal void Reset()
```
- **用途**: 清除所有缓存的动作数据
- **调用者**: CharacterActor.Reset() (如会话结束)
- **备注**: 回到初始空状态

### ReadActions()
```csharp
internal void ReadActions(out SCharacterInputActions actions)
```
- **用途**: 聚合当前帧所有动作为 SCharacterInputActions，然后清除帧信号
- **参数**: `actions` — 输出参数，聚合后的所有动作
- **调用者**: CharacterActor.Evaluate()
- **备注**: 调用所有按钮 struct 的 ClearFrameSignals()

### ReadPrimaryInteract()
```csharp
internal bool ReadPrimaryInteract(out SIActionPrimaryInteract action)
```
- **用途**: 读取主交互状态
- **返回**: Button.IsRequested
- **调用者**: CharacterActor 交互逻辑

### ReadSecondaryInteract()
```csharp
internal bool ReadSecondaryInteract(out SIActionSecondaryInteract action)
```
- **用途**: 读取副交互状态
- **返回**: Button.IsRequested
- **调用者**: CharacterActor 交互逻辑

### ReadCameraControl()
```csharp
internal bool ReadCameraControl(out SCameraSnapshot control)
```
- **用途**: 读取摄像机快照
- **返回**: hasCameraControl
- **调用者**: CharacterActor 摄像机相关逻辑

### ReadMouseGroundPosition()
```csharp
internal bool ReadMouseGroundPosition(out Vector3 worldPosition)
```
- **用途**: 读取鼠标地面坐标
- **返回**: hasMouseGround
- **调用者**: CharacterActor 目标指示相关逻辑

## 内部机制

### Subscription struct
```csharp
private readonly struct Subscription
{
    public readonly Action<EventDispatcherService> Subscribe;
    public readonly Action<EventDispatcherService> Unsubscribe;
}
```
- 每类事件一个 Subscription，统一注册/注销

### Register\<TPayload\>()
```csharp
private void Register<TPayload>() where TPayload : struct
```
- **用途**: 泛型方法，为 TPayload 类型创建 Handler + Subscription
- **调用者**: 构造函数
- **备注**: Handler 内部先检查 owner 有效性，再调用 PutAction

### PutAction\<TPayload\>()
```csharp
private void PutAction<TPayload>(TPayload payload) where TPayload : struct
```
- **用途**: 通过运行时类型匹配写入对应字段
- **调用者**: 内部事件 Handler
- **备注**: 使用 `typeof(TPayload) == typeof(SIActionMove)` 链式匹配，而非面向对象方式

### RegisterCamera()
```csharp
private void RegisterCamera()
```
- **用途**: 单独注册 SCameraSnapshot 订阅
- **调用者**: 构造函数

### HandleCameraSnapshot()
```csharp
private void HandleCameraSnapshot(SCameraSnapshot snapshot, MetaStruct meta)
```
- **用途**: 缓存摄像机快照和鼠标地面坐标
- **调用者**: EventDispatcherService
- **备注**: 只缓存 isPlayer 角色的数据

### TryResolveDispatcher()
```csharp
private static bool TryResolveDispatcher(out EventDispatcherService dispatcher)
```
- **用途**: 通过 GameContext 查找 EventDispatcherService
- **返回**: 是否找到
- **调用者**: Subscribe()

## SCharacterInputActions

> `Assets/Scripts/Character/Input/SCharacterInputActions.cs` — 只读 struct，聚合所有输入动作的当前帧状态。

### 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `MoveAction` | SIActionMove | 当前帧移动输入 |
| `LastMoveAction` | SIActionMove | 上一帧移动输入（用于方向变化检测） |
| `LookAction` | SIActionLook | 当前帧朝向输入 |
| `CrouchAction` | SIActionCrouch | 当前帧蹲下状态 |
| `ProneAction` | SIActionProne | 当前帧趴下状态 |
| `WalkAction` | SIActionWalk | 当前帧行走切换 |
| `RunAction` | SIActionRun | 当前帧跑步切换 |
| `SprintAction` | SIActionSprint | 当前帧冲刺状态 |
| `JumpAction` | SIActionJump | 当前帧跳跃状态 |
| `StandAction` | SIActionStand | 当前帧站立状态 |

```csharp
public static SCharacterInputActions None => /* 所有字段初始化为 None */
```

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| WASD 解禁（Phase 4 A* Pathfinding 替代驱动） | 待做 | 09-pathfinding | 代码 TODO:50 |
| MoveAction 恢复注册 | 待做 | 09-pathfinding | CharacterEventReceiver.cs:49-50 |
