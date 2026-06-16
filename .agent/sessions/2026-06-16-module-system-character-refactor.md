# 2026-06-16 Module 系统 + Character 树形生命周期重构

## 背景

Character 模块初始化堆在 Awake 里，依赖 Unity 隐式调用顺序，已导致 EventHub 时序 bug。纯 C# 子模块构造参数各自为政，Model 替换时引用全部过期。缺少通用模块生命周期抽象。

## 方案

1. L1 定义 `IInitializable` 协议 (OnAssemble + OnWire)
2. L1 建 Module 系统四件套: ModuleRegistry, Module, ModuleBehaviour, ModuleComponent
3. `CharacterBuildContext` 统一所有子模块依赖
4. Character 全链路接入 Module 系统

## 设计决策

- 树形递归: 父模块持有 Registry，子模块构造自注册，Awake 自动发现 MB 子
- 无 Teardown: Unity Destroy 级联 + IGameplaySessionHandler 覆盖清理
- AnimationBrain 升 ModuleBehaviour: Drivers 是其子模块，不再挂 Actor 下
- Footstep 事件桥接 (AnimationBrain 中介，CharacterAudio 订阅)
- BaseService/GameService 后续 PR 跟进

## 已知问题

- AnimationBrain.OnWire 中 FootstepCallback → event 桥接是临时方案，TODO 改 EventHub
- Prefab 需手动移除根节点 LocomotionDriver/TraversalDriver (已由 Brain 代码挂载)
- ReplaceModel 为占位，Phase 3 完整实现
