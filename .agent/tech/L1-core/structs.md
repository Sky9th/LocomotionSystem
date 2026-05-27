# Core Structs · 数据结构

> `Core/Structs/` — 所有核心模块的 Immutable Struct 定义

## 调用链

```
被谁调:
  各 Service                       → new Struct() 构造 + PublishState() 发布
  外部系统                         → GameContext.TryGetSnapshot<T>() 读取
  EventDispatcherService           → Publish() 中自动附带 MetaStruct

调谁:
  (纯数据结构，不调用任何模块)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 所有 Service | 发布/订阅的 Payload 类型 |
| 被依赖 | 所有外部系统 | 通过 GameContext 读取状态 |
| 依赖 | — | 纯数据，无依赖 |

## 设计原则

- **Immutable** — 所有字段 `{ get; }` 只读，构造时完整初始化
- **Default 静态属性** — 每个 Struct 提供 `Default` 工厂
- **无行为** — Struct 不包含业务逻辑，只存数据
- **不内嵌 MetaStruct** — 元数据由 EventDispatcher 在 Publish() 时统一生成

### Struct 前缀约定

| 前缀 | 用途 | 示例 |
|------|------|------|
| `S` | 全局快照（GameContext 存储/EventDispatcher 发布） | `SGameState`、`SCameraSnapshot`、`SPlayer` |
| `SIAction` | 输入动作（EventDispatcher 发布） | `SIActionMove`、`SIActionLook`、`SIActionJump` |
| `SCharacter` | 角色系统内部 struct | `SCharacterMotor`、`SCharacterKinematic`、`SCharacterInputActions` |

> 规则源自 `9809b78` — 将 `STimeScaleIAction` 等不统一的命名改为 `SIActionWorldSpeed`。

---

## MetaStruct

> `Core/Structs/MetaStruct.cs`

```csharp
struct MetaStruct {
    float Timestamp;       // Time.time — 事件产生的时间戳
    uint FrameIndex;       // Time.frameCount — 事件产生的帧号
    bool IsValid;          // Timestamp >= 0f
}
```

**用途**: 每次 `Dispatcher.Publish()` 自动附带的元数据。用于调试时序、去重、性能分析。

**工厂**:
- `MetaStruct(float timestamp, uint frameIndex)` — 手动构造
- `Publish()` 中自动 `new MetaStruct { Timestamp = Time.time, FrameIndex = Time.frameCount }`

---

## SCharacter

> `Core/Structs/Contexts/SCharacter.cs`

```csharp
struct SCharacter {
    int InstanceID;        // GameObject.GetInstanceID()
    Vector3 Position;      // World position
    Quaternion Rotation;   // World rotation
}
```

**用途**: 基础角色标识快照，被 `SPlayer` 内嵌使用。

**工厂**: `SCharacter.Default`、`SCharacter(int id, Vector3 pos, Quaternion rot)`

---

## SPlayer

> `Core/Structs/Contexts/SPlayer.cs`

```csharp
struct SPlayer {
    SCharacter Character;    // 基础角色快照
    bool IsLocalPlayer;      // 是否本地玩家
}
```

**用途**: 每帧由 `PlayerService.Update()` 发布。`CameraService` 读取来计算 Pivot 位置。

**工厂**:
- `SPlayer(SCharacter character, bool isLocalPlayer)`
- `SPlayer.FromTransform(Transform root, bool isLocalPlayer)` — 从 Transform 构造
- `SPlayer.Default`

---

## SPlayerSpawnedEvent

> `Core/Structs/Contexts/SPlayerSpawnedEvent.cs`

```csharp
struct SPlayerSpawnedEvent {
    Transform PlayerTransform;   // 生成的 Player Transform
    bool IsLocalPlayer;          // 是否本地玩家
}
```

**用途**: Player 生成完成后的一次性事件。`CameraService` 监听此事件开始跟随。

---

## SGameState

> `Core/Structs/Contexts/SGameState.cs`

```csharp
struct SGameState {
    EGameState CurrentState;     // 当前状态
    EGameState PreviousState;    // 上一个状态
    bool HasChanged;             // CurrentState != PreviousState
}
```

**用途**: `GameStateService` 发布。被 `GameService`（触发 Teardown）、`TimeService`（暂停/恢复）监听。

**工厂**: `SGameState(current, previous)`、`SGameState.Default`

---

## SCameraSnapshot

> `Core/Structs/Contexts/SCameraSnapshot.cs`

```csharp
struct SCameraSnapshot {
    Vector3 CameraPosition;          // 渲染摄像机世界位置
    Quaternion CameraRotation;       // 渲染摄像机世界旋转
    Vector3 AnchorPosition;          // 游戏 Anchor 位置
    Quaternion AnchorRotation;       // 游戏 Anchor 旋转（朝向鼠标）
    Vector2 LookDelta;               // 本帧朝向变化 (x=yaw, y=pitch)
    Vector3 MouseGroundPosition;     // 鼠标与地面 Y=0 交点
    bool IsMouseGroundValid;         // 交点是否有效
}
```

**用途**: 每帧由 `CameraService.TickCameraPivot()` 发布。外部系统通过 `TryGetSnapshot<SCameraSnapshot>()` 获取鼠标地面坐标和摄像机状态。

**工厂**:
- 主构造：`SCameraSnapshot(camPos, camRot, anchorPos, anchorRot, lookDelta, mouseGroundPos, isValid)`
- 简化构造：`SCameraSnapshot(camPos, camRot, lookDelta)` — Anchor=Camera, 无鼠标数据

---

## SSceneTransition

> `Core/Scene/SSceneTransition.cs`

```csharp
struct SSceneTransition {
    string SceneName;              // 正在加载/卸载的场景名
    string PreviousSceneName;      // 之前的场景名
    bool IsLoading;                // true=加载中, false=加载完成
}
```

**用途**: `SceneService` 通过 `PublishState()` 发布。UI（LoadingOverlay）监听此状态显示/隐藏加载界面。

## SLoadSceneRequest / SUnloadSceneRequest

- `SLoadSceneRequest` — `{ string SceneName }` — 请求加载场景
- `SUnloadSceneRequest` — `{ string SceneName }` — 请求卸载场景

**发布者**: UI（MainMenu、按钮等）或游戏逻辑。

## SSceneLoadStart / SSceneLoadComplete

- `SSceneLoadStart` — `{ string SceneName }` — 加载开始通知
- `SSceneLoadComplete` — `{ string SceneName, string PreviousSceneName }` — 加载完成通知

**用途**: 加载生命周期通知。`PlayerService`、`TimeService` 订阅。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| SPlayer 扩展为多玩家支持 | 远期 | 多人/NPC 系统 |
| SCameraSnapshot 增加 FOV 字段 | 待做 | CameraService 扩展 |
| SGameState 增加 Loading 独立状态 | 待做 | 当前通过 SSceneTransition 表示加载 |
