# 2026-06-16-model-setup-refinement

## 背景

v0.14.11 将配置集中到 CharacterActor 后，Model 运行时装配还有一些问题需要修复。

## 改动内容

### CharacterActor Awake 拆分

- `SetupModel()` — 实例化 Model + 补组件 + 清理旧节点
- `ResolveComponents()` — 所有 GetComponent 集中一处
- `SetupAnimation()` — CharacterRig + AnimationBrain 对接
- `SetupModules()` — Director / Kinematic / Locomotion / Combat

### Model 装配修复

- **animancerTransitions**: CharacterActor 新增 `TransitionLibraryAsset` 字段，SetupModel 中注入到 NamedAnimancerComponent
- **DestroyImmediate**: 旧 Model 清理从 `Destroy` 改为 `DestroyImmediate`——`Destroy` 延迟执行导致新旧 AnimationBrain 并存，Animancer 状态引用悬空崩溃
- **清理顺序**: 先判 modelPrefab 是否设置，有才删旧建新

### 角色 Prefab 拆分

- 从 Synty 全家桶 Prefab 拆分出 30 个独立角色 Prefab
- 拆分时修正：Avatar 引用（External GUID → Art GUID）+ Animator 配置（AnimatePhysics + AlwaysAnimate）
- 拆分脚本用完即删
