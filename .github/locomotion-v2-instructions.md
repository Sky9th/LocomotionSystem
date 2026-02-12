# Locomotion v2 子系统说明（新命名空间版本）

> 本文档仅约束「新」Locomotion 实现，不修改也不废弃现有 `Assets/Scripts/Locomotion` 目录及其行为。
> 旧实现仍按原有 `locomotion-instructions.md` 执行；新实现将使用独立命名空间与目录，以便并行迭代与逐步迁移。

## 总体目标
- 在不影响旧版 Locomotion 的前提下，基于全新结构重写角色移动系统。
- 以 `Agent` 为唯一入口，严格划分 **输入(Input)**、**逻辑(Logic)**、**计算(Computation)**、**动画(Animation)** 四个模块。
- 保持「输入 / 物理 / 表现」分离：新 Locomotion 只负责消费标准化 IAction 与推进物理，其它系统只读快照和事件。

## 目录与命名空间约定

### 目录结构（新）
新实现全部放在独立目录中，示例：

- `Assets/Scripts/LocomotionV2/`
  - `Agent/`
  - `Input/`
  - `Logic/`
  - `Computation/`
  - `Animation/`
  - `Shared/`（通用结构体、枚举、配置）

> 实际目录名可以调整，但需保持「物理目录结构」与「命名空间」一一对应，避免与旧版类名冲突。

### 命名空间

新 Locomotion 使用统一命名空间前缀：

- 根命名空间：`Game.LocomotionV2`
- 示例：
  - Agent：`Game.LocomotionV2.Agent`
  - Input：`Game.LocomotionV2.Input`
  - Logic：`Game.LocomotionV2.Logic`
  - Computation：`Game.LocomotionV2.Computation`
  - Animation：`Game.LocomotionV2.Animation`
  - Shared：`Game.LocomotionV2.Shared`

所有新类、结构体、枚举均归属于上述命名空间层级内，禁止直接放在全局命名空间，避免与旧版 `LocomotionAgent` 等类型混淆。

## 顶层角色：LocomotionAgent v2

### 职责

- 挂载在角色（玩家或 AI）上，作为 **唯一入口** 和「编排者」。
- 负责：
  - 与LocomotionManager进行注册交互
  - 聚合输入模块产出的「期望运动指令」（例如期望移动方向、强度、姿态/步态请求）。
  - 调用逻辑模块更新 Posture / Gait / Condition 等离散状态。
  - 调用计算模块生成连续的运动数据（速度、转身角度、地面接触信息等）。
  - 组装并推送 `SPlayerLocomotionV2` 快照给外部（包括动画模块和其他系统）。
- 不直接执行任何动画播放逻辑，也不直接操作 Animator / Animancer；只输出可观测的数据。

### 生命周期（建议）

- `Awake`：解析依赖（配置 Profile、必要组件）、初始化各模块实例。
- `OnEnable`：订阅输入模块、重置内部状态。
- `Update`：
  1. 收集本帧输入（从 Input 缓存中读取标准 IAction）。
  2. 驱动逻辑模块更新姿态/步态/条件等状态机。
  3. 调用计算模块完成本帧运动数值计算。
  4. 生成并缓存 `SPlayerLocomotionV2` 快照，推送给外部监听者。
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
- 命名空间：`Game.LocomotionV2.Input`

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
  - `void BufferMoveAction(SPlayerMoveActionV2 action)`
  - `void BufferLookAction(SPlayerLookActionV2 action)`
- 不能访问或修改 Agent 的内部状态机字段和物理数据。

### 2. 逻辑模块（Logic Module）

**目的：**
- 管理角色的离散状态：
  - 高层 Locomotion 状态（如 Grounded / Airborne / Landing）。
  - 姿态（Posture）：Standing / Crouching / Prone 等。
  - 步态（Gait）：Idle / Walk / Run / Sprint / Crawl 等。
  - 条件（Condition）：Normal / InjuredLight / InjuredHeavy 等。
- 根据输入、当前地面状态、体力等信息，确定状态切换，不直接计算速度向量。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Logic/`
- 命名空间：`Game.LocomotionV2.Logic`

**核心元素：**
- 枚举（优先复用已有定义）：
  - `ELocomotionState`
  - `EPostureState`
  - `EMovementGait`
  - `ELocomotionCondition`

- 状态机类：
  - `LocomotionStateMachine`：管理高层 Grounded/Airborne 等状态。
  - `PostureStateMachine`：管理 Standing/Crouching/Prone。
  - `GaitStateMachine`：管理 Idle/Walk/Run/Sprint/Crawl。
  - `ConditionStateMachine`：管理 Normal/Injured 等修饰状态。

**数据流：**
- 输入：
  - 上一帧离散状态；
  - 输入模块提供的 IAction（移动方向/强度、切换姿态按钮等）；
  - 计算模块或 Agent 提供的辅助信息（是否贴地、速度大小等）。
- 输出：
  - 新一帧的姿态/步态/条件/高层状态；
  - 状态切换事件（可选），供其他系统响应（如播放特效、音效）。

### 3. 计算模块（Computation Module）

**目的：**
- 纯数值计算：基于输入和逻辑状态，计算角色的连续运动数据，不直接决定「角色是什么姿态」。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Computation/`
- 命名空间：`Game.LocomotionV2.Computation`

**核心元素：**
- `LocomotionKinematics`：
  - 输入：期望移动方向、当前姿态/步态、配置 Profile、上一帧速度等。
  - 输出：本帧线速度、角速度、加速度、阻尼等。

- `GroundDetection`：
  - 职责：执行 Raycast/Spherecast 获取地面法线、坡度角、材质标签。
  - 输出：`SGroundContactV2` 结构，包含是否贴地、地面法线、坡度等。

- `TurnCalculator`：
  - 职责：基于 Model 朝向和 Follow 水平向量，计算转身角度、是否需要原地转身、插值速度等。

**原则：**
- 尽量保持无状态或轻状态，便于复用和单元测试；
- 不直接访问 MonoBehaviour 或 Unity 组件（如 Animator），一切依赖通过参数传入。

### 4. 动画模块（Animation Module）

**目的：**
- 只读 `SPlayerLocomotionV2` 等快照 + 配置，驱动动画系统（如 Animancer），不参与逻辑决策和物理计算。

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Animation/`
- 命名空间：`Game.LocomotionV2.Animation`

**核心元素：**
- `LocomotionAnimancerAdapterV2`（示例命名）：
  - MonoBehaviour，挂在与 Agent 同一角色上；
  - 持有对 Agent 的引用；
  - 在 `Update`/`LateUpdate` 中读取最新快照：
    - 平面速度大小 / 方向；
    - 是否转身、转身角度；
    - 姿态/步态/条件枚举（来自 Logic 模块）；
  - 将这些数据映射到 Animancer 状态机参数与混合比例。

- 子状态类（可选）：
  - IdleStateV2 / MoveStateV2 / TurnInPlaceStateV2 等，仅属于动画模块，用于封装表现层状态逻辑。

**原则：**
- 动画模块只消费数据，最多通过事件向外部报告「动画完成」之类的结果；
- 若需要影响逻辑（例如转身动画结束后通知可以恢复移动状态），通过 Agent 暴露的显式 API 或事件，而不是直接修改内部字段。

### 5. Shared 模块

**位置 & 命名空间：**
- 目录：`Assets/Scripts/LocomotionV2/Shared/`
- 命名空间：`Game.LocomotionV2.Shared`

**内容：**
- 结构体（若新旧语义基本一致则直接复用已有定义）：
  - `SPlayerLocomotion`：
    - 若现有字段已覆盖 v2 需求，则直接使用现有结构；
    - 若需新增字段（如额外调试信息），优先通过扩展方法或旁路数据结构解决，避免破坏旧用例；
    - 仅在确实需要新增核心语义且无法兼容时，才考虑增补字段。
  - `SGroundContact`：
    - 继续复用现有地面接触信息定义，必要时通过配置或辅助结构扩展行为。

- 配置：`LocomotionConfigProfileV2`（ScriptableObject），提供：
  - 各姿态、步态的基础移动速度、加速度；
  - 转身进入/完成阈值；
  - 头部旋转限制和插值速度；
  - 地面检测参数（射线长度、球半径、坡度容差等）。

## 与旧版 Locomotion 的关系

- 旧版目录 `Assets/Scripts/Locomotion/` 保持不变，可以继续在项目中使用；
- 新版目录 `Assets/Scripts/LocomotionV2/` 与之完全并行：
  - 不复用旧版类名（例如不会再定义无命名空间的 `LocomotionAgent`）；
  - 通过命名空间和文件夹物理隔离，避免冲突。
- 后续可逐步：
  - 在新场景或新角色上试用 v2；
  - 稳定后再考虑将旧逻辑迁移或替换为新版结构。

## 建议的实现顺序（v2）

1. **Shared 基础结构**
  - 复用或轻量扩展现有的 `SPlayerLocomotion`、`SGroundContact`、枚举（`ELocomotionState` 等），仅在语义差异较大时再新增类型。
2. **Agent 骨架**
   - 在 `Agent` 目录实现 `LocomotionAgentV2` 的生命周期与 `Update` 流程（仅调用占位模块）。
3. **输入模块最小实现**
   - 实现 PlayerMoveInputHandler，支持基础移动输入；
   - Agent 内部提供 `BufferMoveAction` 接口并在 Simulation 里读取。
4. **计算模块最小实现（Idle / Walk）**
   - 基于 Follow 前向与输入向量，计算简单的平面速度（无跳跃、无坡度处理）；
   - 将结果写入 `SPlayerLocomotionV2` 并供外部读取。
5. **逻辑模块最小实现（Idle ↔ Walk）**
   - 根据输入强度、速度大小判断 GroundedIdle / GroundedMoving 与 Walk/Idle 步态切换。
6. **动画模块基础适配**
   - 实现 `LocomotionAnimancerAdapterV2`，只驱动 Idle/Walk 状态切换与基本 2D 混合。
7. **逐步扩展**
   - 引入 Jump/Airborne、Sprint、Crouch 等高级逻辑；
   - 扩展配置 Profile；
   - 必要时再考虑接入 `LocomotionManager` 或 GameContext。

---

本文件只作为 v2 实现的设计约束与参考，后续如在实现中发现更合适的拆分方式，可以通过增量修改本说明与代码一同演进。