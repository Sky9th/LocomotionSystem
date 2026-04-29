# 场景结构梳理

> 更新日期：2025-04-26
> 基于实际代码 + GameManager.prefab + Player.prefab 分析

---

## 目录

1. [GameManager — 根 GameObject](#1-gamemanager--根-gameobject)
2. [Player — 角色 GameObject](#2-player--角色-gameobject)
3. [Anchor — 相机锚点 GameObject](#3-anchor--相机锚点-gameobject)
4. [Scene Setup — 场景额外物件](#4-scene-setup--场景额外物件)
5. [启动时序](#5-启动时序)
6. [串行化引用速查](#6-串行化引用速查)

---

## 1. GameManager — 根 GameObject

**ExecutionOrder: -500**（最先执行）  
**Prefab:** `Assets/Prefabs/GameManager.prefab`

### 层级

```
GameManager (GameManager)
├── InputManager (InputManager)
├── EventDispatcher (EventDispatcher)
├── CameraManager (CameraManager)    ← ExecutionOrder -400
│   ├── Main Camera (Camera + AudioListener + CinemachineBrain)
│   └── Virtual Camera (CinemachineVirtualCamera)
│       └── cm (CinemachineThirdPersonFollow + CinemachinePOVExtension)
├── GameContext (GameContext)
├── GameState (GameState)
├── TimeScaleManager (TimeScaleManager)
├── UIManager (UIManager)
│   └── UIRoot
│       └── Canvas
│           └── OverlayRoot
└── PlayerManager (PlayerManager)
```

### 逐组件配置

| GameObject | Component | 关键字段 | 配置说明 |
|---|---|---|---|
| **GameManager** | `GameManager` | 全部 auto-detect | 无需手动赋值。Awake 中自动发现所有子节点的 `BaseService` |
| **EventDispatcher** | `EventDispatcher` | `inspectorListeners` | 运行时自动填充，Inspector 留空 |
| **GameContext** | `GameContext` | `logDebugInfo` = false | 服务注册表 + 快照缓存。单例 `GameContext.Instance` |
| **GameState** | `GameState` | `initialState` = **MainMenu** | 游戏状态机。`lockCursorWhenPlaying` = true |
| **InputManager** | `InputManager` | `actionHandlers[0..9]` | 10个 `InputActionHandler` SO 引用。在 `OnDispatcherAttached` 阶段初始化 |
| **CameraManager** | `CameraManager` | 见下表 | ExecutionOrder -400 |
| **PlayerManager** | `PlayerManager` | `PlayerPrefab` = Player.prefab | `PlayerStartAnchor` 留空（会找名为 "PlayerStart" 的 GO） |
| **TimeScaleManager** | `TimeScaleManager` | `minScale`=0.2, `maxScale`=1.0 | 监听慢动作/恢复输入 |
| **UIManager** | `UIManager` | 见下表 | 管理 Screen/Overlay 生命周期 |

### CameraManager 配置详情

| 字段 | 类型 | 值 | 说明 |
|---|---|---|---|
| `cameraBrain` | `CinemachineBrain` | Main Camera 上的组件 | 可 auto-locate |
| `defaultVirtualCamera` | `CinemachineVirtualCamera` | Virtual Camera 子对象 | 可 auto-locate |
| `autoLocateBrain` | bool | true | |
| `autoLocateDefaultVirtualCamera` | bool | true | |
| `verticalOffset` | float | **1.7** | Anchor 高度偏移（眼睛位置） |
| `followPlanarOnly` | bool | true | |
| `maxPitchDegrees` | float (0-90) | **75** | 俯仰限制 |
| `gameProfile` | `GameProfile` | 一个 SO 资产 | `cameraLookRotationSpeed` = 1.0 |

### UIManager 配置详情

| 字段 | 类型 | 说明 |
|---|---|---|
| `uiRoot` | `Transform` | UIRoot 子对象 |
| `screensRoot` | `Transform` | Screen 实例化父节点 |
| `overlaysRoot` | `Transform` | Overlay 实例化父节点 |
| `screenConfig` | `UIScreenConfig` | SO: 注册所有 Screen prefab |
| `overlayConfig` | `UIOverlayConfig` | SO: 注册所有 Overlay prefab |

### Main Camera 子对象

| Component | 说明 |
|---|---|
| `Camera` | |
| `AudioListener` | |
| `CinemachineBrain` | Cinemachine 与 Unity 的桥接 |

### Virtual Camera 子对象

| Component | 说明 |
|---|---|
| `CinemachineVirtualCamera` | Follow=Anchor, LookAt=Anchor |
| `CinemachineThirdPersonFollow` | Damping=(1,0.2,1), ShoulderOffset=(0,0,0), VerticalArmLength=0.4, CameraDistance=3.67, CameraRadius=0.2 |
| `CinemachinePOVExtension` | Damping=0 |

---

## 2. Player — 角色 GameObject

**Prefab:** `Assets/Prefabs/Player.prefab`  
**Tag:** Player  
**Layer:** 6

### 层级

```
Player (Tag=Player, Layer=6)
├── [Component] Character
├── [Component] Rigidbody
├── [Component] CapsuleCollider
└── Model (嵌套 Animancer Humanoid prefab)
    ├── [Component] LocomotionAgent
    ├── [Component] CharacterAnimationController
    ├── [Component] LocomotionAnimancerPresenter
    ├── [Component] Animator
    ├── [Component] NamedAnimancerComponent
    └── ... (骨骼网格层级)
```

### 逐组件配置

#### Player (根节点)

| Component | 关键字段 | 值 / 说明 |
|---|---|---|
| **Character** | `characterAnimation` | 指向 Model 上的 `CharacterAnimationController`（可 auto-detect） |
| | `locomotion` | 指向 Model 上的 `LocomotionAgent` |
| | `locomotionPresenter` | 指向 Model 上的 `LocomotionAnimancerPresenter` |
| **Rigidbody** | `constraints` | FreezePositionX \| FreezePositionZ \| FreezeRotationX \| FreezeRotationY \| FreezeRotationZ |
| **CapsuleCollider** | `radius` | 0.2 |
| | `height` | 1.8 |
| | `center` | (0, 0.9, 0) |

#### Model 子节点

| Component | 字段 | 值 / 说明 | 自动解析 |
|---|---|---|---|
| **LocomotionAgent** | `modelRoot` | Model Transform | ✓ `transform.Find("Model")` |
| | `locomotionProfile` | `LocomotionProfile` SO | ✗ 必须手动赋值 |
| | `isPlayer` | true（默认 false） | ✗ 关键：决定是否接收相机 |
| | `animancerPresenter` | 指向同 GO 的 `LocomotionAnimancerPresenter` | ✓ `GetComponentInChildren<>` |
| | `autoSubscribeInput` | true | |
| **CharacterAnimationController** | `animancer` | 指向同 GO 的 `NamedAnimancerComponent` | ✓ `GetComponentInChildren<>` |
| | `animator` | 指向同 GO 的 `Animator` | ✓ `GetComponentInChildren<>` |
| | `upperBodyMask` | `AvatarMask` | ✗ 必须手动赋值 |
| | `additiveMask` | `AvatarMask` | ✗ |
| | `facialMask` | `AvatarMask` | ✗ |
| | `headMask` | `AvatarMask` | ✗ |
| | `footMask` | `AvatarMask` | ✗ |
| **LocomotionAnimancerPresenter** | `agent` | 指向同父节点 `LocomotionAgent` | ✓ `GetComponentInParent<>` |
| | `animancerStringProfile` | `LocomotionAliasProfile` SO | ✗ 必须手动赋值 |
| | `animationProfile` | `LocomotionAnimationProfile` SO | ✗ 必须手动赋值 |
| | `animator` | 指向同 GO 的 `Animator` | ✓ `GetComponent<>` |
| | `forwardRootMotion` | true | |
| | `applyRootMotionPlanarPositionOnly` | true | **关键 Bug-2 相关** |
| **Animator** | `Update Mode` | Animate Physics | |
| | `Controller` | 留空 (Animancer 管理) | |
| | `Apply Root Motion` | true | |
| **NamedAnimancerComponent** | | | Animancer 核心组件 |

### 必须手动赋值的 SO 资产清单

| GO | Component | 字段 | 资产类型 |
|---|---|---|---|
| Model | LocomotionAgent | `locomotionProfile` | `LocomotionProfile` |
| Model | LocomotionAnimancerPresenter | `animancerStringProfile` | `LocomotionAliasProfile` |
| Model | LocomotionAnimancerPresenter | `animationProfile` | `LocomotionAnimationProfile` |
| Model | CharacterAnimationController | `upperBodyMask` | `AvatarMask` |
| Model | CharacterAnimationController | `additiveMask` | `AvatarMask` |
| Model | CharacterAnimationController | `facialMask` | `AvatarMask` |
| Model | CharacterAnimationController | `headMask` | `AvatarMask` |
| Model | CharacterAnimationController | `footMask` | `AvatarMask` |

### 运行时自动创建的组件

以下组件为纯 C# 类，运行时由代码构造，不在 Inspector 中显示：

| 类 | 创建位置 | 创建时机 |
|---|---|---|
| `LocomotionMotor` | `LocomotionAgent.OnEnable()` | Agent 启用时 |
| `LocomotionCoordinatorHuman` | `LocomotionAgent.OnEnable()` | Agent 启用时 |
| `LocomotionInputModule` | `LocomotionAgent.OnEnable()` | Agent 启用时 |
| `LocomotionDriver` | `Character.Start()` | Character Start 时 |
| `TraversalDriver` | `Character.Start()` | Character Start 时 |
| `DriverArbiter` (×4) | `CharacterAnimationController.Awake()` | Controller Awake 时 |
| `LocomotionAnimationController` | `LocomotionDriver.EnsureInitialized()` | 首次 Driver Update 时 |

---

## 3. Anchor — 相机锚点 GameObject

### 层级

```
Anchor (空 GameObject)
  （无特殊组件，仅 Transform）
```

### 说明

| 项目 | 详情 |
|---|---|
| GameObject 名称 | **"Anchor"**（常量 `CommonConstants.FollowAnchorName`） |
| 父节点 | 场景根（独立于 GameManager / Player） |
| 组件 | 仅 `Transform` |
| 查找方式 | `CameraManager.OnServicesReady()` → `GameObject.Find("Anchor")` |
| 用途 | `CinemachineVirtualCamera.Follow` 和 `.LookAt` 目标 |
| 位置更新 | `CameraManager.Update()` → `TickLocalPlayerAnchor()` → 设置 position/rotation |

### 运行时行为

```
每帧:
  anchor.position = lastLocomotionPosition + Vector3.up * verticalOffset(1.7)
  anchor.rotation = 基于 look delta 累积的 pitch/yaw
    - yaw 无限制
    - pitch 限制在 [-maxPitchDegrees, +maxPitchDegrees] = [-75°, +75°]
```

---

## 4. Scene Setup — 场景额外物件

### 必须物件

| 名称 | 类型 | 说明 |
|---|---|---|
| **GameManager** | Prefab 实例 | 拖入 `GameManager.prefab` |
| **Anchor** | 空 GameObject | 相机追踪目标，见第3节 |
| **PlayerStart** | 空 GameObject | `PlayerManager.OnServicesReady()` 中 `GameObject.Find("PlayerStart")` 获取初始位置 |

### 可选物件

| 名称 | 类型 | 说明 |
|---|---|---|
| `LocomotionDebugOverlay` | Prefab 实例 | 实时运动状态 UI 覆盖层 |

### Ground / Collision

- 场景必须有标记为 `groundLayerMask` 的地面碰撞体
- `LocomotionProfile.groundLayerMask` 默认为 Everything (~0)
- 角色 `Rigidbody` 使用重力，但 Y 轴被 Freeze 管理

---

## 5. 启动时序

```
Execution Order  -500      0          (default)     (animation)
                    │       │             │              │
GameManager.Awake() │       │             │              │
  → Bootstrap (4阶段)        │             │              │
                    │       │             │              │
CameraManager       │       │             │              │
  .Update() [-400] ─┤       │             │              │
                    │       │             │              │
                    │   LocomotionAgent.Update()         │
                    │     → Simulate()                   │
                    │     → PushSnapshot()               │
                    │       → GameContext.UpdateSnapshot │
                    │       → EventDispatcher.Publish    │
                    │                    │               │
                    │   CharacterAnimationController     │
                    │     .Update()                      │
                    │     → FullBodyArbiter.Update()     │
                    │       → LocomotionDriver.Update()  │
                    │       → TraversalDriver.BuildReq() │
                    │                    │               │
                    │                    │   Animator Update
                    │                    │     → OnAnimatorMove()
                    │                    │       (Root Motion)
```

### Bootstrap 4 阶段

| 阶段 | 方法 | 每个 Service |
|---|---|---|
| Phase 1: Register | `service.Register(context)` | `OnRegister(context)` |
| Phase 2: AttachDispatcher | `service.AttachDispatcher(dispatcher)` | `OnDispatcherAttached()` |
| Phase 3: ActivateSubscriptions | `service.ActivateSubscriptions()` | `OnSubscriptionsActivated()` |
| Phase 4: NotifyInitialized | `service.NotifyInitialized()` | `OnServicesReady()` |

### 各 Service 的订阅关系

| Service | 订阅的事件 | 在哪个阶段 |
|---|---|---|
| **CameraManager** | `SLocomotion`, `SLookIAction` | Phase 3 |
| **GameState** | `SUIEscapeIAction` | Phase 3 |
| **InputManager** | `SGameState` | Phase 3 |
| **TimeScaleManager** | `STimeScaleIAction` | Phase 3 |

---

## 6. 串行化引用速查

### 必须手动拖入的引用

```
GameManager (根):
  └─ PlayerManager.PlayerPrefab        → Player.prefab
  └─ InputManager.actionHandlers[]     → 10× InputActionHandler SO
  └─ CameraManager.gameProfile         → GameProfile SO
  └─ UIManager.screenConfig            → UIScreenConfig SO
  └─ UIManager.overlayConfig           → UIOverlayConfig SO
  └─ CameraManager.defaultVirtualCamera→ Virtual Camera (子对象, 可 auto-locate)
  └─ CameraManager.cameraBrain         → Main Camera 上的 CinemachineBrain (可 auto-locate)

Player/Model:
  └─ LocomotionAgent.locomotionProfile         → LocomotionProfile SO
  └─ LocomotionAgent.isPlayer                  → true
  └─ LocomotionAnimancerPresenter.animancerStringProfile → LocomotionAliasProfile SO
  └─ LocomotionAnimancerPresenter.animationProfile       → LocomotionAnimationProfile SO
  └─ CharacterAnimationController.upperBodyMask → AvatarMask
  └─ CharacterAnimationController.additiveMask  → AvatarMask
  └─ CharacterAnimationController.facialMask    → AvatarMask
  └─ CharacterAnimationController.headMask      → AvatarMask
  └─ CharacterAnimationController.footMask      → AvatarMask
  └─ Character.characterAnimation    → CharacterAnimationController (可 auto-detect)
  └─ Character.locomotion            → LocomotionAgent (可 auto-detect)
  └─ Character.locomotionPresenter   → LocomotionAnimancerPresenter (可 auto-detect)
```

### 自动解析的引用（无需手动赋值）

| 目标 | 解析方式 | 解析时机 |
|---|---|---|
| `LocomotionAgent.animancerPresenter` | `GetComponentInChildren<LocomotionAnimancerPresenter>()` | Awake |
| `LocomotionAgent.modelRoot` | `transform.Find("Model")` | Awake |
| `CharacterAnimationController.animancer` | `GetComponentInChildren<NamedAnimancerComponent>()` | Awake |
| `CharacterAnimationController.animator` | `GetComponentInChildren<Animator>()` | Awake |
| `LocomotionAnimancerPresenter.agent` | `GetComponentInParent<LocomotionAgent>()` | Start |
| `LocomotionAnimancerPresenter.animator` | `GetComponent<Animator>()` | Start |
| `Character.characterAnimation` | `GetComponentInChildren<CharacterAnimationController>()` | Awake |
| `Character.locomotion` | `GetComponentInChildren<LocomotionAgent>()` | Awake |
| `Character.locomotionPresenter` | `GetComponentInChildren<LocomotionAnimancerPresenter>()` | Awake |
| `PlayerManager.PlayerStartAnchor` | `GameObject.Find("PlayerStart")` | OnServicesReady |
| `CameraManager.localPlayerAnchor` | `GameObject.Find("Anchor")` | OnServicesReady |
| `CameraManager.cameraBrain` | `Camera.main` 或 `FindObjectOfType` | OnRegister |
| `CameraManager.defaultVirtualCamera` | `FindObjectOfType` | OnRegister |
