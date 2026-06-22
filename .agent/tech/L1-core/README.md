# L1_Core · GameManager 层

> 根节点层 — 持有并管理所有 L2 Service，不包含业务逻辑。

## 层级定位

L1 是架构的根。GameService 继承 ModuleHub 作为 Module 树根节点，GameContext 做数据总线。

**向下管理 L2**：GameService 通过 ModuleHub.Awake 自动扫描发现所有 L2 Service。
**不向上依赖**：L1 不依赖任何上层模块。

## 调用链

```
GameService.Awake() [order=-500, DontDestroyOnLoad]
  │
  ├── 单例守卫 + DontDestroyOnLoad
  ├── GameContext 主动创建 + Initialize
  └── base.Awake()  → ModuleHub
      ├── GetComponentsInChildren<ModuleChildMono> 扫描
      ├── 判主 → Register
      └── Registry.OnAssembleAll() → 所有 Service.OnAssemble() → RegisterService(self)

GameService.Start()
  ├── TryResolveService<EventDispatcherService> + Subscribe<SGameState> (pre-wire)
  ├── base.Start() → Registry.OnWireAll() → 所有 Service.OnWire()
  └── Editor 自动加载场景 (post-wire)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| GameService | — | 根节点，不被任何模块依赖 |
| GameContext | 所有 L2 Service、L3 Module | Service Registry + Snapshot Store |
| BaseService | 所有 L2 Service | 继承此基类 |
| GameService | L2 全部 Service | 持有、发现、注册、销毁 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 四阶段 Service 初始化 | 解耦注册顺序，OnServicesReady 时所有依赖就绪 |
| PublishState 原子写入 | Snapshot 和 Event 同步，避免数据不一致 |
| DOTween unscaledTime | Time.timeScale=0 时 UI 动画不冻结 |

## 未来规划

各子文档已有详细未来规划，汇总如下：

| 规划 | 状态 | 来源 |
|------|------|------|
| Inspector 分组展示 Snapshots/Services | 待做 | [game-context.md](game-context.md) |
| Bootstrap 失败回滚 | 待做 | [game-service.md](game-service.md)、[base-service.md](base-service.md) |
| Editor 路径与运行时路径统一 | 待做 | [game-service.md](game-service.md) |
| ActorRegistry 角色引用字典 | 待做 | [game-context.md](game-context.md) |
| Phase 1 GameplayTag 资产创建 (21个) | 待做 | [gameplay-tag.md](gameplay-tag.md) |
| Phase 2 GameplayTag 资产创建 (剩余) | 待做 | [gameplay-tag.md](gameplay-tag.md) |
| 天气系统快照 | 远期 | [game-context.md](game-context.md) |
| 多人支持 | 远期 | [game-context.md](game-context.md) |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [game-context.md](game-context.md) | GameContext — Service Registry + Snapshot Store，每个方法 |
| [game-service.md](game-service.md) | GameService — Bootstrap 五步启动 + TeardownSession |
| [base-service.md](base-service.md) | BaseService — 四阶段生命周期 + TryResolveService + PublishState |
| [gameplay-tag.md](gameplay-tag.md) | GameplayTag 完整资产树 — 9 根 190 资产，按 Phase 分阶段 |
| [gameplay-tag-runtime.md](gameplay-tag-runtime.md) | GameplayTag struct — 运行时值类型，Matches/Depth/隐式转换 |
| [gameplay-tag-container.md](gameplay-tag-container.md) | GameplayTagContainer — 标签集合，HasTag/HasTagExact/AddTag |
| [structs.md](structs.md) | MetaStruct + 所有 Core Context Struct |
