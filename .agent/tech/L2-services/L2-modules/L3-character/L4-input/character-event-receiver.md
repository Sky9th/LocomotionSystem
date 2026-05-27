# CharacterEventReceiver · 输入桥接

> `Character/Input/CharacterEventReceiver.cs` — 纯 C# 类，订阅 EventDispatcher 输入事件，聚合到 SCharacterInputActions

## 调用链

```
被谁调:
  CharacterActor.Awake()     → new CharacterEventReceiver(this)
  CharacterActor.OnEnable()  → Subscribe()
  CharacterActor.OnDisable() → Unsubscribe() / Reset()
  CharacterActor.Update()    → ReadActions() / ReadMouseGroundPosition()

调谁:
  EventDispatcher.Subscribe<TPayload>()  → 订阅输入事件
  EventDispatcher.Unsubscribe<TPayload>() → 取消订阅
  PutAction<T>(payload)                  → 类型分支分发到对应字段
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 输入消费 |
| 依赖 | EventDispatcherService | 事件订阅/取消 |
| 依赖 | SIActionMove | 移动输入 struct |
| 依赖 | SIActionLook | 朝向输入 struct |
| 依赖 | SIActionCrouch/Prone/Walk/Run/Sprint/Jump/Stand | 按钮动作 struct |
| 依赖 | SIActionPrimaryInteract/SecondaryInteract | 交互动作 struct |
| 依赖 | SCameraSnapshot | 相机快照（鼠标地面坐标） |
| 输出 | SCharacterInputActions | ReadActions() 聚合输出 |

## 内部状态

```csharp
// 输入动作缓存（每帧由 EventDispatcher 事件更新）
private SIActionMove moveAction, lastMoveAction;
private SIActionLook lookAction;
private SIActionCrouch crouchAction;
private SIActionProne proneAction;
private SIActionWalk walkAction;
private SIActionRun runAction;
private SIActionSprint sprintAction;
private SIActionJump jumpAction;
private SIActionStand standAction;
private SIActionPrimaryInteract primaryInteractAction;
private SIActionSecondaryInteract secondaryInteractAction;

// 相机
private SCameraSnapshot cameraControl;
private bool hasCameraControl;
private Vector3 mouseGroundPosition;
private bool hasMouseGround;
```

## 方法

### CharacterEventReceiver()
```csharp
internal CharacterEventReceiver(Game.Character.Components.CharacterActor owner)
```
- **用途**: 构造，注册所有输入类型和相机的订阅
- **调用者**: `CharacterActor.Awake()`
- **备注**: MoveAction 的订阅被注释掉（WASD 移动已禁用 — Phase 4 A* 驱动）

### Reset()
```csharp
internal void Reset()
```
- **用途**: 重置所有输入和相机状态到默认
- **调用者**: `CharacterActor.OnDisable()`

### ReadActions()
```csharp
internal void ReadActions(out SCharacterInputActions actions)
```
- **用途**: 聚合所有输入到 SCharacterInputActions，清空单帧信号
- **调用者**: `CharacterActor.Update()`
- **备注**: 各按钮的 IsRequested/IsReleased 为单帧信号，读取后清空

### ReadPrimaryInteract() / ReadSecondaryInteract()
```csharp
internal bool ReadPrimaryInteract(out SIActionPrimaryInteract action)
internal bool ReadSecondaryInteract(out SIActionSecondaryInteract action)
```
- **用途**: 读取交互按钮状态
- **调用者**: (预留) 外部系统

### ReadCameraControl() / ReadMouseGroundPosition()
```csharp
internal bool ReadCameraControl(out SCameraSnapshot control)
internal bool ReadMouseGroundPosition(out Vector3 worldPosition)
```
- **用途**: 读取相机快照 / 鼠标地面坐标
- **调用者**: `CharacterActor.Update()` (ReadMouseGroundPosition)

### Subscribe() / Unsubscribe()
```csharp
internal void Subscribe()
internal void Unsubscribe()
```
- **用途**: 批量订阅/取消订阅所有输入事件
- **调用者**: CharacterActor.OnEnable/OnDisable

### RegisterCamera()
```csharp
private void RegisterCamera()
```
- **用途**: 注册 SCameraSnapshot 订阅
- **调用者**: 构造

### HandleCameraSnapshot()
```csharp
private void HandleCameraSnapshot(SCameraSnapshot snapshot, MetaStruct meta)
```
- **用途**: 更新相机数据（仅玩家角色接收）
- **调用者**: EventDispatcher

### Register\<TPayload\>()
```csharp
private void Register<TPayload>() where TPayload : struct
```
- **用途**: 泛型注册输入事件订阅
- **调用者**: 构造

### PutAction\<TPayload\>()
```csharp
private void PutAction<TPayload>(TPayload payload) where TPayload : struct
```
- **用途**: 延迟类型匹配 — 将泛型 payload 通过 typeof() 分支分发到对应字段
- **调用者**: Register 中注册的事件处理委托
- **备注**: 使用 `typeof()` 分支兼容 Unity IL2CPP

### LogInteract()
```csharp
private static void LogInteract(string name, SButtonInputState state)
```
- **用途**: 调试日志 — 打印交互按钮按下/释放
- **调用者**: PutAction

### TryResolveDispatcher()
```csharp
private static bool TryResolveDispatcher(out EventDispatcherService dispatcher)
```
- **用途**: 从 GameContext 解析 EventDispatcherService
- **调用者**: Subscribe()

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| WASD 移动恢复 — Phase 4 A* Pathfinding 接入时重新启用 MoveAction 订阅 | 待做 | 代码 TODO |
| 交互按钮（PrimaryInteract/SecondaryInteract）实际使用 | 待做 | 代码预留 |
