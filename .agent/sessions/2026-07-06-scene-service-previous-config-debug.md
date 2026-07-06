# 2026-07-06 — scene-service-previous-config-debug

## Background

SceneService 在处理场景切换请求时，会把当前配置缓存到 previousConfig，再把 _currentConfig 切到新配置并启动 TransitionGate。本次会话的触发点是调试时观察到 _currentConfig 看起来有值，但局部变量 previousConfig 在赋值位置显示为 null，需要判断这是运行时真实丢值，还是调试器观察时机或 Unity Object null 语义导致的误判。

## Changes

### Scene Service Investigation
- 阅读 SceneService 的 HandleSceneRequest，确认 previousConfig 的赋值发生在 _currentConfig 被覆盖之前，且中间没有异步边界
- 阅读 TransitionGate.Begin，确认 previousConfig 作为协程参数被直接传入，并在首个 yield 前用于推导 previousSceneName
- 阅读 SceneLoadConfigSO 定义，确认其继承 ScriptableObject，属于 UnityEngine.Object 体系，存在 Unity 特有的 null 语义
- 交叉检查 L2 scene-service 技术文档，确认设计意图就是在切换时保留前一个场景配置用于卸载和事件通知

### Debug Instrumentation
- 在 SceneService 的 BeginPreload、HandleSceneRequest 入口、config 查找后、previousConfig 赋值后、_currentConfig 切换后、StartCoroutine 前添加统一前缀日志
- 在 TransitionGate.Begin 的入参处、previousSceneName 推导后、boot gate 通过后、卸载 previous scene 前后添加统一前缀日志
- 日志同时输出 managed null、Unity null、GetInstanceID、assetName、sceneName，用于区分断点时机问题与 UnityEngine.Object 假 null

### Scene Unload Fix
- 将 SceneLoader 的卸载目标解析从仅 SceneManager.GetSceneByName 扩展为兼容 scene name、完整 scene path、以及路径末段文件名
- 在实际执行卸载时打印请求值、命中的 scene.name 和 scene.path，便于判断场景名与配置值是否一致
- 将 SceneService 的当前场景跟踪从 SceneLoadConfigSO 引用改为纯运行时快照（sceneName/scenePath/assetLabels）
- 将 TransitionGate 的 previous scene 入参改为快照字段，而不是依赖可能已被 Unity/Addressables 释放的 previousConfig ScriptableObject

### Loading Overlay Input Fix
- 修复 UIService 的 Loading Overlay 显隐逻辑：隐藏时不再只设置 alpha，同时同步关闭 CanvasGroup.interactable 和 CanvasGroup.blocksRaycasts
- 在 UIService.OnWire 初始化时强制把 Loading Overlay 置为隐藏且不拦截输入，避免首帧或上次残留状态继续挡鼠标

### Scene Routing Adjustment
- 将 UIService.RequestNewGame 的目标场景从 NewGame 切换为 PathFinding，以便 MainMenu 的“新游戏”按钮直接进入 PathFinding 场景

### Editor Startup Safety
- 重写 EditorCoreLoader 的编辑器直跑策略：非 Core 场景进入 Play 时，不再 Additive 打开 Core，而是临时把 playModeStartScene 指向 Core
- 用 SessionState 记录进入 Play 前的活动场景名，并在返回 EditMode 后恢复原 playModeStartScene，避免污染编辑器设置
- 修改 GameService 启动逻辑：不再在 Editor 下对 activeScene 再补发一次 SceneRequest，而是把记录下来的场景名作为 SceneService.BeginPreload 的首场景覆盖参数
- 修改 SceneService.BeginPreload，允许以指定场景配置作为首场景预加载，避免“先进 MainMenu 再二次切换到测试场景”的双加载路径

### PathFinding Volume Verification
- 将 PathFinding 场景的 Global Volume Profile 引用切到已知生效的 SampleSceneProfile，用最小资产改动验证 Volume 链路本身是否正常
- 保留原 PathFinding 专属 Profile 资产不删，仅先绕过其过弱的配置，以便后续再决定是否回填独立风格

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 将问题定性为“先验证调试时机和 Unity null 语义”，而不是直接修改 SceneService | A: 直接改写赋值逻辑；B: 立刻给 previousConfig 加缓存字段 | 当前代码顺序上不存在把非 null 赋成 null 的路径，先改代码属于在未证伪前提下修表象，风险高且没有必要 |
| 优先建议把断点移到赋值后的下一行或 StartCoroutine 调用处验证 | A: 仅凭观察继续猜测；B: 先重构为多行辅助变量 | 这是最便宜且最能区分“断点停在线前”与“真实传参丢失”的检查 |
| 将 UnityEngine.Object 的假 null 作为次级假设而不是主因 | A: 直接认定资源被销毁；B: 忽略 Unity null 语义 | SceneLoadConfigSO 是 ScriptableObject，理论上可能受 Unity null 重载影响，但从当前调用链看更像调试观察时机问题 |
| 修复卸载时优先增强 SceneLoader 的场景定位，而不是继续改 previousConfig 传递 | A: 修改 SceneService 额外保存 previous path；B: 只靠日志等待复现 | MainMenu 未卸载更可能是卸载目标查找不命中；把 SceneLoader 做成兼容 name/path 的解析更接近根因，也不影响现有调用方 |
| 当前场景状态必须脱离 UnityEngine.Object 生命周期单独缓存 | A: 继续把 SceneLoadConfigSO 当作 runtime state；B: 每次切换时重新反查当前已加载场景 | 日志已证明 _currentConfig 在切场景前就变成 unityNull=true；运行时状态若继续依赖 SO 引用，previous scene 信息仍会丢失 |
| Loading Overlay 的隐藏必须同时关闭射线拦截 | A: 只改 alpha；B: 直接禁用整个 GameObject | 当前问题是“看不见但挡鼠标”，CanvasGroup 的 blocksRaycasts/interactable 正是控制点；直接禁用 GameObject 会扩大行为面且没必要 |
| 编辑器下直跑非 Core 场景应通过 playModeStartScene 重定向到 Core，而不是把 Core Additive 打进当前场景 | A: 保留 Additive Core；B: 继续在 GameService.Start 里按 activeScene 补发请求 | Additive Core 会和内容场景自带的全局根叠加，补发 activeScene 还会造成二次加载；改成从 Core 单点启动并显式传递目标场景更符合 L1 唯一根设计 |

## Known Issues

- [x] 已追加实例 ID、ReferenceEquals 语义等调试日志，可直接在运行日志中证伪 previousConfig 是否真实丢失
- [x] 已确认 previousConfig 不是赋值丢失，而是引用的 SceneLoadConfigSO 在切换前已变成 Unity 假 null
- [ ] 如果 MainMenu 仍未卸载，需要查看新的 SceneLoader 日志中 requested/name/path 是否命中到了错误场景 — P1 — 下一步根据日志决定是否改为直接按 previousConfig.ScenePath 调用或补充 active scene 保护

## Cross-References

### Related Sessions
- _No related sessions/plans/docs._

### Related Plans
- _No related sessions/plans/docs._

### Related Tech Docs
- [.agent/tech/L2-services/L2-scene-service/scene-service-loading.md](../tech/L2-services/L2-scene-service/scene-service-loading.md) — SceneService v2 的切换设计与 previous scene 生命周期说明

### Related Design Docs
- _No related sessions/plans/docs._

### Flag for Design Doc Creation
- [ ] NEW design doc needed for: none — because: this session only investigated runtime/debug behavior
