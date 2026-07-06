# 2026-07-06 — 场景重复 GameManager + Addressables Build 修复

## Background

Core 是常驻场景，GameManager 通过 DontDestroyOnLoad 全局唯一。但 MainMenu 和 SampleScene
也都放了 GameManager prefab，导致 additive 加载时产生第二个 GameManager 实例。第二个实例
虽被 GameService.Awake 立即 Destroy，但其子组件（InputService、SceneService 等）的
OnEnable/OnDisable 仍会执行，污染共享的 ScriptableObject 状态。

此外，Core.unity 中 SceneService 的 `_firstSceneConfig` 被覆写指向不存在的 GUID，导致
`BeginPreload` 中 `initialConfig` 为 null。

Build 环境下 Addressables catalog 未就绪时，`InitializeAsync()` 返回的 handle 内部状态
无效，直接访问 `.Status` 抛出 `Attempting to use an invalid operation handle`。

## Changes

### 场景 GameManager 重复修复
- **MainMenu.unity** — 移除 GameManager PrefabInstance（含 SceneRoots 引用）
- **SampleScene.unity** — 移除 GameManager PrefabInstance（含 SceneRoots 引用）
- **Core.unity** — 删除 GameManager PrefabInstance 上两个 broken field override：
  `_firstSceneConfig`（指向不存在的 GUID `36224b16...`）和 `_configs.Array.size: 3`

### Addressables Build 修复
- **AddressablesService.cs** — `InitializeAsync()` while 循环增加 `IsValid()` 检查；
  IsDone 后再检查一次 `IsValid()`，handle 失效时假定系统已初始化并 graceful return

### 其他清理
- **Player.prefab** — 清理空 `initialTags` / `initialPassives` 数组；`innateTrees` 顺序调整
- **PathFinding.unity** — Light color 复位到白色；移除空 `initialPassives` / `targetLayers`
- **Default Local Group.asset** — 移除 boot 标签组中多余 scene config 条目
- **ProjectSettings.asset** — 添加 `preloadedAssets`（InputActionAsset 等）
- **addressables_content_state.bin** — 内容状态更新

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 从 additive 场景移除 GameManager，而非加 `_isDuplicate` 代码守卫 | A: 在每个 ModuleChildMono 中检查 `GameService.Instance` 是否自己是 root → 改动分散、治标不治本 | Core 是唯一常驻场景，additive 场景不需要 GameManager。从源头消除重复，代码层不额外引入守卫逻辑 |
| AddressablesService 只加 `IsValid()` 检查，不加 try-catch | A: try-catch 包裹整个 InitializeAsync → 捕获范围过宽，可能掩盖真正异常 | `AsyncOperationHandle.IsValid()` 是 Addressables API 文档推荐的标准检查，语义明确 |

## Known Issues

- [ ] Build 环境下 Addressables 需手动构建（`m_BuildAddressablesWithPlayerBuild: 0`），后续考虑改为自动构建 — P1
- [x] `InputService` 的 `_isDuplicate` 守卫已撤回 — 从场景移除 GameManager 后不需要

## Cross-References

### Related Sessions
- [2026-06-22-inputservice-editor-validation.md](2026-06-22-inputservice-editor-validation.md) — InputService 编辑器校验

### Related Tech Docs
- [tech/L2-services/L2-input/input-service.md](../tech/L2-services/L2-input/input-service.md) — 注：此文档描述旧架构（BaseService + Handler），当前代码已改为 ModuleChildMono + 直接绑定

### Flag for Design Doc Creation
- [x] No design doc needed — bug fix + internal robustness improvement, no design-facing changes.
