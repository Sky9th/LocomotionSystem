# UI System – Copilot 指南

本说明文档定义项目内统一的 UI 系统设计，用于承载**正式游戏 UI**（主菜单、HUD、背包等）以及 **Debug UI**（如 Locomotion 调试面板）。它应保持简洁、可扩展，并与 GameManager / GameContext / EventDispatcher 架构一致。

## 核心目标
- 为所有 UI 提供统一的管理入口 `UIService`，由 GameManager 注册和驱动。
- 定义通用的 Screen / Overlay 抽象，避免各功能模块各自管理 UI 生命周期。
- 让游戏 UI 与 Debug UI 共用一套框架：同样的导航、显隐与数据获取逻辑。
- 严格区分“展示层”和“业务层”：UI 只读 GameContext、响应 EventDispatcher，不直接修改业务状态。

## 核心角色

### UIService（运行时入口）
- 类型：`UIService : BaseService`，由 GameManager 在启动阶段注册。
- 职责：
  - 持有 UI 根节点（通常为场景中的主 Canvas 与 EventSystem）。
  - 维护可用的 Screen / Overlay 实例或 Prefab 映射表（按 string id 区分）。
  - 提供简洁 API：
    - `ShowScreen(string screenId, object payload = null)`：切换全屏界面。
    - `ShowOverlay(string overlayId, object payload = null)`：显示叠加层。
    - `HideOverlay(string overlayId)`：隐藏叠加层。
    - `ToggleDebugOverlay(string overlayId)`：用于 Debug 面板的显隐切换。
  - 在 `AttachDispatcher(EventDispatcher dispatcher)` 后统一订阅 UI 相关事件（如弹出提示、伤害飘字等），并将事件转发给具体 Screen/Overlay。
- 生命周期约定：
  - `Register(GameContext)`：
    - 解析并缓存 GameContext 和必要的 UI 资源引用（Canvas、Screen/Overlay 列表等）。
    - 扫描 Canvas（或指定根节点）下所有 UIScreenBase / UIOverlayBase 实例，记录其根 GameObject 的初始激活状态 `activeSelf`，并统一将这些 GameObject 关闭，以避免尚未初始化完成时抢先参与渲染或逻辑。
    - Prefab 形式的 Screen/Overlay 只在配置中登记，不在此阶段实例化。
  - `AttachDispatcher(EventDispatcher dispatcher)`：
    - 缓存 Dispatcher 引用（不做订阅）。
  - `ActivateSubscriptions()`：
    - 订阅用于 UI 的 IAction / 领域事件（例如 Debug UI 的 toggle 输入）。
  - `OnInitialized()`（通过 BaseService.NotifyInitialized 由 GameManager 统一触发）：
    - 对所有已登记的场景内 UIScreenBase / UIOverlayBase，再执行一次性轻量初始化（按需为其注入 GameContext、UIService 引用等）。
    - 根据之前记录的初始激活状态决定默认显示哪些 UI：对于在编辑器中默认激活（`activeSelf == true`）的 Screen/Overlay，通过 `ShowScreen/ShowOverlay` 调用其 OnEnter/OnShow，使其在引导完成后自动显示；其余保持隐藏，等待运行期逻辑显式打开。

### UIScreenBase（全屏界面基类）
- 类型：`UIScreenBase : MonoBehaviour`。
- 用途：主菜单、设置菜单、暂停菜单等**全屏互斥界面**。
- 建议接口：
  - `virtual void OnEnter(object payload)`：当被 UIService 设为当前 Screen 时调用，可根据 payload 初始化内容。
  - `virtual void OnExit()`：当离开当前 Screen 时调用。
  - `virtual void SetVisible(bool visible)`：统一显隐入口（内部可用 `gameObject.SetActive` 或 CanvasGroup）。
- 导航规则：
  - 同一时间仅有一个活跃 Screen，由 UIService 维护当前引用。
  - 第一版实现可以只支持“直接切换当前 Screen”，后续如有需要再扩展简单堆栈（如 Pause → Options → Back）。

### UIOverlayBase（叠加层基类）
- 类型：`UIOverlayBase : MonoBehaviour`。
- 用途：HUD、提示层、Debug 面板等**可叠加界面**。
- 建议接口：
  - `virtual void OnShow(object payload)`：通过 UIService 显示时调用，可用 payload 传递上下文（如目标实体）。
  - `virtual void OnHide()`：被隐藏时调用。
  - `virtual void SetVisible(bool visible)`：统一显隐入口。
- 管理规则：
  - 允许多个 Overlay 同时显示，由 UIService 通过 overlayId 进行查找和管理。
  - Overlay 本身不负责导航逻辑，只专注于展示和与玩家的本地交互（按钮回调等）。

## Debug UI 集成

Debug UI 被视作 UI System 的一个子模块，以 Overlay 形式接入：

- Debug 面板采用 `UIOverlayBase` 的派生类实现，例如：
  - `LocomotionDebugOverlay`：用于展示 `SPlayerLocomotion` 等运动数据。
- Debug 面板的显隐由 UIService 统一控制：
  - 提供 `ToggleDebugOverlay(string overlayId)` API，供 InputManager 或其它服务调用。
  - Overlay 自身不直接监听输入设备事件，避免与 InputManager 职责重叠。
- 输入链路示例：
  - InputManager 定义 Debug UI 切换的 IAction（例如 `SDebugToggleIAction`）。
  - UIService 在 `ActivateSubscriptions()` 中通过 EventDispatcher 订阅该 IAction。
  - 收到 toggle 消息时，调用 `ToggleDebugOverlay("LocomotionDebug")` 等实现显隐切换。

## 数据流与职责边界

- **GameContext 为主数据源**：
  - 各类 Screen / Overlay 在 `Update()` 或定期 Tick 中从 GameContext 读取只读 Struct 快照（如 `SPlayerLocomotion`、`SPlayerStatusContext` 等）。
  - UI 逻辑只依赖这些快照进行展示，不尝试修改或缓存可变业务状态。
- **EventDispatcher 负责事件驱动 UI**：
  - 例如：任务完成、受到伤害、弹出提示等事件通过 EventDispatcher 广播。
  - UIService 在集中订阅后，将事件分发给对应的 Screen/Overlay，减少各 UI 组件直接依赖 Dispatcher 的数量。
- **UI 不直接写业务状态**：
  - 点击按钮后如需触发行为（例如“开始游戏”“退出到主菜单”“使用物品”），应通过 EventDispatcher 发送领域事件，或调用对应 Service 的公开方法（由 GameManager 注册的 Service）。
  - 禁止在 UI 脚本中直接操作 GameContext 内部存储结构（例如直接修改 Struct 或 Service Registry）。

## 资源组织

- **Canvas 与 EventSystem**：
  - 场景中通常放置一个主 Canvas 与 EventSystem，由 UIService 在 Register 阶段通过 SerializeField 或查找方式绑定。
  - 将来如果需要分屏或多 Canvas，可以在 UIService 内部扩展对多个 Canvas 的管理，但第一版只考虑单 Canvas。
- **Prefab 与 ID 映射**：
  - 所有 Screen / Overlay 建议以 Prefab 形式存放在 `Assets/UI/` 下（可按类别分子文件夹）。
  - 第一版可在 UIService 上直接用 `List<UIEntry>`（结构体包含 `string id; UIScreenBase screenPrefab;` 或 `UIOverlayBase overlayPrefab;`）手工配置；
  - UI 数量增多后，可考虑迁移到 ScriptableObject 配置（如 `UIConfig` 资产），在 Register 时加载并建立 `id → prefab` 映射表。
- **实例化策略**：
  - 简化起见，第一版可以在场景中预放常驻 Overlay（如 HUD、LocomotionDebug），由 UIService 引用并控制显隐；
  - 对于较重或使用频率低的 Screen/Overlay，可以在首次 `Show` 时从 prefab 动态实例化，并缓存引用以复用。

## 推荐层级结构

在典型场景中，推荐的 UI GameObject 层级如下：

- GameManager
  - GameContext
  - EventDispatcher
  - InputManager
  - CameraManager
  - LocomotionManager
  - TimeScaleManager
  - UIService（或 UIManager：挂载 UI 入口 Service）
    - UIRoot（可选空节点，仅用于归类）
      - Canvas（Screen Space - Overlay）
      - EventSystem
        - ScreensRoot（空节点，承载全屏 Screen）
          - MainMenuScreen（挂 UIScreenBase 派生类）
          - PauseMenuScreen（挂 UIScreenBase 派生类）
          - InventoryScreen（挂 UIScreenBase 派生类）
        - OverlaysRoot（空节点，承载 Overlay）
          - GameHudOverlay（挂 UIOverlayBase 派生类）
            - HealthBar
            - StaminaBar
            - Hotbar
          - NotificationOverlay（挂 UIOverlayBase 派生类）
          - LocomotionDebugOverlay（挂 UIOverlayBase 派生类，用于调试 SPlayerLocomotion）

每个 Screen/Overlay 内部再根据功能自行组织子节点（如 Background、布局容器、TextMeshPro 文本等），但生命周期与显隐一律由 UIService 统一调度，以保证结构清晰、行为一致。

## 与 Locomotion Debug 的关系

- Locomotion 子系统每帧向 GameContext 写入 `SPlayerLocomotion` 快照。
- `LocomotionDebugOverlay` 作为 UIOverlayBase 派生类：
  - 在初始化时（如 `Awake/OnShow`）通过 GameContext 或 Service resolve 拿到 Locomotion 相关快照访问入口。
  - 在 `Update()` 中轮询 `SPlayerLocomotion`，将位置、速度、当前运动状态、是否在地面等字段映射到 TextMeshPro 文本或其他 UI 元素。
  - 不向外广播事件、不修改运动状态，仅作只读展示。

> 本文档只给出 UI System 的总体约定与边界。具体类名、字段与方法签名在实现阶段可根据需要微调，但应保持：统一通过 `UIService` 管理 UI 生命周期，游戏 UI 与 Debug UI 共用同一套 Screen/Overlay 抽象，并遵守“UI 只读 GameContext + 通过 EventDispatcher 响应事件”的原则。
