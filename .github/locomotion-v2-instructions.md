# Locomotion v2 子系统说明（新命名空间版本）

> 本文档仅约束「新」Locomotion 实现，不修改也不废弃现有 `Assets/Scripts/Locomotion` 目录及其行为。
> 旧实现仍按原有 `locomotion-instructions.md` 执行；新实现将使用独立命名空间与目录，以便并行迭代与逐步迁移。

## 总体目标
- 在不影响旧版 Locomotion 的前提下，基于全新结构重写角色移动系统。
- 以 `Agent` 为唯一入口，严格划分 **输入(Input)**、**状态(State)**、**计算(Computation)**、**动画(Animation)** 四个模块。
- 保持「输入 / 物理 / 表现」分离：新 Locomotion 只负责消费标准化 IAction 与推进物理，其它系统只读快照和事件。

## 目录与命名空间约定

### 目录结构（新）
新实现全部放在独立目录中，示例：

- `Assets/Scripts/LocomotionV2/`
  - `Agent/`
  - `Input/`
  - `State/`
    - `Core/`
    - `Layers/`
    - `Controllers/`
  - `Computation/`
  - `Animation/`

> 实际目录名可以调整，但需保持「物理目录结构」与「命名空间」一一对应，避免与旧版类名冲突。

### 命名空间

新 Locomotion 使用统一命名空间前缀：

- 根命名空间：`Game.Locomotion`
- 示例：
  - Agent：`Game.Locomotion.Agent`
  - Input：`Game.Locomotion.Input`
  - State：`Game.Locomotion.State`
  - Computation：`Game.Locomotion.Computation`
  - Animation：`Game.Locomotion.Animation`
  - Shared：`Game.Locomotion.Shared`

所有新类、结构体、枚举均归属于上述命名空间层级内，禁止直接放在全局命名空间，避免与旧版 `LocomotionAgent` 等类型混淆。

## 顶层角色：LocomotionAgent v2

### 职责

- 挂载在角色（玩家或 AI）上，作为 **唯一入口** 和「编排者」。
- 负责：
  - 与LocomotionManager进行注册交互
  - 聚合输入模块产出的「期望运动指令」（例如期望移动方向、强度、姿态/步态请求）。
  - 调用状态模块更新 Posture / Gait / Condition 等离散状态。
  - 调用计算模块生成连续的运动数据（速度、转身角度、地面接触信息等）。
  - 组装并推送 `SPlayerLocomotion` 快照给外部（包括动画模块和其他系统）。
- 不直接执行任何动画播放逻辑，也不直接操作 Animator / Animancer；只输出可观测的数据。

### 生命周期（建议）

- `Awake`：解析依赖（配置 Profile、必要组件）、初始化各模块实例。
- `OnEnable`：订阅输入模块、重置内部状态。
- `Update`：
  1. 收集本帧输入（从 Input 缓存中读取标准 IAction）。
  2. 驱动状态模块更新姿态/步态/条件等状态机。
  3. 调用计算模块完成本帧运动数值计算。
  4. 生成并缓存 `SPlayerLocomotion` 快照，推送给外部监听者。
- `LateUpdate`：
  - 执行需要在核心模拟之后的对齐操作（例如 Model 与 Follow 的朝向对齐）。
- `OnDisable`：
  - 退订输入，清空缓存和内部状态。

## 模块划分

### 1. 输入模块（Input Module）

**目的：**
- 把各种来源的输入统一标准化为不可变的 IAction 结构，并缓存在 Agent 内部；
- 不直接修改 Agent 的状态机或物理数据。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Input/`
- 命名空间：`Game.Locomotion.Input`

**核心元素：**
- `InputActionHandler<TAction>`：
  - 职责：
    - 订阅游戏全局或本地的输入事件系统（如 `EventDispatcher`、新 Input System、AI 行为树回调）；
    - 将收到的输入转为 `TAction` 结构，调用 Agent 的 `BufferAction` / `ApplyInput` 接口；
    - 统一管理订阅与退订（`Subscribe` / `Unsubscribe`）。
  - 特征：
    - 不依赖具体数值和状态机，只关心「事件 → 数据结构」。

- 示例具体 Handler：
  - `PlayerMoveInputHandler`：处理玩家移动（摇杆/键盘方向、强度）。
  - `PlayerLookInputHandler`：处理视角或朝向相关输入。
  - 预留：`AIInputHandler`、`ScriptedInputHandler` 等用于 AI 或脚本驱动。

**与 Agent 的边界：**
- 只允许调用 Agent 暴露的输入写入接口，例如：
  - `void BufferMoveAction(SPlayerMoveAction action)`
  - `void BufferLookAction(SPlayerLookAction action)`
- 不能访问或修改 Agent 的内部状态机字段和物理数据。

### 2. 状态模块（State Module）

**目的：**
- 管理角色的所有离散状态维度：
  - 高层 Locomotion 相位（Phase）：如 Grounded / Airborne / Landing 等；
  - 姿态（Posture）：Standing / Crouching / Prone 等；
  - 步态（Gait）：Idle / Walk / Run / Sprint / Crawl 等；
  - 条件（Condition）：Normal / InjuredLight / InjuredHeavy 等；
  - 以及未来可拓展的其他维度（如 WeaponStance、CoverState 等）。
- 根据输入、当前地面状态、体力等信息，驱动各维度状态机演化，但不直接计算连续速度向量。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/State/`
- 命名空间：`Game.Locomotion.State`
  - 核心类型（上下文、状态机、接口等）：`Game.Locomotion.State.Core`
  - 状态层实现（Phase/Gait/Posture/Condition 等）：`Game.Locomotion.State.Layers`
  - 控制器与 Archetype：`Game.Locomotion.State.Controllers`

**核心抽象：**
- 枚举（优先复用已有定义）：
  - `ELocomotionState`：顶点相位（Grounded / Airborne / Landing 等）。
  - `EPostureState`：姿态。
  - `EMovementGait`：步态。
  - `ELocomotionCondition`：状态修饰（受伤等）。

- 统一状态控制接口：
  - `ILocomotionController`：
    - 由 Agent 持有，代表“这一角色当前使用的完整 Locomotion 行为集”（例如 Human、Zombie 等不同 Archetype）。
    - 提供 `UpdateDiscreteState(...)` 等接口，输入标准化上下文，输出一帧离散状态快照。

- 抽象基类：
  - `LocomotionControllerBase`：
    - 实现 `ILocomotionController` 的通用部分，内部持有一个 `LocomotionStateMachine`；
    - 子类通过重写工厂/组装方法，决定使用哪些状态层（Layer）来构建具体的状态机。

- 核心状态机与状态层：
  - `LocomotionStateMachine`：
    - 聚合多个正交状态层，负责根据输入上下文驱动这些层并组合成 `SLocomotionDiscreteState`；
    - 顶点输出始终包含 Phase / Posture / Gait / Condition 四个维度。
  - `ILocomotionStateLayer<TState>`：
    - 统一的状态层接口，每个维度（Phase / Posture / Gait / Condition）各有一个实现；
    - 输入一个只读的 `LocomotionStateContext`，内部维护自身的 `Current` 状态。
  - 典型实现示例（命名非强制，仅作约定）：
    - `LocomotionPhaseStateLayer : ILocomotionStateLayer<ELocomotionState>`
    - `PostureStateLayer : ILocomotionStateLayer<EPostureState>`
    - `GaitStateLayer : ILocomotionStateLayer<EMovementGait>`
    - `ConditionStateLayer : ILocomotionStateLayer<ELocomotionCondition>`

- 上下文结构：
  - `LocomotionStateContext`：
    - 封装状态机与各层所需的只读输入，例如：
      - 当前速度向量、上一帧离散状态；
      - 当前地面接触信息；
      - 本帧移动输入 IAction；
      - 配置 Profile 等。
    - 屏蔽 Agent / MonoBehaviour 细节，使 State 模块保持可测试、可重用。

**多 Archetype 支持：**
- 每一种 Archetype（Human / Zombie / Animal 等）通过继承 `LocomotionControllerBase` 自定义自身的状态机组合：
  - 示例：
    - `HumanLocomotionController : LocomotionControllerBase`
    - `ZombieLocomotionController : LocomotionControllerBase`
- 每个具体 Controller 在其内部决定：
  - 使用哪一套 Phase/Posture/Gait/Condition 状态层；
  - 是否引入额外的自定义状态层（例如僵尸的 Shamble/Charge 状态）。
- Agent 只持有 `ILocomotionController` 引用，并通过配置或工厂（例如 `LocomotionControllerFactory` + Archetype Profile）实例化具体实现，从而可以按角色类型切换不同 Locomotion 规则，而无需改 Agent 代码。

**数据流：**
- 输入：
  - 上一帧离散状态；
  - 输入模块提供的 IAction（移动方向/强度等）；
  - 计算模块或 Agent 提供的辅助信息（是否贴地、速度大小等）；
  - 统一封装为 `LocomotionStateContext`。
- 逻辑：
  - Agent 调用 `ILocomotionController.UpdateDiscreteState(context)`；
  - `LocomotionControllerBase` 内部转交给 `LocomotionStateMachine`；
  - 状态机驱动各状态层的 `Update(context)`，汇总成新的离散状态。
- 输出：
  - 新一帧的姿态 / 步态 / 条件 / 高层 Phase 状态；
  - 组合为 `SLocomotionDiscreteState`，并最终写入 `SPlayerLocomotion` 供动画和其他系统读取。

### 3. 计算模块（Computation Module）

**目的：**
- 纯数值计算：基于输入和逻辑状态，计算角色的连续运动数据，不直接决定「角色是什么姿态」。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Computation/`
- 命名空间：`Game.Locomotion.Computation`

**核心元素：**
- `LocomotionKinematics`：
  - 输入：期望移动方向、当前姿态/步态、配置 Profile、上一帧速度等。
  - 输出：本帧线速度、角速度、加速度、阻尼等。

- `GroundDetection`：
  - 职责：执行 Raycast/Spherecast 获取地面法线、坡度角、材质标签。
  - 输出：`SGroundContact` 结构，包含是否贴地、地面法线、坡度等。

- `TurnCalculator`：
  - 职责：基于 Model 朝向和 Follow 水平向量，计算转身角度、是否需要原地转身、插值速度等。

**原则：**
- 尽量保持无状态或轻状态，便于复用和单元测试；
- 不直接访问 MonoBehaviour 或 Unity 组件（如 Animator），一切依赖通过参数传入。

### 4. 动画模块（Animation Module）
 把各种来源的输入统一标准化为不可变的 IAction 结构；
 将 IAction 映射为"高层控制意图"并通过统一接口写入 Agent；
 不直接修改 Agent 的状态机或物理数据。
- 只读 `SPlayerLocomotion` 等快照 + 配置，驱动 Animancer，不参与逻辑决策和物理计算；
- 支持复杂分层（基础移动、原地转身、空中、上半身、Additive 等），同时保持结构清晰、易于扩展和测试。

**目录 & 命名空间：**
- 目录：
  - `Assets/Scripts/LocomotionV2/Animation/`

- `ILocomotionControlSink`：
  - 位置：`Assets/Scripts/LocomotionV2/Input/ILocomotionControlSink.cs`
  - 命名空间：`Game.Locomotion.Input`
  - 职责：
    - 抽象一套与来源无关的控制接口，例如：
      - `SetMoveInput(Vector2 rawInput, Vector3 worldDirection)`
      - `SetLookInput(Vector2 delta)`
      - `SetCrouch(bool wantCrouch)` / `SetProne(bool wantProne)` / `SetRun(bool wantRun)` / `SetStand(bool wantStand)`
    - 由 `LocomotionAgent` 实现，使“玩家输入 / AI 行为 / 脚本驱动”等都可以通过同一入口施加控制意图；
    - 屏蔽 EventDispatcher、IAction 等细节，Agent 只感知高层意图。
    - `Core/`
    - `Config/`
    - `Presenters/`
    - 将收到的输入转为 `TAction` 结构，并翻译为对 `ILocomotionControlSink` 的调用（例如把 `SPlayerLookIAction` 转成 `SetLookInput` 调用）；
  - 核心控制器与上下文：`Game.Locomotion.Animation.Core`
  - 各动画层实现：`Game.Locomotion.Animation.Layers`
  - 动画参数配置：`Game.Locomotion.Animation.Config`

- 具体实现（当前版本）：
  - `LocomotionInputModule`：
    - 位置：`Assets/Scripts/LocomotionV2/Input/LocomotionInputModule.cs`
    - 命名空间：`Game.Locomotion.Input`
    - 职责：
      - 集中订阅全局 `EventDispatcher` 中与 Locomotion 相关的所有 IAction（如 `SPlayerMoveIAction`、`SPlayerLookIAction` 等）；
      - 将最新 IAction 按类型缓存在内部（`Dictionary<Type, object>`）；
      - 根据当前设计逐步演进为：收到 IAction 时直接调用 `ILocomotionControlSink`，不再由 Agent 主动拉取 IAction；
      - 对外不暴露 EventDispatcher，仅由 Agent 创建和持有该模块实例。
  - MonoBehaviour 桥接层：`Game.Locomotion.Animation.Presenters`

 只允许通过 `ILocomotionControlSink` 传递控制意图，例如：
  - `SetMoveInput` / `SetLookInput` / `SetCrouch` 等；
 不访问或修改 Agent 的内部状态机字段和物理数据；
 IAction 作为输入层的标准 DTO，可以被多个子系统订阅，但 Agent 自身只依赖 `ILocomotionControlSink`。
- 命名空间：`Game.Locomotion.Animation.Presenters`
- 职责：
  - 持有引用：
    - `Game.Locomotion.Agent.LocomotionAgent agent`
    - `NamedAnimancerComponent animancer`
    - `AnimancerStringProfile animancerStringProfile`
    - 本帧的移动/视角/姿态等输入（通常由 Agent 内部根据 `ILocomotionControlSink` 收集的控制状态组装为 IAction，传入状态机）；
    - 配置 Profile 等。
    - `controller = new LocomotionAnimationController(animancer, animancerStringProfile, animationProfile);`
  - 在 `Update` 或 `LateUpdate` 中：
    - 读取最新 `agent.Snapshot`；
    - 调用 `controller.UpdateAnimations(snapshot, deltaTime);`

Presenter 不做任何业务决策，只负责「从 Agent 取快照 → 调用控制器」，是场景与动画逻辑之间的唯一桥接层。

#### 4.2 Core 层（动画控制器与上下文）

**位置：**`Animation/Core/`

- `LocomotionAnimationContext`
  - 命名空间：`Game.Locomotion.Animation.Core`
  - 结构体，封装驱动每一帧动画层所需的信息：
    - `SPlayerLocomotion Snapshot`
    - `float DeltaTime`
    - `NamedAnimancerComponent Animancer`
    - `AnimancerStringProfile Alias`
    - `LocomotionAnimationProfile Profile`
  - 作为所有动画层的统一只读输入，屏蔽 MonoBehaviour 细节，便于单元测试。

- `ILocomotionAnimationLayer`
  - 命名空间：`Game.Locomotion.Animation.Core`
  - 接口示例：
    - `void Update(in LocomotionAnimationContext context);`
  - 每个动画层只关注自己的职责（基础移动 / 转身 / 空中 / 上半身 / Additive 等）。

- `LocomotionAnimationController`
  - 命名空间：`Game.Locomotion.Animation.Core`
  - 职责：
    - 持有：
      - `NamedAnimancerComponent animancer`
      - `AnimancerStringProfile alias`
      - `LocomotionAnimationProfile profile`
      - 一组 `ILocomotionAnimationLayer[] layers`
    - 对外仅暴露：
      - `void UpdateAnimations(SPlayerLocomotion snapshot, float deltaTime);`
    - 在 `UpdateAnimations` 中：
      - 构造 `LocomotionAnimationContext`；
      - 依次调用各动画层的 `Update` 方法；
    - 各 Layer 内再通过 `context.Animancer.TryPlay(context.Alias.xxx)` 与 Transition Library + 命名 alias 对接，不直接引用 AnimationClip。

#### 4.3 Layers 层（动画分层实现）

**位置：**`Animation/Layers/`

典型层次（可按需裁剪或扩展）：

- `BaseLocomotionLayer`
  - 命名空间：`Game.Locomotion.Animation.Layers`
  - 职责：基础移动树（Idle / Walk / Run / Sprint 等）以及不同 Posture 对应的变体；
  - 依据：`snapshot.Gait`、`snapshot.Posture`、`snapshot.Speed` 等；
  - 通过 alias 播放不同循环动画，例如：
    - Standing Idle/Walk/Run/Sprint
    - Crouch Idle/Walk
    - Prone Idle/Crawl 等。

- `TurnInPlaceLayer`
  - 管理原地转身动画；
  - 触发条件：通常为 `snapshot.IsTurning == true` 且 `snapshot.State == ELocomotionState.GroundedIdle`；
  - 根据 `snapshot.TurnAngle` 的正负与绝对值选择左/右、小角度/大角度的转身动画 alias。

- `AirborneLayer`
  - 管理起跳 / 空中 / 落地动画；
  - 依据：`snapshot.State == ELocomotionState.Airborne`、垂直速度等；
  - 可在落地瞬间（从非 Grounded 切回 Grounded）触发 Landing 动画。

- `UpperBodyLayer`（预留）
  - 管理武器持握、瞄准姿态等，只影响上半身（通常配合 AvatarMask 使用）。

- `AdditivePoseLayer`（预留）
  - 管理受击反应、呼吸、抖动等 Additive 动画叠加。

- `HeadLookLayer`（预留）
  - 基于 `snapshot.LookDirection` 进行头部 / 上半身的看向控制，可参考旧版 HeadMask 逻辑。

各 Layer 实现 `ILocomotionAnimationLayer` 接口，由 `LocomotionAnimationController` 统一调度；需要时可以通过配置启用/禁用某些层或替换为 Archetype 专用实现。

#### 4.4 Config 层（动画配置）

**位置：**`Animation/Config/`

- `LocomotionAnimationProfile`
  - 命名空间：`Game.Locomotion.Animation.Config`
  - `ScriptableObject`，作为整套 locomotion 动画行为的参数配置（不直接持有 AnimationClip）；
  - 示例字段：
    - Movement：
      - `float walkSpeedThreshold;`
      - `float runSpeedThreshold;`
      - `float sprintSpeedThreshold;`
    - TurnInPlace：
      - `float minTurnAngle;`
      - `float maxTurnAngle;`
      - `float blendOutAngle;`
    - Airborne：
      - `float landHardVelocity;` 等；
  - 具体动画资源绑定通过 Transition Library + `AnimancerStringProfile` 完成，代码仅使用 alias 字段名。

后续如 TurnInPlace / Airborne 等子系统变得足够复杂，可以再拆分出更细的 Profile，但初期统一收敛在一个 `LocomotionAnimationProfile` 中即可。

## 与旧版 Locomotion 的关系

- 旧版目录 `Assets/Scripts/Locomotion/` 保持不变，可以继续在项目中使用；
- 新版目录 `Assets/Scripts/LocomotionV2/` 与之完全并行：
  - 不复用旧版类名（例如不会再定义无命名空间的 `LocomotionAgent`）；
  - 通过命名空间和文件夹物理隔离，避免冲突。
- 后续可逐步：
  - 在新场景或新角色上试用 v2；
  - 稳定后再考虑将旧逻辑迁移或替换为新版结构。